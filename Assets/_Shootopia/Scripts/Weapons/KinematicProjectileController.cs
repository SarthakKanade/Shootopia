using UnityEngine;
using VelocityWars.Core;

namespace VelocityWars
{
    /// <summary>
    /// Programmatic kinematic projectile controller that moves bullet visuals
    /// and performs manual raycast/linecast sweeps for collision detection.
    /// Replaces standard physics Rigidbodies for reliable netcode-friendly registration.
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
        private Vector3 _lastPosition;

        public override void Init(WeaponSO data, GameObject owner)
        {
            base.Init(data, owner);

            // Read settings from WeaponSO data
            if (data != null)
            {
                speed = data.projectileSpeed;
                hasGravity = data.hasGravity;
                canBounce = data.maxBounces > 0;
                maxBounces = data.maxBounces;
                
                // Set lifetime (default to 5s if not specified)
                lifetime = data.projectileLifetime > 0.1f ? data.projectileLifetime : 5f;
            }

            _velocity = transform.forward * speed;
            _aliveTime = 0f;
            _bounceCount = 0;
            _lastPosition = transform.position;

            // Apply scale override
            transform.localScale = Vector3.one * projectileScale;

            if (isHoming)
            {
                FindHomingTarget();
            }
        }

        private void Update()
        {
            _aliveTime += Time.deltaTime;
            if (_aliveTime >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            // 1. Calculate Homing direction adjustment
            if (isHoming)
            {
                if (_homingTarget == null)
                {
                    FindHomingTarget();
                }

                if (_homingTarget != null)
                {
                    Vector3 targetDir = (_homingTarget.position - transform.position).normalized;
                    _velocity = Vector3.RotateTowards(_velocity.normalized, targetDir, homingTurnSpeed * Time.deltaTime * Mathf.Deg2Rad, 0f) * speed;
                    transform.rotation = Quaternion.LookRotation(_velocity);
                }
            }

            // 2. Apply Gravity if active
            if (hasGravity)
            {
                _velocity += Physics.gravity * gravityScale * Time.deltaTime;
            }

            // 3. Move and perform collision sweeps
            Vector3 currentPos = transform.position;
            Vector3 delta = _velocity * Time.deltaTime;
            Vector3 nextPos = currentPos + delta;
            float distance = delta.magnitude;

            if (distance > 0.0001f)
            {
                Ray ray = new Ray(currentPos, delta.normalized);
                if (Physics.Raycast(ray, out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Ignore))
                {
                    // Ignore collision with the shooter (owner)
                    if (_owner != null && hit.transform.root == _owner.transform.root)
                    {
                        transform.position = nextPos;
                    }
                    else
                    {
                        // If we hit an IDamageable target, deal damage and destroy immediately (do not bounce)
                        if (hit.collider.GetComponentInParent<IDamageable>() != null || hit.collider.GetComponent<IDamageable>() != null)
                        {
                            HandleImpact(hit);
                            return;
                        }

                        if (canBounce && _bounceCount < maxBounces)
                        {
                            _bounceCount++;
                            // Reflect velocity off the surface normal
                            _velocity = Vector3.Reflect(_velocity, hit.normal);
                            transform.rotation = Quaternion.LookRotation(_velocity);

                            // Spawn bounce sparks if hit VFX is configured
                            GameObject hitVFX = _weaponData != null ? _weaponData.HitVFXPrefab : null;
                            if (hitVFX != null)
                            {
                                Instantiate(hitVFX, hit.point, Quaternion.LookRotation(hit.normal));
                            }

                            // Advance position slightly away from surface to prevent getting stuck
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
                    transform.position = nextPos;
                }
            }

            _lastPosition = transform.position;
        }

        private void HandleImpact(RaycastHit hit)
        {
            // Apply Damage
            if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
            {
                float damage = _weaponData != null ? _weaponData.Damage : 10f;
                damageable.TakeDamage(damage, DamageType.Projectile, _weaponData);
            }

            // Spawn Void Zone if configured
            if (voidZonePrefab != null)
            {
                Vector3 spawnPoint = hit.point;
                if (hit.normal.y > 0.7f)
                {
                    spawnPoint += Vector3.up * 0.5f; // offset vertically upwards by 0.5 units
                }
                var zone = Instantiate(voidZonePrefab, spawnPoint, Quaternion.identity);
                var zoneScript = zone.GetComponent<VoidZone>();
                if (zoneScript != null && _weaponData != null)
                {
                    zoneScript.Initialize(_weaponData);
                }
            }
            else
            {
                // Spawn Hit VFX / Explosion
                GameObject hitVFX = _weaponData != null ? _weaponData.HitVFXPrefab : null;
                if (hitVFX != null)
                {
                    GameObject spawnedVFX = Instantiate(hitVFX, hit.point, Quaternion.LookRotation(hit.normal));
                    ApplyExplosionModifiers(spawnedVFX);
                }

                // Spawn extra explosion prefabs
                if (extraExplosionPrefabs != null && extraExplosionPrefabs.Length > 0)
                {
                    foreach (var extraPrefab in extraExplosionPrefabs)
                    {
                        if (extraPrefab != null)
                        {
                            GameObject spawnedExtra = Instantiate(extraPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                            ApplyExplosionModifiers(spawnedExtra);
                        }
                    }
                }
            }

            Destroy(gameObject);
        }

        private void ApplyExplosionModifiers(GameObject vfx)
        {
            if (vfx == null) return;

            // Scale the transform
            vfx.transform.localScale = Vector3.one * explosionScale;

            // Loop through all child ParticleSystems and scale particle speed
            ParticleSystem[] particleSystems = vfx.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in particleSystems)
            {
                var main = ps.main;
                main.simulationSpeed *= explosionSpeed;
            }

            // Loop through all child AudioSources and adjust volume and pitch
            AudioSource[] audioSources = vfx.GetComponentsInChildren<AudioSource>(true);
            foreach (var audio in audioSources)
            {
                audio.volume = Mathf.Clamp01(audio.volume * 1.5f);
                audio.pitch *= 0.7f;
            }
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

                // Target anything that is IDamageable (like Target Dummies, Enemies, etc.)
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
