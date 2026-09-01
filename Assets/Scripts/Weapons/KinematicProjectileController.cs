using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Shootopia
{
    /// <summary>
    /// Kinematic projectile controller that moves bullet visuals and performs
    /// continuous raycast sweeps for reliable netcode-friendly collision registration.
    /// Supports gravity, ricochet bouncing, homing, explosive splash, and void zone spawning.
    /// </summary>
    public class KinematicProjectileController : BaseProjectile
    {
        [Header("Movement Settings")]
        [SerializeField] private float speed = 40f;
        [SerializeField] private float lifetime = 5f;
        [SerializeField] private bool hasGravity = false;
        [SerializeField] private float gravityScale = 1f;

        [Header("Bounce Settings (Trick Shot)")]
        [SerializeField] private bool canBounce = false;
        [SerializeField] private int maxBounces = 3;

        [Header("Homing Settings")]
        [SerializeField] private bool isHoming = false;
        [SerializeField] private float homingTurnSpeed = 15f;
        [SerializeField] private float homingRange = 25f;

        [Header("Void Settings (Void Gun)")]
        [SerializeField] private GameObject voidZonePrefab;

        [Header("Visual Overrides (Scale & Customization)")]
        [SerializeField] private float projectileScale = 1f;
        [SerializeField] private float explosionScale = 1f;
        [SerializeField] private float explosionSpeed = 1f;
        [SerializeField] private GameObject[] extraExplosionPrefabs;

        private Vector3 _velocity;
        private float _aliveTime;
        private int _bounceCount;
        private Transform _homingTarget;

        public override void Init(WeaponSO data, GameObject owner)
        {
            base.Init(data, owner);

            if (data != null)
            {
                speed = data.projectileSpeed > 0.1f ? data.projectileSpeed : speed;
                hasGravity = data.hasGravity;
                canBounce = data.maxBounces > 0;
                maxBounces = data.maxBounces;
                lifetime = data.projectileLifetime > 0.1f ? data.projectileLifetime : 5f;
            }

            _velocity = transform.forward * speed;
            _aliveTime = 0f;
            _bounceCount = 0;

            transform.localScale = Vector3.one * projectileScale;

            if (isHoming)
            {
                FindHomingTarget();
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _aliveTime += dt;
            if (_aliveTime >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            // 1. Homing direction adjustment
            if (isHoming)
            {
                if (_homingTarget == null) FindHomingTarget();

                if (_homingTarget != null)
                {
                    Vector3 targetDir = (_homingTarget.position - transform.position).normalized;
                    _velocity = Vector3.RotateTowards(_velocity.normalized, targetDir, homingTurnSpeed * dt * Mathf.Deg2Rad, 0f) * speed;
                    transform.rotation = Quaternion.LookRotation(_velocity);
                }
            }

            // 2. Apply gravity
            if (hasGravity)
            {
                _velocity += Physics.gravity * gravityScale * dt;
            }

            // 3. Sweep collision detection
            Vector3 currentPos = transform.position;
            Vector3 delta = _velocity * dt;
            float distance = delta.magnitude;

            if (distance > 0.0001f)
            {
                Ray ray = new Ray(currentPos, delta.normalized);
                if (Physics.Raycast(ray, out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Ignore))
                {
                    // Ignore collision with shooter
                    if (_owner != null && hit.transform.root == _owner.transform.root)
                    {
                        transform.position = currentPos + delta;
                    }
                    else
                    {
                        // Direct hit on damageable targets immediately triggers impact
                        bool isDamageable = hit.collider.GetComponentInParent<IDamageable>() != null || hit.collider.GetComponent<IDamageable>() != null;

                        if (!isDamageable && canBounce && _bounceCount < maxBounces)
                        {
                            _bounceCount++;
                            _velocity = Vector3.Reflect(_velocity, hit.normal);
                            transform.rotation = Quaternion.LookRotation(_velocity);

                            if (_weaponData != null && _weaponData.HitVFXPrefab != null)
                            {
                                Instantiate(_weaponData.HitVFXPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                            }

                            transform.position = hit.point + hit.normal * 0.02f;
                        }
                        else
                        {
                            HandleImpact(hit);
                            return;
                        }
                    }
                }
                else
                {
                    transform.position = currentPos + delta;
                }
            }
        }

        private void HandleImpact(RaycastHit hit)
        {
            Vector3 poc = hit.point;
            bool isSplashWeapon = _weaponData != null && (_weaponData.isExplosive || _weaponData.blastRadius > 0.1f);

            if (isSplashWeapon)
            {
                ApplySplashDamage(poc, hit);
            }
            else
            {
                ApplyDirectDamage(hit);
            }

            SpawnVoidZone(hit);
            SpawnExplosionVFX(hit);

            Destroy(gameObject);
        }

        private void ApplySplashDamage(Vector3 poc, RaycastHit directHit)
        {
            float blastRadius = (_weaponData != null && _weaponData.blastRadius > 0.1f) ? _weaponData.blastRadius : 3.5f;
            float maxDamage = _weaponData != null ? _weaponData.Damage : 25f;
            float minDamage = (_weaponData != null && _weaponData.splashDamage > 0f) ? _weaponData.splashDamage : (maxDamage * 0.25f);

            Collider[] hitColliders = Physics.OverlapSphere(poc, blastRadius);
            var processedTargets = new HashSet<IDamageable>();
            ulong attackerId = GetAttackerClientId();

            foreach (var col in hitColliders)
            {
                if (col == null) continue;
                if (_weaponData != null && !_weaponData.selfDamage && _owner != null && col.transform.root == _owner.transform.root)
                    continue;

                var targetDamageable = col.GetComponentInParent<IDamageable>() ?? col.GetComponent<IDamageable>();
                if (targetDamageable == null || processedTargets.Contains(targetDamageable)) continue;

                processedTargets.Add(targetDamageable);

                if (IsOwnerProjectile())
                {
                    var targetHealth = col.GetComponentInParent<PlayerHealth>() ?? col.GetComponent<PlayerHealth>();
                    if (targetHealth != null) targetHealth.LastAttackerClientId = attackerId;

                    float distToPOC = Vector3.Distance(poc, col.bounds.center);
                    float normalizedDist = Mathf.Clamp01(distToPOC / blastRadius);
                    float calculatedDamage = Mathf.Lerp(maxDamage, minDamage, normalizedDist);

                    targetDamageable.TakeDamage(calculatedDamage, DamageType.Explosion, _weaponData);

                    // Freeze status effect
                    if (IsFreezeWeapon() && targetHealth != null)
                    {
                        bool isDirect = (col.gameObject == directHit.collider.gameObject || distToPOC <= 0.45f);
                        if (isDirect)
                        {
                            targetHealth.ApplyFreezeEffect(5.0f, 1.0f, true);
                        }
                        else
                        {
                            float maxSlow = _weaponData.maxSlowPercent > 0f ? _weaponData.maxSlowPercent : 0.80f;
                            float minSlow = _weaponData.minSlowPercent > 0f ? _weaponData.minSlowPercent : 0.20f;
                            float slowPercent = Mathf.Lerp(maxSlow, minSlow, normalizedDist);
                            float slowDuration = Mathf.Lerp(3.5f, 1.5f, normalizedDist);
                            targetHealth.ApplyFreezeEffect(slowDuration, slowPercent, false);
                        }
                    }
                }
            }
        }

        private void ApplyDirectDamage(RaycastHit hit)
        {
            if (!IsOwnerProjectile()) return;

            if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
            {
                ulong attackerId = GetAttackerClientId();
                var targetHealth = hit.collider.GetComponentInParent<PlayerHealth>() ?? hit.collider.GetComponent<PlayerHealth>();

                if (targetHealth != null)
                {
                    targetHealth.LastAttackerClientId = attackerId;

                    if (IsFreezeWeapon())
                    {
                        targetHealth.ApplyFreezeEffect(5.0f, 1.0f, true);
                    }

                    if (IsFireWeapon())
                    {
                        float duration = _weaponData.afterburnDuration > 0f ? _weaponData.afterburnDuration : 5.0f;
                        float dps = _weaponData.afterburnDPS > 0f ? _weaponData.afterburnDPS : 5.0f;
                        targetHealth.ApplyBurnEffect(duration, dps, _weaponData);
                    }
                }

                float damage = _weaponData != null ? _weaponData.Damage : 10f;
                damageable.TakeDamage(damage, DamageType.Projectile, _weaponData);
            }
        }

        private void SpawnVoidZone(RaycastHit hit)
        {
            if (voidZonePrefab == null) return;

            Vector3 spawnPoint = hit.point;
            if (hit.normal.y > 0.7f) spawnPoint += Vector3.up * 0.5f;

            var zone = Instantiate(voidZonePrefab, spawnPoint, Quaternion.identity);
            var zoneScript = zone.GetComponent<VoidZone>();
            if (zoneScript != null && _weaponData != null)
            {
                zoneScript.Initialize(_weaponData, GetAttackerClientId());
            }
        }

        private void SpawnExplosionVFX(RaycastHit hit)
        {
            Quaternion hitRotation = hit.normal != Vector3.zero
                ? (Mathf.Abs(hit.normal.y) > 0.95f ? Quaternion.LookRotation(hit.normal, Vector3.forward) : Quaternion.LookRotation(hit.normal, Vector3.up))
                : Quaternion.LookRotation(Vector3.up, Vector3.forward);

            if (_weaponData != null && _weaponData.HitVFXPrefab != null)
            {
                var spawned = Instantiate(_weaponData.HitVFXPrefab, hit.point, hitRotation);
                ApplyExplosionModifiers(spawned);
            }

            if (extraExplosionPrefabs != null)
            {
                foreach (var extra in extraExplosionPrefabs)
                {
                    if (extra != null)
                    {
                        var spawnedExtra = Instantiate(extra, hit.point, hitRotation);
                        ApplyExplosionModifiers(spawnedExtra);
                    }
                }
            }
        }

        private void ApplyExplosionModifiers(GameObject vfx)
        {
            if (vfx == null) return;
            vfx.transform.localScale = Vector3.one * explosionScale;

            foreach (var ps in vfx.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                main.simulationSpeed *= explosionSpeed;
            }

            foreach (var audio in vfx.GetComponentsInChildren<AudioSource>(true))
            {
                audio.volume = Mathf.Clamp01(audio.volume * 1.5f);
                audio.pitch *= 0.7f;
            }
        }

        private ulong GetAttackerClientId()
        {
            if (_owner != null)
            {
                var netObj = _owner.GetComponent<NetworkObject>();
                if (netObj != null) return netObj.OwnerClientId;
            }
            return NetworkManager.ServerClientId;
        }

        private bool IsFreezeWeapon()
        {
            if (_weaponData == null) return false;
            return _weaponData.freezeDuration > 0f || _weaponData.maxSlowPercent > 0f ||
                   (!string.IsNullOrEmpty(_weaponData.weaponName) && _weaponData.weaponName.ToLower().Contains("freeze"));
        }

        private bool IsFireWeapon()
        {
            if (_weaponData == null) return false;
            return _weaponData.afterburnDuration > 0f || _weaponData.afterburnDPS > 0f ||
                   (!string.IsNullOrEmpty(_weaponData.weaponName) && (_weaponData.weaponName.ToLower().Contains("flame") || _weaponData.weaponName.ToLower().Contains("fire")));
        }

        private void FindHomingTarget()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, homingRange);
            float minAngle = 360f;
            Transform bestTarget = null;

            foreach (var col in colliders)
            {
                if (col.isTrigger) continue;
                if (_owner != null && col.transform.root == _owner.transform.root) continue;

                if (col.GetComponent<IDamageable>() != null || col.gameObject.layer == LayerMask.NameToLayer("Enemy"))
                {
                    Vector3 toTarget = (col.transform.position - transform.position).normalized;
                    float angle = Vector3.Angle(transform.forward, toTarget);
                    if (angle < minAngle)
                    {
                        minAngle = angle;
                        bestTarget = col.transform;
                    }
                }
            }

            _homingTarget = bestTarget;
        }
    }
}
