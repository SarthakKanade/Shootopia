using UnityEngine;
using VelocityWars.Core;
using VelocityWars.Player;
using VelocityWars;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float _maxHp = 100f;
    [SerializeField] private FloatGameEvent _onHpChanged;
    [SerializeField] private GameEvent _onPlayerDied;

    [SerializeField] private float _currentHp;
    private VelocityGaugeManager _gaugeManager;

    void Awake()
    {
        _gaugeManager = GetComponent<VelocityGaugeManager>();
    }

    void Start()
    {
        _currentHp = _maxHp;
        Debug.Log($"[PlayerHealth] Start: Raising OnHpChanged with {_currentHp} (Max: {_maxHp})");
        _onHpChanged?.Raise(_currentHp);
    }

    public void TakeDamage(float amount, DamageType type = DamageType.Projectile, WeaponSO weaponUsed = null)
    {
        if (_currentHp <= 0) return;

        _currentHp = Mathf.Max(_currentHp - amount, 0f);
        _onHpChanged?.Raise(_currentHp);

        if (_currentHp <= 0f)
        {
            if (_gaugeManager != null)
            {
                _gaugeManager.OnPlayerDied(type);
            }
            _onPlayerDied?.Raise();
            
            // Set object inactive, LivesManager will handle respawn
            gameObject.SetActive(false);
        }
    }

    public void Heal(float amount)
    {
        if (_currentHp <= 0) return;
        
        _currentHp = Mathf.Min(_currentHp + amount, _maxHp);
        _onHpChanged?.Raise(_currentHp);
    }

    public float GetCurrentHp() => _currentHp;
    public float GetMaxHp() => _maxHp;
}
