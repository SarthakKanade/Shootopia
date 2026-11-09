using UnityEngine;
using Unity.Netcode;

namespace Shootopia
{
    public partial class HUDController
    {
private void UpdateMultiKillTimer()
        {
            if (_multiKillCount > 0)
            {
                _multiKillTimer -= Time.deltaTime;
                if (_multiKillTimer <= 0f)
                {
                    _multiKillCount = 0;
                }
            }
        }

        private void UpdateLocalPlayerReferences()
        {
            if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient == null)
            {
                CleanUpLocalPlayerReferences();
                return;
            }

            var localPlayerObj = NetworkManager.Singleton.LocalClient.PlayerObject;
            if (localPlayerObj == null)
            {
                CleanUpLocalPlayerReferences();
                return;
            }

            if (localPlayerObj == _cachedLocalPlayerObj && _localHealth != null)
            {
                return; // Already resolved and cached! Zero GetComponent allocations.
            }
            _cachedLocalPlayerObj = localPlayerObj;

            var hp = localPlayerObj.GetComponent<PlayerHealth>();
            var wc = localPlayerObj.GetComponent<WeaponController>();
            var shield = localPlayerObj.GetComponent<PlayerShield>();
            var invis = localPlayerObj.GetComponent<PlayerInvisibility>();
            var motor = localPlayerObj.GetComponent<CharacterMotor>();

            if (hp != _localHealth)
            {
                if (_localHealth != null)
                {
                    _localHealth.CurrentHp.OnValueChanged -= OnHealthChanged;
                }
                _localHealth = hp;
                if (_localHealth != null)
                {
                    _localHealth.CurrentHp.OnValueChanged += OnHealthChanged;
                    OnHealthChanged(0f, _localHealth.CurrentHp.Value);
                }
            }

            _localWeaponController = wc;
            _localShield = shield;
            _localInvisibility = invis;
            _localMotor = motor;
        }

        private void CleanUpLocalPlayerReferences()
        {
            if (_localHealth != null)
            {
                _localHealth.CurrentHp.OnValueChanged -= OnHealthChanged;
                _localHealth = null;
            }
            _localWeaponController = null;
            _localShield = null;
            _localInvisibility = null;
            _localMotor = null;
        }

        private void OnHealthChanged(float previousValue, float newValue)
        {
            if (_localHealth == null) return;
            float maxHp = _localHealth.GetMaxHp();
            if (maxHp <= 0) maxHp = 100f;
            float percentage = Mathf.Clamp01(newValue / maxHp);

            if (healthFill != null)
            {
                healthFill.fillAmount = percentage;
            }

            if (healthText != null)
            {
                healthText.text = $"{Mathf.RoundToInt(newValue)} / {Mathf.RoundToInt(maxHp)}";
            }

            // Damage flash vignette
            if (newValue < previousValue && previousValue > 0f)
            {
                float damageTaken = previousValue - newValue;
                TriggerDamageFlash(damageTaken);
            }
        }
    }
}
