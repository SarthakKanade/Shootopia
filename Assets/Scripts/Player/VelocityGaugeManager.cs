using UnityEngine;
using VelocityWars.Core;
using VelocityWars;
using System.Collections;
using StarterAssets;

namespace VelocityWars.Player
{
    public class VelocityGaugeManager : MonoBehaviour, IMomentum
    {
        [Header("Streak settings")]
        [SerializeField] private int _multStartsAtKill = 3;
        [SerializeField] private int _maxStreakCap = 5;
        [SerializeField] private float _multPerStreakLevel = 0.5f;

        [Header("Death penalties (% gauge lost)")]
        [SerializeField] private float _penaltyRobotMelee = 50f;
        [SerializeField] private float _penaltyTurret = 30f;
        [SerializeField] private float _penaltyWeaponless = 20f;
        [SerializeField] private float _penaltyPlayer = 40f;

        [Header("Events")]
        [SerializeField] private FloatGameEvent _onGaugeChanged;
        [SerializeField] private GameEvent _onAbilityReady;
        [SerializeField] private GameEvent _onAbilityFired;
        [SerializeField] private FloatGameEvent _onStreakChanged;

        [Header("Ability")]
        [SerializeField] private AbilitySO _selectedAbility;
        [SerializeField] private VelocityGaugeUI _ui;

        private float _gaugeFill;
        private int _currentStreak;
        private bool _abilityReady;
        private bool _isAbilityActive;
        private StarterAssetsInputs _inputs;

        public float CurrentCharge => _gaugeFill;

        private void Start()
        {
            _inputs = GetComponent<StarterAssetsInputs>();

            // Initialize gauge to 0
            _gaugeFill = 0f;
            _onGaugeChanged?.Raise(_gaugeFill);

            if (_ui != null && _selectedAbility != null)
            {
                _ui.SetAbilityIcon(_selectedAbility.icon);
            }
        }

        void Update()
        {
            if (_inputs != null && _inputs.ability && _abilityReady && !_isAbilityActive)
            {
                _inputs.AbilityInput(false); // Reset input
                StartCoroutine(ExecuteAbilityRoutine());
            }
        }

        private IEnumerator ExecuteAbilityRoutine()
        {
            if (_selectedAbility == null) yield break;

            _isAbilityActive = true;
            _selectedAbility.Activate(gameObject, GameContext.SinglePlayer);
            
            _gaugeFill = 0f;
            _abilityReady = false;
            _onAbilityFired?.Raise();
            _onGaugeChanged?.Raise(_gaugeFill);

            yield return new WaitForSeconds(_selectedAbility.duration);

            _selectedAbility.Deactivate(gameObject);
            _isAbilityActive = false;
        }

        private float GetStreakMultiplier(int streak)
        {
            if (streak < _multStartsAtKill) return 1.0f;
            int levelsAboveStart = Mathf.Min(
                streak - _multStartsAtKill,
                _maxStreakCap - _multStartsAtKill
            );
            return 1.0f + (levelsAboveStart + 1) * _multPerStreakLevel;
        }

        public void AddCharge(float amount)
        {
            if (_abilityReady) return; // no overflow when full
            _gaugeFill = Mathf.Min(_gaugeFill + amount, 100f);
            _onGaugeChanged?.Raise(_gaugeFill);

            if (_gaugeFill >= 100f && !_abilityReady)
            {
                _abilityReady = true;
                _onAbilityReady?.Raise();
            }
        }

        public void OnEnemyKilled(WeaponSO weaponUsed)
        {
            float fill = weaponUsed != null ? weaponUsed.gaugeKillFill : 0f;
            OnEnemyKilledWithGauge(fill);
        }

        public void OnEnemyKilledWithGauge(float gaugeFill)
        {
            _currentStreak++;
            float mult = GetStreakMultiplier(_currentStreak);
            float fill = gaugeFill * mult;

            AddCharge(fill);
            _onStreakChanged?.Raise((float)_currentStreak);
        }

        public void ConsumeCharge(float amount)
{
            _gaugeFill = Mathf.Max(_gaugeFill - amount, 0f);
            if (_gaugeFill < 100f && _abilityReady)
            {
                _abilityReady = false;
                _onAbilityFired?.Raise(); // Or define a specific "not ready" event
            }
            _onGaugeChanged?.Raise(_gaugeFill);
        }

        public void OnPlayerDied(DamageType causeOfDeath)
        {
            float penalty = causeOfDeath switch
            {
                DamageType.Melee => _penaltyRobotMelee,
                DamageType.Projectile => _penaltyTurret,
                DamageType.Environment or DamageType.Explosion => _penaltyWeaponless,
                _ => _penaltyPlayer
            };
            _currentStreak = 0;
            ConsumeCharge(penalty);
            _onStreakChanged?.Raise((float)_currentStreak);
        }
    }
}