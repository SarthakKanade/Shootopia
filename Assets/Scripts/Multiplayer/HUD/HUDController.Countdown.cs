using System.Collections;
using UnityEngine;
using TMPro;

namespace Shootopia
{
    public partial class HUDController
    {
        private void HandleCountdownTick(int countdown)
        {
            EnsurePreMatchCountdownUI();
            if (preMatchRoot == null || preMatchCountdownText == null) return;

            if (_preMatchRoutine != null) StopCoroutine(_preMatchRoutine);
            _preMatchRoutine = StartCoroutine(CountdownAnimateRoutine(countdown));
        }

        private IEnumerator CountdownAnimateRoutine(int countdown)
        {
            if (countdown > 0)
            {
                preMatchRoot.SetActive(true);
                if (preMatchGroup != null) preMatchGroup.alpha = 1f;

                if (preMatchSubText != null)
                {
                    preMatchSubText.text = "MATCH STARTING IN";
                    preMatchSubText.color = new Color(0.7f, 0.9f, 1.0f, 0.95f);
                }

                preMatchCountdownText.text = countdown.ToString();
                preMatchCountdownText.fontSize = 110f;

                Color numColor;
                switch (countdown)
                {
                    case 3:
                        numColor = new Color(0.0f, 0.94f, 1.0f); // Neon Cyan
                        break;
                    case 2:
                        numColor = new Color(1.0f, 0.85f, 0.15f); // Electric Amber
                        break;
                    case 1:
                        numColor = new Color(1.0f, 0.28f, 0.28f); // High-Alert Crimson
                        break;
                    default:
                        numColor = new Color(0.0f, 0.94f, 1.0f);
                        break;
                }
                preMatchCountdownText.color = numColor;

                if (countdownBeepClip != null && _audioSource != null)
                {
                    float originalPitch = _audioSource.pitch;
                    _audioSource.pitch = countdown == 1 ? 1.25f : (countdown == 2 ? 1.12f : 1.0f);
                    _audioSource.PlayOneShot(countdownBeepClip, 0.9f);
                    _audioSource.pitch = originalPitch;
                }

                // Punch scale with smooth spring bounce
                var rt = preMatchCountdownText.GetComponent<RectTransform>();
                if (rt != null)
                {
                    float popDuration = 0.28f;
                    float elapsed = 0f;
                    while (elapsed < popDuration)
                    {
                        elapsed += Time.deltaTime;
                        float t = Mathf.Clamp01(elapsed / popDuration);
                        float scale = 1f + (1f - t) * 0.55f * Mathf.Cos(t * Mathf.PI * 1.5f);
                        rt.localScale = Vector3.one * Mathf.Max(scale, 1f);
                        yield return null;
                    }
                    rt.localScale = Vector3.one;
                }

                yield return new WaitForSeconds(0.6f);
            }
            else if (countdown == 0)
            {
                preMatchRoot.SetActive(true);
                if (preMatchGroup != null) preMatchGroup.alpha = 1f;

                if (preMatchSubText != null)
                {
                    preMatchSubText.text = "ENGAGE ALL HOSTILES";
                    preMatchSubText.color = new Color(1.0f, 0.92f, 0.5f, 0.95f);
                }

                preMatchCountdownText.text = "FIGHT!";
                preMatchCountdownText.fontSize = 100f;
                preMatchCountdownText.color = new Color(1.0f, 0.88f, 0.1f); // Radiant Gold

                if (fightHornClip != null && _audioSource != null)
                {
                    _audioSource.PlayOneShot(fightHornClip, 1.0f);
                }

                // Impact punch scale for FIGHT!
                var rt = preMatchCountdownText.GetComponent<RectTransform>();
                if (rt != null)
                {
                    float popDuration = 0.38f;
                    float elapsed = 0f;
                    while (elapsed < popDuration)
                    {
                        elapsed += Time.deltaTime;
                        float t = Mathf.Clamp01(elapsed / popDuration);
                        float scale = 1f + (1f - t) * 0.85f * Mathf.Cos(t * Mathf.PI * 1.5f);
                        rt.localScale = Vector3.one * Mathf.Max(scale, 1f);
                        yield return null;
                    }
                    rt.localScale = Vector3.one;
                }

                yield return new WaitForSeconds(0.65f);

                // Smooth fade out & subtle expansion
                if (preMatchGroup != null)
                {
                    float elapsed = 0f;
                    float dur = 0.4f;
                    Vector3 startScale = preMatchRoot.transform.localScale;
                    while (elapsed < dur)
                    {
                        elapsed += Time.deltaTime;
                        float t = Mathf.Clamp01(elapsed / dur);
                        preMatchGroup.alpha = 1f - t;
                        preMatchRoot.transform.localScale = Vector3.Lerp(startScale, startScale * 1.15f, t);
                        yield return null;
                    }
                }

                preMatchRoot.SetActive(false);
                preMatchRoot.transform.localScale = Vector3.one;
                if (preMatchGroup != null) preMatchGroup.alpha = 1f;
            }
            else
            {
                preMatchRoot.SetActive(false);
            }
        }
    }
}

