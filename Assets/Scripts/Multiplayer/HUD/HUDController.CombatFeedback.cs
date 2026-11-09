using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

namespace Shootopia
{
    public partial class HUDController
    {
        // ──────────────────────── COMBAT FEEL / JUICE ────────────────────────

        public void TriggerDamageFlash(float damage)
        {
            if (damageFlashGroup == null) return;
            if (_damageFlashRoutine != null) StopCoroutine(_damageFlashRoutine);
            _damageFlashRoutine = StartCoroutine(DamageFlashRoutine(Mathf.Clamp01(damage / 50f)));
        }

        private IEnumerator DamageFlashRoutine(float intensity)
        {
            damageFlashGroup.alpha = Mathf.Lerp(0.35f, 0.85f, intensity);
            float duration = 0.35f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                damageFlashGroup.alpha = Mathf.Lerp(intensity, 0f, elapsed / duration);
                yield return null;
            }
            damageFlashGroup.alpha = 0f;
        }

        private void OnPlayerHitConfirmed(bool isKill)
        {
            ShowHitMarker(isKill);
        }

        public void ShowHitMarker(bool isKill)
        {
            if (hitSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(hitSound, 0.7f);
            }

            if (hitMarkerRoot == null) return;
            if (_hitMarkerRoutine != null) StopCoroutine(_hitMarkerRoutine);
            _hitMarkerRoutine = StartCoroutine(HitMarkerRoutine(isKill));
        }

        private IEnumerator HitMarkerRoutine(bool isKill)
        {
            hitMarkerRoot.SetActive(true);
            Color tickColor = isKill ? new Color(1f, 0.2f, 0.2f, 1f) : new Color(1f, 1f, 1f, 1f);

            if (hitMarkerTicks != null)
            {
                foreach (var tick in hitMarkerTicks)
                {
                    if (tick != null) tick.color = tickColor;
                }
            }

            float duration = 0.22f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float a = Mathf.Lerp(1f, 0f, elapsed / duration);
                if (hitMarkerTicks != null)
                {
                    foreach (var tick in hitMarkerTicks)
                    {
                        if (tick != null) tick.color = new Color(tickColor.r, tickColor.g, tickColor.b, a);
                    }
                }
                yield return null;
            }

            hitMarkerRoot.SetActive(false);
        }

        // ──────────────────────── LOW-HEALTH SENSORY FEEDBACK (<30% HP) ────────────────────────
    }
}
