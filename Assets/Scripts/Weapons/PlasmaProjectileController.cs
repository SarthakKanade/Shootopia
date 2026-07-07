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
            if (col.TryGetComponent(out IDamageable t))
            {
                if (IsOwnerProjectile())
                {
                    ulong attackerId = Unity.Netcode.NetworkManager.ServerClientId;
                    if (_owner != null)
                    {
                        var netObj = _owner.GetComponent<NetworkObject>();
                        if (netObj != null) attackerId = netObj.OwnerClientId;
                    }

                    var targetHealth = col.GetComponentInParent<PlayerHealth>();
                    if (targetHealth == null) targetHealth = col.GetComponent<PlayerHealth>();
                    if (targetHealth != null)
                    {
                        targetHealth.LastAttackerClientId = attackerId;
                    }

                    t.TakeDamage(18f, DamageType.Bullet, _weaponData);
                }
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
