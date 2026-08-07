using UnityEngine;
using Shootopia;
using Unity.Netcode;

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
                    if (IsOwnerProjectile())
                    {
                        ulong attackerId = Unity.Netcode.NetworkManager.ServerClientId;
                        if (_owner != null)
                        {
                            var netObj = _owner.GetComponent<NetworkObject>();
                            if (netObj != null) attackerId = netObj.OwnerClientId;
                        }

                        if (t is PlayerHealth ph)
                        {
                            ph.LastAttackerClientId = attackerId;
                        }
                        else if (t is MonoBehaviour mb)
                        {
                            var ph2 = mb.GetComponentInParent<PlayerHealth>();
                            if (ph2 == null) ph2 = mb.GetComponent<PlayerHealth>();
                            if (ph2 != null) ph2.LastAttackerClientId = attackerId;
                        }

                        float normalizedDist = Mathf.Clamp01(dist / (_weaponData.blastRadius > 0.1f ? _weaponData.blastRadius : 3.5f));
                        float maxDamage = _weaponData.Damage;
                        float minDamage = _weaponData.splashDamage > 0f ? _weaponData.splashDamage : (maxDamage * 0.25f);
                        float calculatedDamage = Mathf.Lerp(maxDamage, minDamage, normalizedDist);

                        t.TakeDamage(calculatedDamage, DamageType.Projectile, _weaponData);

                        PlayerHealth targetHealth = t as PlayerHealth;
                        if (targetHealth == null && t is MonoBehaviour mbTarget)
                        {
                            targetHealth = mbTarget.GetComponentInParent<PlayerHealth>();
                            if (targetHealth == null) targetHealth = mbTarget.GetComponent<PlayerHealth>();
                        }

                        if (targetHealth != null)
                        {
                            bool isDirectHit = dist <= 0.45f;
                            if (isDirectHit)
                            {
                                // Direct hit at POC: Completely freezes player for 5 seconds
                                targetHealth.ApplyFreezeEffect(5.0f, 1.0f, true);
                            }
                            else
                            {
                                // Splash hit: Speed slow depends on distance from POC
                                float maxSlow = _weaponData != null && _weaponData.maxSlowPercent > 0f ? _weaponData.maxSlowPercent : 0.80f;
                                float minSlow = _weaponData != null && _weaponData.minSlowPercent > 0f ? _weaponData.minSlowPercent : 0.20f;
                                float slowPercent = Mathf.Lerp(maxSlow, minSlow, normalizedDist);
                                float slowDuration = Mathf.Lerp(3.5f, 1.5f, normalizedDist);

                                targetHealth.ApplyFreezeEffect(slowDuration, slowPercent, false);
                            }
                        }
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
