using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// Attached to the physical force field sphere GameObject.
    /// Implements IDamageable so incoming projectiles, hitscan raycasts, and melee hits
    /// impact rigid on the outer force field surface and send networked damage absorption to PlayerShield.
    /// </summary>
    public class ShieldColliderReceiver : MonoBehaviour, IDamageable
    {
        [SerializeField] private PlayerShield ownerShield;

        public PlayerShield OwnerShield => ownerShield;

        public void Initialize(PlayerShield shield)
        {
            ownerShield = shield;
        }

        public void TakeDamage(float amount, DamageType type = DamageType.Projectile, WeaponSO weaponUsed = null)
        {
            if (ownerShield != null)
            {
                ownerShield.TakeDamage(amount);
            }
        }
    }
}
