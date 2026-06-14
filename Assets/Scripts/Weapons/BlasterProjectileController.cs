using UnityEngine;
using VelocityWars.Core;
using Unity.Cinemachine;

namespace VelocityWars
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
            if (other.transform.root == _owner.transform.root) return;

            Explode();
        }

        private void Explode()
        {
            hasExploded = true;

            // Sphere overlap for splash damage
            Collider[] hits = Physics.OverlapSphere(transform.position, _weaponData.blastRadius);
            
            foreach (Collider hit in hits)
            {
                if (hit.TryGetComponent(out IDamageable damageable))
                {
                    float distance = Vector3.Distance(transform.position, hit.transform.position);
                    
                    // Linear damage falloff: Damage (at center) -> SplashDamage (at edge)
                    float t = Mathf.Clamp01(distance / _weaponData.blastRadius);
                    float damage = Mathf.Lerp(_weaponData.Damage, _weaponData.splashDamage, t);
                    
                    // Ensure it doesn't go below splash damage
                    damage = Mathf.Clamp(damage, _weaponData.splashDamage, _weaponData.Damage);

                    damageable.TakeDamage(damage, DamageType.Explosion, _weaponData);
                }
            }

            // Visuals
            if (_weaponData.HitVFXPrefab != null)
            {
                Instantiate(_weaponData.HitVFXPrefab, transform.position, Quaternion.identity);
            }

            // Camera Shake
            var impulse = GetComponent<CinemachineImpulseSource>();
            if (impulse != null)
            {
                impulse.GenerateImpulse(0.25f);
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
