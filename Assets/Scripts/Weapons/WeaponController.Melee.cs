using System.Collections;
using UnityEngine;

namespace Shootopia
{
    public partial class WeaponController
    {
        public void PerformMeleeHit(float damage = 25f, WeaponSO weaponUsed = null)
        {
            if (!IsOwner) return;

            Vector3 origin = transform.position + Vector3.up * 1.2f;
            Vector3 aimDir = (Aim != null && Aim.AimPoint != Vector3.zero) ? (Aim.AimPoint - origin).normalized : transform.forward;
            if (aimDir.sqrMagnitude < 0.001f) aimDir = transform.forward;

            float castRadius = (weaponUsed == null) ? 0.45f : 0.55f;
            float castDistance = (weaponUsed == null) ? 1.2f : 1.6f;

            RaycastHit[] hits = Physics.SphereCastAll(origin, castRadius, aimDir, castDistance, hitMask, QueryTriggerInteraction.Ignore);

            foreach (var hit in hits)
            {
                if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform)) continue;

                var targetHealth = hit.collider.GetComponentInParent<PlayerHealth>() ?? hit.collider.GetComponent<PlayerHealth>();
                var damageable = hit.collider.GetComponent<IDamageable>() ?? (IDamageable)targetHealth ?? hit.collider.GetComponentInParent<IDamageable>();

                if (damageable != null)
                {
                    if (targetHealth != null) targetHealth.LastAttackerClientId = OwnerClientId;
                    damageable.TakeDamage(damage, DamageType.Melee, weaponUsed);
                    break;
                }
            }
        }

        public void OnMeleeHit()
        {
            float damage = (CurrentAttach?.gameplayData != null && CurrentAttach.gameplayData.Damage > 0)
                ? CurrentAttach.gameplayData.Damage
                : 25f;
            PerformMeleeHit(damage, CurrentAttach?.gameplayData);
        }

        private IEnumerator DelayedMeleeHitRoutine(float delaySeconds, float damage, WeaponSO weaponUsed)
        {
            yield return new WaitForSeconds(delaySeconds);
            PerformMeleeHit(damage, weaponUsed);
        }

        private IEnumerator TriggerMeleeAnimation(float duration)
        {
            if (animator == null) yield break;
            animator.SetBool(HashIsFiring, true);
            animator.SetFloat(HashShootSpeed, 1f);
            yield return new WaitForSeconds(duration);
            animator.SetBool(HashIsFiring, false);
        }
    }
}

