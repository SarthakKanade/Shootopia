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
        // ──────────────────────── DYNAMIC ELIMINATION & DEATH BANNERS ────────────────────────

        public void ShowEliminationBanner(string victimName, string weaponName)
        {
            _multiKillCount++;
            _multiKillTimer = MultiKillWindow;

            if (killSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(_multiKillCount > 1 && headshotKillSound != null ? headshotKillSound : killSound, 1.0f);
            }

            if (eliminationBannerRoot == null) return;
            if (_eliminationBannerRoutine != null) StopCoroutine(_eliminationBannerRoutine);
            _eliminationBannerRoutine = StartCoroutine(EliminationBannerRoutine(victimName, weaponName, _multiKillCount));
        }

        private IEnumerator EliminationBannerRoutine(string victimName, string weaponName, int streak)
        {
            string streakTitle = "ELIMINATED";
            string xpAmount = "+100";
            if (streak == 2) { streakTitle = "DOUBLE KILL!"; xpAmount = "+200"; }
            else if (streak == 3) { streakTitle = "TRIPLE KILL!"; xpAmount = "+300"; }
            else if (streak >= 4) { streakTitle = "RAMPAGE!"; xpAmount = "+500"; }

            if (eliminationXpText != null)
            {
                eliminationXpText.text = $"{xpAmount} PTS";
                eliminationXpText.color = new Color(1.0f, 0.85f, 0.2f);
            }

            if (eliminationTitleText != null)
            {
                eliminationTitleText.text = streakTitle;
                eliminationTitleText.color = streak > 1 ? new Color(1.0f, 0.85f, 0.2f) : new Color(0.0f, 0.94f, 1.0f);
            }

            if (eliminationSubtitleText != null)
            {
                string wpn = string.IsNullOrEmpty(weaponName) ? "" : $" • <color=#8A99B5>{weaponName.ToUpper()}</color>";
                eliminationSubtitleText.text = $"<color=#FFFFFF>{victimName.ToUpper()}</color>{wpn}";
            }

            if (eliminationIcon != null && skullSprite != null)
            {
                eliminationIcon.sprite = skullSprite;
            }

            eliminationBannerRoot.SetActive(true);
            if (eliminationBannerGroup != null) eliminationBannerGroup.alpha = 1f;

            var rt = eliminationBannerRoot.GetComponent<RectTransform>();
            if (rt != null) UIJuice.PunchScale(rt, 0.32f);

            yield return new WaitForSeconds(2.0f);

            if (eliminationBannerGroup != null)
            {
                float duration = 0.35f;
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    eliminationBannerGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                    yield return null;
                }
            }

            eliminationBannerRoot.SetActive(false);
            if (eliminationBannerGroup != null) eliminationBannerGroup.alpha = 1f;
        }
    }
}
