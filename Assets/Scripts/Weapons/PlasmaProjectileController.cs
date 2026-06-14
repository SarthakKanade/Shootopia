using UnityEngine;
using VelocityWars.Core;

namespace VelocityWars
{
    public class PlasmaProjectileController : BaseProjectile
    {
        [SerializeField] private GameObject hitSparkVFX;
        private Rigidbody rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        void Start()
        {
            if (rb != null)
            {
                rb.linearVelocity = transform.forward * 28f;
            }
            Destroy(gameObject, 4f);
        }

        private void OnTriggerEnter(Collider col)
        {
            if (col.TryGetComponent(out IDamageable t))
            {
                t.TakeDamage(18f, DamageType.Bullet);
                if (hitSparkVFX != null)
                {
                    Instantiate(hitSparkVFX, transform.position, Quaternion.identity);
                }
                Destroy(gameObject);
            }
            else if (!col.isTrigger)
            {
                if (hitSparkVFX != null)
                {
                    Instantiate(hitSparkVFX, transform.position, Quaternion.identity);
                }
                Destroy(gameObject);
            }
        }
    }
}
