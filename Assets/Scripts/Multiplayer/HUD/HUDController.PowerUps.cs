using UnityEngine;

namespace Shootopia
{
    public partial class HUDController
    {
        private void UpdatePowerUpTimers()
        {
            // 1. Force Field Shield
            if (_localShield != null && _localShield.IsShieldActive)
            {
                if (shieldBarRoot != null && !shieldBarRoot.activeSelf) shieldBarRoot.SetActive(true);

                float curHp = _localShield.CurrentShieldHp;
                float maxHp = _localShield.MaxShieldHp;
                float timeRem = _localShield.ShieldTimeRemaining;
                float maxDur = _localShield.MaxShieldDuration;

                if (shieldFill != null)
                {
                    shieldFill.fillAmount = maxDur > 0f ? Mathf.Clamp01(timeRem / maxDur) : 0f;
                }

                if (shieldText != null)
                {
                    string timeStr = timeRem > 0.05f ? $"{timeRem:F1}s" : "EXPIRING";
                    shieldText.text = $"<color=#00F0FF>SHIELD</color>  <b>{Mathf.RoundToInt(curHp)}</b>/{Mathf.RoundToInt(maxHp)} HP  •  {timeStr}";
                }

                // Pulse warning if <= 3.0s left
                if (timeRem <= 3.0f && shieldCanvasGroup != null)
                {
                    float pulse = 0.6f + 0.4f * Mathf.Sin(Time.time * 10f);
                    shieldCanvasGroup.alpha = pulse;
                }
                else if (shieldCanvasGroup != null)
                {
                    shieldCanvasGroup.alpha = 1f;
                }
            }
            else
            {
                if (shieldBarRoot != null && shieldBarRoot.activeSelf) shieldBarRoot.SetActive(false);
            }

            // 2. Invisibility Cloak
            if (_localInvisibility != null && _localInvisibility.IsInvisible)
            {
                if (invisibilityBarRoot != null && !invisibilityBarRoot.activeSelf) invisibilityBarRoot.SetActive(true);

                float timeRem = _localInvisibility.InvisibilityTimeRemaining;
                float totalDur = _localInvisibility.InvisibilityTotalDuration;

                if (invisibilityFill != null)
                {
                    invisibilityFill.fillAmount = totalDur > 0f ? Mathf.Clamp01(timeRem / totalDur) : 0f;
                }

                if (invisibilityText != null)
                {
                    string timeStr = timeRem > 0.05f ? $"{timeRem:F1}s" : "EXPIRING";
                    invisibilityText.text = $"<color=#D47BFF>STEALTH ACTIVE</color>  •  <b>{timeStr}</b>";
                }

                // Pulse warning if <= 2.5s left
                if (timeRem <= 2.5f && invisibilityCanvasGroup != null)
                {
                    float pulse = 0.6f + 0.4f * Mathf.Sin(Time.time * 12f);
                    invisibilityCanvasGroup.alpha = pulse;
                }
                else if (invisibilityCanvasGroup != null)
                {
                    invisibilityCanvasGroup.alpha = 1f;
                }
            }
            else
            {
                if (invisibilityBarRoot != null && invisibilityBarRoot.activeSelf) invisibilityBarRoot.SetActive(false);
            }

            // 3. Roll Cooldown
            if (_localMotor != null && _localMotor.RollCooldownRemaining > 0f)
            {
                if (rollBarRoot != null && !rollBarRoot.activeSelf) rollBarRoot.SetActive(true);

                float timeRem = _localMotor.RollCooldownRemaining;
                float totalDur = _localMotor.RollCooldown;

                if (rollFill != null)
                {
                    rollFill.fillAmount = totalDur > 0f ? Mathf.Clamp01(1f - (timeRem / totalDur)) : 1f;
                }

                if (rollText != null)
                {
                    rollText.text = $"<color=#2BFFA3>ROLL</color>  COOLDOWN  •  <b>{timeRem:F1}s</b>";
                }

                if (rollCanvasGroup != null)
                {
                    rollCanvasGroup.alpha = 1f;
                }
            }
            else
            {
                if (rollBarRoot != null && rollBarRoot.activeSelf) rollBarRoot.SetActive(false);
            }
        }
    }
}

