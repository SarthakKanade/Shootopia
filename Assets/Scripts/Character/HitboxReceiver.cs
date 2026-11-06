using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// Attached to PlayerHitbox child collider object.
    /// Implements IDamageable so incoming projectiles, hitscan sweeps, and melee hits
    /// immediately find IDamageable directly on the child collider (satisfying TryGetComponent<IDamageable>)
    /// and seamlessly forward damage to the parent PlayerHealth.
    /// </summary>
    public class HitboxReceiver : MonoBehaviour, IDamageable
    {
        [SerializeField] private PlayerHealth health;

        public PlayerHealth Health => health != null ? health : (health = GetComponentInParent<PlayerHealth>());

        public void Initialize(PlayerHealth h)
        {
            health = h;
        }

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponentInParent<PlayerHealth>();
            }
        }

        public void TakeDamage(float amount, DamageType type = DamageType.Projectile, WeaponSO weaponUsed = null)
        {
            if (Health != null)
            {
                Health.TakeDamage(amount, type, weaponUsed);
            }
        }
    }
}
