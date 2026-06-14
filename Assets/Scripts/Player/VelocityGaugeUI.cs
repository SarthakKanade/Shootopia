using UnityEngine;
using UnityEngine.UI;
using VelocityWars.Core;

namespace VelocityWars.Player
{
    public class VelocityGaugeUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image _gaugeFillBar;
        [SerializeField] private Image _abilityIcon;
        [SerializeField] private GameObject _readyIndicator;

        public void OnGaugeChanged(float percent)
        {
            Debug.Log($"[VelocityGaugeUI] Gauge Percent Received: {percent}%");
            if (_gaugeFillBar != null)
            {
                // Convert 0-100 scale to 0-1 for fillAmount
                float fill = Mathf.Clamp01(percent / 100f);
                _gaugeFillBar.fillAmount = fill;
                
                // Extra safety: If sprite is null, "Filled" mode might fail to visually update.
                if (_gaugeFillBar.sprite == null)
                {
                    Debug.LogWarning("[VelocityGaugeUI] Warning: _gaugeFillBar has no Sprite! 'Filled' mode requires a sprite to work.");
                }

                Debug.Log($"[VelocityGaugeUI] Gauge updated: {percent}% (FillAmount set to: {fill})");
            }
            else
            {
                Debug.LogError("[VelocityGaugeUI] Error: _gaugeFillBar is not assigned!");
            }
        }

        public void OnAbilityReady()
        {
            if (_readyIndicator != null)
            {
                _readyIndicator.SetActive(true);
            }
        }

        public void OnAbilityFired()
        {
            if (_readyIndicator != null)
            {
                _readyIndicator.SetActive(false);
            }
        }

        public void SetAbilityIcon(Sprite icon)
        {
            if (_abilityIcon != null && icon != null)
            {
                _abilityIcon.sprite = icon;
                _abilityIcon.enabled = true;
            }
            else if (_abilityIcon != null)
            {
                _abilityIcon.enabled = false;
            }
        }
    }
}