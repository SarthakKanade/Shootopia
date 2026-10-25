using UnityEngine;
using Shootopia;
using Unity.Cinemachine;
using Unity.Netcode;

namespace Shootopia
{
    public class BlasterProjectileController : BaseProjectile
    {
        private Rigidbody rb;
        private bool hasExploded = false;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        public override void Init(WeaponSO data, GameObject owner)
        {
            base.Init(data, owner);
            
            // Set velocity on init so we use the SO's speed
            if (rb != null)
            {
                rb.linearVelocity = transform.forward * data.projectileSpeed;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasExploded) return;
            
            // Don't hit the player who fired (check root for children)
            if (_owner != null && other.transform.root == _owner.transform.root) return;

            Explode(other);
        }

        private void Explode(Collider directHitCollider = null)
        {
            hasExploded = true;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.TriggerCombatDucking(0.35f, 1.2f, true);
            }

            // Point of Contact (POC): either contact point on direct hit collider, or projectile position
            Vector3 poc = transform.position;
            if (directHitCollider != null)
            {
                poc = directHitCollider.ClosestPoint(transform.position);
            }

            float blastRadius = (_weaponData != null && _weaponData.blastRadius > 0f) ? _weaponData.blastRadius : 4.5f;
            float maxDamage = (_weaponData != null && _weaponData.Damage > 0f) ? _weaponData.Damage : 100f;
            float minDamage = (_weaponData != null && _weaponData.splashDamage > 0f) ? _weaponData.splashDamage : 25f;

            // Sphere overlap for splash damage from POC
            Collider[] hits = Physics.OverlapSphere(poc, blastRadius);
            var damagedTargets = new System.Collections.Generic.HashSet<IDamageable>();
            var damagedPlayers = new System.Collections.Generic.HashSet<PlayerHealth>();
            
            foreach (Collider hit in hits)
            {
                if (hit == null) continue;
                bool isOwner = (_owner != null && hit.transform.root == _owner.transform.root);
                bool allowSelfDamage = (_weaponData != null && (_weaponData.selfDamage || _weaponData.isExplosive));
                if (isOwner && !allowSelfDamage)
                    continue;

                IDamageable damageable = null;
                var targetHealth = hit.GetComponentInParent<PlayerHealth>() ?? hit.GetComponent<PlayerHealth>();
                if (targetHealth != null)
                {
                    if (!damagedPlayers.Add(targetHealth)) continue;
                    damageable = targetHealth;
                }
                else
                {
                    damageable = hit.GetComponentInParent<IDamageable>() ?? hit.GetComponent<IDamageable>();
                    if (damageable == null || !damagedTargets.Add(damageable)) continue;
                }

                // Check if this target is the direct contact hit
                bool isDirectHit = directHitCollider != null && (hit == directHitCollider || hit.transform.root == directHitCollider.transform.root);
                float damage;

                if (isDirectHit)
                {
                    // Direct contact hit guarantees full center damage (100 = 1-hit KO)
                    damage = maxDamage;
                }
                else
                {
                    // Distance from Point of Contact (POC) to closest point on target's collider
                    Vector3 closestPt = hit.ClosestPoint(poc);
                    float distance = Vector3.Distance(poc, closestPt);
                    
                    // Linear damage falloff: maxDamage at POC -> minDamage at blast edge
                    float t = Mathf.Clamp01(distance / blastRadius);
                    damage = Mathf.Lerp(maxDamage, minDamage, t);
                    damage = Mathf.Clamp(damage, minDamage, maxDamage);
                }

                DealDamage(damageable, damage, DamageType.Explosion, hit);
            }

            // Visuals at POC
            if (_weaponData != null && _weaponData.HitVFXPrefab != null)
            {
                Vector3 blastNormal = Vector3.up;
                if (Physics.Raycast(poc + Vector3.up * 0.2f, Vector3.down, out RaycastHit gHit, 1.5f))
                {
                    blastNormal = gHit.normal;
                }
                Quaternion rot = Mathf.Abs(blastNormal.y) > 0.95f
                    ? Quaternion.LookRotation(blastNormal, Vector3.forward)
                    : Quaternion.LookRotation(blastNormal, Vector3.up);
                Instantiate(_weaponData.HitVFXPrefab, poc, rot);
            }

            // Camera Shake
            var impulse = GetComponent<CinemachineImpulseSource>();
            if (impulse != null)
            {
                impulse.GenerateImpulse(0.35f);
            }
            else
            {
                CameraShakeManager.Instance?.ShakeExplosion(poc, blastRadius, 1.2f);
            }

            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            if (_weaponData != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, _weaponData.blastRadius);
            }
        }
    }
}
