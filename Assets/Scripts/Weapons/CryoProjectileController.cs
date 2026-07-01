using UnityEngine;
using Shootopia;

namespace Shootopia
{
    public class CryoProjectileController : BaseProjectile
    {
        [SerializeField] private GameObject iceBurstVFX;
        
        private Rigidbody rb;

        public override void Init(WeaponSO data, GameObject owner)
        {
            base.Init(data, owner);
            rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = transform.forward * data.projectileSpeed;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Don't hit the player who fired (check root for children)
            if (other.transform.root == _owner.transform.root) return;
            
            Explode();
        }

        private void Explode()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, _weaponData.blastRadius);
            System.Collections.Generic.HashSet<IDamageable> processed = new System.Collections.Generic.HashSet<IDamageable>();
            foreach (Collider c in hits)
            {
                IDamageable t = c.GetComponentInParent<IDamageable>();
                if (t != null && !processed.Contains(t))
                {
                    processed.Add(t);
                    // Use center of collider for distance check if possible, otherwise transform position
                    Vector3 targetPos = c.bounds.center;
                    float dist = Vector3.Distance(transform.position, targetPos);
                    
                    // Check if it's a Robot for status effects (check parent too)
                    if (dist <= 1.5f)
                    {
                        t.TakeDamage(_weaponData.Damage, DamageType.Projectile, _weaponData);
                    }
                    else
                    {
                        t.TakeDamage(_weaponData.splashDamage, DamageType.Projectile, _weaponData);
                    }
                }
            }
            
            if (iceBurstVFX != null)
            {
                Instantiate(iceBurstVFX, transform.position, Quaternion.identity);
            }
            
            Destroy(gameObject);
        }
    }
}
