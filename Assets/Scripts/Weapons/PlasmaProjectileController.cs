using UnityEngine;
using Shootopia;
using Unity.Netcode;

namespace Shootopia
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
            var t = col.GetComponentInParent<IDamageable>() ?? col.GetComponent<IDamageable>();
            if (t != null)
            {
                DealDamage(t, 18f, DamageType.Bullet, col);
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
