using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Shootopia
{
    public partial class HUDController
    {
        public void ShowDeathBanner(string killerName, string weaponName)
        {
            if (deathSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(deathSound, 0.9f);
            }

            if (deathBannerRoot == null) return;
            if (_deathBannerRoutine != null) StopCoroutine(_deathBannerRoutine);
            _deathBannerRoutine = StartCoroutine(DeathBannerRoutine(killerName, weaponName));
        }

        private void HandleLocalPlayerDied(float respawnDuration)
        {
            if (deathBannerRoot != null && !deathBannerRoot.activeSelf)
            {
                ShowDeathBanner("Hazard", "Environment");
            }
        }

        private void HandleLocalPlayerRespawned()
        {
            _isWaitingForRespawn = false;
        }

        private IEnumerator DeathBannerRoutine(string killerName, string weaponName)
        {
            EnsureDeathBannerCountdownUI();

            if (deathTitleText != null)
            {
                deathTitleText.text = "YOU WERE ELIMINATED";
                deathTitleText.color = new Color(1f, 0.3f, 0.3f);
            }

            if (deathSubtitleText != null)
            {
                string wpn = string.IsNullOrEmpty(weaponName) ? "" : $" WITH <color=#FFE066>{weaponName.ToUpper()}</color>";
                deathSubtitleText.text = $"KILLED BY <color=#FFFFFF>{killerName.ToUpper()}</color>{wpn}";
            }

            if (deathIcon != null && skullSprite != null)
            {
                deathIcon.sprite = skullSprite;
                deathIcon.color = new Color(1f, 0.3f, 0.3f, 0.9f);
            }

            deathBannerRoot.SetActive(true);
            if (deathBannerGroup != null) deathBannerGroup.alpha = 1f;

            var rt = deathBannerRoot.GetComponent<RectTransform>();
            if (rt != null) UIJuice.PunchScale(rt, 0.25f);

            float respawnDuration = 3.0f;
            float elapsed = 0f;
            _isWaitingForRespawn = true;

            while (elapsed < respawnDuration && _isWaitingForRespawn)
            {
                elapsed += Time.deltaTime;
                float remaining = Mathf.Max(0f, respawnDuration - elapsed);
                int seconds = Mathf.CeilToInt(remaining);

                if (respawnCountdownText != null)
                {
                    respawnCountdownText.text = $"RESPAWNING IN  <color=#FFE066><b>{seconds}</b></color>";
                }
                if (respawnProgressBarFill != null)
                {
                    respawnProgressBarFill.fillAmount = Mathf.Clamp01(1f - (elapsed / respawnDuration));
                }
                yield return null;
            }

            if (respawnCountdownText != null)
            {
                respawnCountdownText.text = "<color=#00F0FF><b>DEPLOYING...</b></color>";
            }
            if (respawnProgressBarFill != null)
            {
                respawnProgressBarFill.fillAmount = 0f;
            }

            if (respawnClip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(respawnClip, 0.9f);
            }

            yield return new WaitForSeconds(0.25f);

            if (deathBannerGroup != null)
            {
                float duration = 0.35f;
                float fadeElapsed = 0f;
                while (fadeElapsed < duration)
                {
                    fadeElapsed += Time.deltaTime;
                    deathBannerGroup.alpha = Mathf.Lerp(1f, 0f, fadeElapsed / duration);
                    yield return null;
                }
            }

            deathBannerRoot.SetActive(false);
            if (deathBannerGroup != null) deathBannerGroup.alpha = 1f;
            _isWaitingForRespawn = false;
        }

        private void EnsureDeathBannerCountdownUI()
        {
            if (deathBannerRoot == null) return;

            // Lower DeathBanner a little down from y = 150 to y = 95
            var rt = deathBannerRoot.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(0f, 95f);
                if (rt.sizeDelta.y < 120f)
                {
                    rt.sizeDelta = new Vector2(Mathf.Max(rt.sizeDelta.x, 420f), 120f);
                }
            }

            // Adjust Title & Subtitle vertical anchors so there is generous breathing room above countdown box
            if (deathTitleText != null)
            {
                var titleRt = deathTitleText.rectTransform;
                titleRt.anchorMin = new Vector2(0.18f, 0.58f);
                titleRt.anchorMax = new Vector2(0.92f, 0.90f);
            }
            if (deathSubtitleText != null)
            {
                var subRt = deathSubtitleText.rectTransform;
                subRt.anchorMin = new Vector2(0.08f, 0.35f);
                subRt.anchorMax = new Vector2(0.92f, 0.58f);
            }
            if (deathIcon != null)
            {
                var iconRt = deathIcon.rectTransform;
                iconRt.anchorMin = new Vector2(0.10f, 0.74f);
                iconRt.anchorMax = new Vector2(0.10f, 0.74f);
            }

            var existing = deathBannerRoot.transform.Find("RespawnCountdownBox");
            if (existing != null)
            {
                respawnCountdownRoot = existing.gameObject;
                var existBoxRt = existing.GetComponent<RectTransform>();
                if (existBoxRt != null)
                {
                    existBoxRt.anchoredPosition = new Vector2(0f, 6f);
                }
                respawnCountdownText = existing.Find("CountdownText")?.GetComponent<TextMeshProUGUI>();
                respawnProgressBarFill = existing.Find("ProgressBar/Fill")?.GetComponent<Image>();
                return;
            }

            var boxObj = new GameObject("RespawnCountdownBox", typeof(RectTransform));
            boxObj.transform.SetParent(deathBannerRoot.transform, false);
            var boxRt = boxObj.GetComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0.05f, 0f);
            boxRt.anchorMax = new Vector2(0.95f, 0f);
            boxRt.pivot = new Vector2(0.5f, 0f);
            boxRt.anchoredPosition = new Vector2(0f, 6f);
            boxRt.sizeDelta = new Vector2(0f, 32f);

            var txtObj = new GameObject("CountdownText", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(boxObj.transform, false);
            var txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = new Vector2(0f, 0.32f);
            txtRt.anchorMax = new Vector2(1f, 1f);
            txtRt.anchoredPosition = Vector2.zero;
            txtRt.sizeDelta = Vector2.zero;
            respawnCountdownText = txtObj.GetComponent<TextMeshProUGUI>();
            respawnCountdownText.text = "RESPAWNING IN  3";
            respawnCountdownText.fontSize = 15;
            respawnCountdownText.fontStyle = FontStyles.Bold;
            respawnCountdownText.alignment = TextAlignmentOptions.Center;
            respawnCountdownText.color = new Color(1f, 0.90f, 0.4f, 1f);

            var barBgObj = new GameObject("ProgressBar", typeof(RectTransform), typeof(Image));
            barBgObj.transform.SetParent(boxObj.transform, false);
            var barBgRt = barBgObj.GetComponent<RectTransform>();
            barBgRt.anchorMin = new Vector2(0.12f, 0f);
            barBgRt.anchorMax = new Vector2(0.88f, 0f);
            barBgRt.pivot = new Vector2(0.5f, 0f);
            barBgRt.anchoredPosition = new Vector2(0f, 2f);
            barBgRt.sizeDelta = new Vector2(0f, 4f);
            var barBgImg = barBgObj.GetComponent<Image>();
            barBgImg.color = new Color(0.1f, 0.15f, 0.25f, 0.8f);

            var fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(barBgObj.transform, false);
            var fillRt = fillObj.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.anchoredPosition = Vector2.zero;
            fillRt.sizeDelta = Vector2.zero;
            respawnProgressBarFill = fillObj.GetComponent<Image>();
            respawnProgressBarFill.color = new Color(1.0f, 0.82f, 0.2f, 0.95f);
            respawnProgressBarFill.type = Image.Type.Filled;
            respawnProgressBarFill.fillMethod = Image.FillMethod.Horizontal;
            respawnProgressBarFill.fillAmount = 1f;

            respawnCountdownRoot = boxObj;
        }
    }
}

