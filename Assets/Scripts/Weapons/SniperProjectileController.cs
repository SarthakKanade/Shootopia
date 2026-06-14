using UnityEngine;
using VelocityWars.Core;

namespace VelocityWars
{
    public class SniperProjectileController : BaseProjectile
    {
        [SerializeField] private GameObject impactVFXPrefab;
        private Rigidbody rb;
        private float _damage;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        public override void Init(WeaponSO data, GameObject owner)
        {
            base.Init(data, owner);
            _damage = data.Damage;

            if (rb == null) rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = false;
                rb.linearVelocity = transform.forward * data.projectileSpeed;
            }

            Destroy(gameObject, 2f);
        }

        private void OnTriggerEnter(Collider other)
        {
            // Don't collide with owner or its children
            if (other.transform.root == _owner.transform.root) return;
            
            // If it's a trigger, only proceed if it's damageable
            if (other.isTrigger && !other.TryGetComponent(out IDamageable _)) return;

            // Check for IDamageable
            if (other.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(_damage, DamageType.Bullet, _weaponData);
            }

            // Visuals
            if (impactVFXPrefab != null)
            {
                Instantiate(impactVFXPrefab, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }
    }
}
