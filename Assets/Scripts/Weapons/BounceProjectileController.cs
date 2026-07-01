using UnityEngine;
using Shootopia;

namespace Shootopia
{
    public class BounceProjectileController : BaseProjectile
    {
        [SerializeField] private GameObject bounceSparkVFX;
        
        private Rigidbody rb;
        private int _bounceCount = 0;
        private int _maxBounces;
        private float _damage;
        private float _lifetime;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        public override void Init(WeaponSO data, GameObject owner)
        {
            base.Init(data, owner);
            _maxBounces = data.maxBounces;
            _damage = data.Damage;
            _lifetime = data.projectileLifetime;

            if (rb != null)
            {
                rb.linearVelocity = transform.forward * data.projectileSpeed;
            }

            Invoke(nameof(AutoDestroy), _lifetime);
        }

        private void OnCollisionEnter(Collision col)
        {
            // Don't collide with owner
            if (col.gameObject == _owner) return;

            // Check for IDamageable
            if (col.gameObject.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(_damage, DamageType.Bullet, _weaponData);
                CancelInvoke();
                Destroy(gameObject);
                return;
            }

            _bounceCount++;
            if (_bounceCount > _maxBounces)
            {
                Destroy(gameObject);
                return;
            }

            // Physics reflection (Rigidbody will handle position, we just update velocity)
            Vector3 normal = col.contacts[0].normal;
            
            // Note: Rigidbody velocity might have already changed due to physics collision
            // but the user's snippet explicitly sets it.
            // rb.linearVelocity = Vector3.Reflect(rb.linearVelocity, normal);
            
            // Actually, if it's a non-trigger collision with 1.0 bounciness, 
            // the physics engine handles the reflection. 
            // But I'll follow the user's code snippet logic.
            
            if (bounceSparkVFX != null)
            {
                Instantiate(bounceSparkVFX, col.contacts[0].point, Quaternion.LookRotation(normal));
            }
        }

        private void AutoDestroy() => Destroy(gameObject);
    }
}
