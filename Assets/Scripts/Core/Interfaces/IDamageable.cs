namespace VelocityWars.Core
{
    using UnityEngine;
    using VelocityWars;

    public enum DamageType { Melee, Projectile, Environment, Player, Explosion, Self, Bullet }

    /// <summary>
    /// Implement on any object that can receive damage.
    /// Use TryGetComponent instead of GetComponent.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Raw damage before any resistance calculation
        /// Used for future armour/resistance logic
        /// </summary>
        void TakeDamage(float amount, DamageType type = DamageType.Projectile, WeaponSO weaponUsed = null);
    }
}
