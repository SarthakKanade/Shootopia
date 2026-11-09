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
            System.Collections.Generic.HashSet<PlayerHealth> processedPlayers = new System.Collections.Generic.HashSet<PlayerHealth>();
            foreach (Collider c in hits)
            {
                if (c == null) continue;
                IDamageable t = null;
                PlayerHealth ph = c.GetComponentInParent<PlayerHealth>() ?? c.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    if (!processedPlayers.Add(ph)) continue;
                    t = ph;
                }
                else
                {
                    t = c.GetComponentInParent<IDamageable>();
                    if (t == null || !processed.Add(t)) continue;
                }

                // Use center of collider for distance check if possible, otherwise transform position
                Vector3 targetPos = c.bounds.center;
                float dist = Vector3.Distance(transform.position, targetPos);
                
                // Check if it's a Robot for status effects (check parent too)
                float normalizedDist = Mathf.Clamp01(dist / (_weaponData.blastRadius > 0.1f ? _weaponData.blastRadius : 3.5f));
                float maxDamage = _weaponData.Damage;
                float minDamage = _weaponData.splashDamage > 0f ? _weaponData.splashDamage : (maxDamage * 0.25f);
                bool isDirectHit = dist <= 0.45f;
                float calculatedDamage = isDirectHit ? maxDamage : Mathf.Lerp(maxDamage, minDamage, normalizedDist);

                DealDamage(t, calculatedDamage, DamageType.Projectile);

                if (IsOwnerProjectile())
                {
                    PlayerHealth targetHealth = ph;
                    if (targetHealth == null && t is MonoBehaviour mbTarget)
                    {
                        targetHealth = mbTarget.GetComponentInParent<PlayerHealth>() ?? mbTarget.GetComponent<PlayerHealth>();
                    }

                    if (targetHealth != null)
                    {
                        var targetShield = targetHealth.GetComponent<PlayerShield>();
                        bool shieldBlocked = targetShield != null && (targetShield.IsShieldActive || targetShield.WasActiveOrAbsorbedRecently);

                        if (!shieldBlocked && !targetHealth.IsInvincible)
                        {
                            float baseDuration = (_weaponData != null && _weaponData.freezeDuration > 0f) ? _weaponData.freezeDuration : 2.5f;
                            if (isDirectHit)
                            {
                                // Direct hit at POC: Completely freezes player for baseDuration
                                targetHealth.ApplyFreezeEffect(baseDuration, 1.0f, true);
                            }
                            else
                            {
                                // Splash hit: Speed slow depends on distance from POC
                                float maxSlow = _weaponData != null && _weaponData.maxSlowPercent > 0f ? _weaponData.maxSlowPercent : 0.80f;
                                float minSlow = _weaponData != null && _weaponData.minSlowPercent > 0f ? _weaponData.minSlowPercent : 0.20f;
                                float slowPercent = Mathf.Lerp(maxSlow, minSlow, normalizedDist);
                                float slowDuration = Mathf.Lerp(baseDuration, 1.0f, normalizedDist);

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
