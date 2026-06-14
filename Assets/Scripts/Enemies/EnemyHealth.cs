using UnityEngine;
using VelocityWars.Core;
using VelocityWars.Enemies;
using VelocityWars.Player;
using VelocityWars;

public class EnemyHealth : MonoBehaviour, IDamageable, IKillable
{
    [SerializeField] private EnemySO _config;
    [SerializeField] private FloatGameEvent _onEnemyKilledWithGauge;

    private float _currentHealth;
    private WeaponSO _lastWeaponUsed;

    private void Awake()
    {
        if (_config != null)
        {
            _currentHealth = _config.maxHealth;
        }
    }

    private void Start()
    {
        GameManager.Instance.RegisterEnemy();
    }

    public void TakeDamage(float amount, DamageType type = DamageType.Projectile, WeaponSO weaponUsed = null)
    {
        if (weaponUsed != null)
        {
            _lastWeaponUsed = weaponUsed;
        }

        if (TryGetComponent(out Robot robot))
        {
            if (robot.IsFrozen)
            {
                amount *= 2f;
                // Optional: Add some visual or console feedback for double damage
                Debug.Log($"<color=cyan>CRITICAL HIT!</color> Frozen target took double damage: {amount}");
            }
            robot.BreakStatus();
        }

        _currentHealth -= amount;

        if (_config != null && _config.hitVFXPrefab != null)
        {
            Instantiate(_config.hitVFXPrefab, transform.position, Quaternion.identity);
        }

        if (_currentHealth <= 0)
        {
            OnDeath();
        }
    }

    public void OnDeath()
    {
        if (_config != null && _config.deathVFXPrefab != null)
        {
            Instantiate(_config.deathVFXPrefab, transform.position, Quaternion.identity);
        }

        if (_onEnemyKilledWithGauge != null)
        {
            _onEnemyKilledWithGauge.Raise(_lastWeaponUsed != null ? _lastWeaponUsed.gaugeKillFill : 0f);
        }

        GameManager.Instance.EnemyKilled();
Destroy(gameObject);
    }

    // Keep legacy support for any script still using int version until fully migrated
    public void TakeDamage(int amount)
    {
        TakeDamage((float)amount);
    }

    public void SelfDestruct()
    {
        OnDeath();
    }
}
