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
    public partial class KinematicProjectileController : BaseProjectile
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

        [Header("Hit VFX Overrides")]
        [SerializeField] private GameObject playerHitVFXPrefab;
        [SerializeField] private GameObject environmentHitVFXPrefab;

        [Header("Visual Overrides (Scale & Customization)")]
        [SerializeField] private float projectileScale = 1f;
        [SerializeField] private float explosionScale = 1f;
        [SerializeField] private float explosionSpeed = 1f;
        [SerializeField] private GameObject[] extraExplosionPrefabs;

        private Vector3 _velocity;
        private float _aliveTime;
        private int _bounceCount;
        private Transform _homingTarget;
        private float _homingSearchTimer;
        private Vector3 _startPosition;
        private float _distanceTraveled;
        private float _maxDistance;
        private ParticleSystem[] _cachedParticleSystems;
        private static int _projectileHitMask = -1;
        private static readonly Collider[] _homingHitBuffer = new Collider[16];

        private void Awake()
        {
            _cachedParticleSystems = GetComponentsInChildren<ParticleSystem>(true);
        }

        public override void Init(WeaponSO data, GameObject owner)
        {
            base.Init(data, owner);

            if (_projectileHitMask == -1)
            {
                int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
                int ui = LayerMask.NameToLayer("UI");
                int maskToIgnore = 0;
                if (ignoreRaycast >= 0) maskToIgnore |= (1 << ignoreRaycast);
                if (ui >= 0) maskToIgnore |= (1 << ui);
                _projectileHitMask = ~maskToIgnore;
            }

            if (data != null)
            {
                speed = data.projectileSpeed > 0.1f ? data.projectileSpeed : speed;
                hasGravity = data.hasGravity;
                canBounce = data.maxBounces > 0;
                maxBounces = data.maxBounces;

                if (data.projectileLifetime > 0.05f)
                {
                    lifetime = data.projectileLifetime;
                }
                else if (data.maxRange > 0.1f)
                {
                    lifetime = data.maxRange / Mathf.Max(speed, 0.1f);
                }
                else
                {
                    lifetime = lifetime > 0.1f ? lifetime : 5f;
                }

                _maxDistance = data.maxRange > 0.1f ? data.maxRange : 0f;
            }
            else
            {
                _maxDistance = 0f;
            }

            _startPosition = transform.position;
            _distanceTraveled = 0f;
            _velocity = transform.forward * speed;
            _aliveTime = 0f;
            _bounceCount = 0;

            transform.localScale = Vector3.one * projectileScale;

            if (_cachedParticleSystems == null)
                _cachedParticleSystems = GetComponentsInChildren<ParticleSystem>(true);

            foreach (var ps in _cachedParticleSystems)
            {
                if (ps != null)
                {
                    ps.Play();
                }
            }

            _homingSearchTimer = 0f;
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
                if (GlobalPoolManager.Instance != null) GlobalPoolManager.Instance.Despawn(gameObject);
                else Destroy(gameObject);
                return;
            }

            // 1. Homing direction adjustment
            if (isHoming)
            {
                _homingSearchTimer -= dt;
                if (_homingTarget == null && _homingSearchTimer <= 0f)
                {
                    _homingSearchTimer = 0.1f; // 10 Hz throttled non-alloc search
                    FindHomingTarget();
                }

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

            _distanceTraveled += distance;
            if (_maxDistance > 0.1f && _distanceTraveled >= _maxDistance)
            {
                if (GlobalPoolManager.Instance != null) GlobalPoolManager.Instance.Despawn(gameObject);
                else Destroy(gameObject);
                return;
            }

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

                            GameObject bounceVfx = (_weaponData != null && _weaponData.HitVFXPrefab != null)
                                ? _weaponData.HitVFXPrefab
                                : environmentHitVFXPrefab;
                            if (bounceVfx != null)
                            {
                                Instantiate(bounceVfx, hit.point, Quaternion.LookRotation(hit.normal));
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

    }
}
