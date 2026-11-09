using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

namespace Shootopia
{
    public partial class PauseMenuController
    {
        private void ApplyTacticalPauseStyling()
        {
            if (_isTacticalLayoutBuilt || pauseMenuRoot == null) return;
            _isTacticalLayoutBuilt = true;

            TacticalUIBuilder.SetupBackdrop(pauseMenuRoot, new Vector2(1000f, 680f));

            // Reposition / Style Title
            Transform titleTr = pauseMenuRoot.transform.Find("Title");
            if (titleTr != null)
            {
                var tRt = titleTr.GetComponent<RectTransform>();
                if (tRt != null)
                {
                    tRt.anchorMin = new Vector2(0.5f, 0.5f);
                    tRt.anchorMax = new Vector2(0.5f, 0.5f);
                    tRt.sizeDelta = new Vector2(880f, 44f);
                    tRt.anchoredPosition = new Vector2(0f, 275f);
                }

                var tmp = titleTr.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = "TACTICAL PAUSE";
                    tmp.fontSize = 32;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.color = TacticalUIBuilder.TextWhite;
                    tmp.alignment = TextAlignmentOptions.Center;
                    pauseModeTitleText = tmp;
                }
            }

            // Subtitle
            Transform subTr = pauseMenuRoot.transform.Find("PauseSubtitle");
            if (subTr == null)
            {
                var subObj = new GameObject("PauseSubtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
                subObj.transform.SetParent(pauseMenuRoot.transform, false);
                var sRt = subObj.GetComponent<RectTransform>();
                sRt.anchorMin = new Vector2(0.5f, 0.5f);
                sRt.anchorMax = new Vector2(0.5f, 0.5f);
                sRt.sizeDelta = new Vector2(880f, 26f);
                sRt.anchoredPosition = new Vector2(0f, 238f);

                var sTmp = subObj.GetComponent<TextMeshProUGUI>();
                sTmp.text = "LIVE MATCH OVERVIEW • PLAYER LEADERBOARD";
                sTmp.fontSize = 15;
                sTmp.color = TacticalUIBuilder.TextMuted;
                sTmp.alignment = TextAlignmentOptions.Center;
                if (pauseModeTitleText != null) sTmp.font = pauseModeTitleText.font;
                pauseSubtitleText = sTmp;
            }

            // Column Headers
            if (pauseMenuRoot.transform.Find("ScoreTableHeader") == null)
            {
                var headerObj = new GameObject("ScoreTableHeader", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
                headerObj.transform.SetParent(pauseMenuRoot.transform, false);
                var hRt = headerObj.GetComponent<RectTransform>();
                hRt.anchorMin = new Vector2(0.5f, 0.5f);
                hRt.anchorMax = new Vector2(0.5f, 0.5f);
                hRt.sizeDelta = new Vector2(880f, 36f);
                hRt.anchoredPosition = new Vector2(0f, 195f);
                headerObj.GetComponent<Image>().color = TacticalUIBuilder.ThemeHeaderBg;

                var hlg = headerObj.GetComponent<HorizontalLayoutGroup>();
                hlg.padding = new RectOffset(24, 24, 4, 4);
                hlg.childAlignment = TextAnchor.MiddleLeft;
                hlg.childControlWidth = false;
                hlg.childControlHeight = true;
                hlg.spacing = 10;

                var font = pauseModeTitleText != null ? pauseModeTitleText.font : null;
                TacticalUIBuilder.CreateHeaderCol("RankHeader", headerObj.transform, "RANK", 65f, TextAlignmentOptions.MidlineLeft, font);
                TacticalUIBuilder.CreateHeaderCol("PlayerHeader", headerObj.transform, "PLAYER", 320f, TextAlignmentOptions.MidlineLeft, font);
                TacticalUIBuilder.CreateHeaderCol("KDHeader", headerObj.transform, "KILLS / DEATHS", 195f, TextAlignmentOptions.Center, font);
                TacticalUIBuilder.CreateHeaderCol("ScoreHeader", headerObj.transform, "SCORE", 135f, TextAlignmentOptions.MidlineRight, font);
                TacticalUIBuilder.CreateHeaderCol("PingHeader", headerObj.transform, "PING", 95f, TextAlignmentOptions.MidlineRight, font);
            }

            var container = TacticalUIBuilder.EnsureScrollRowsContainer(pauseMenuRoot, 350f, -5f);
            if (container != null) scoreTableContent = container;

            // Action Bar
            Transform actionBarTr = pauseMenuRoot.transform.Find("ActionBar");
            if (actionBarTr == null)
            {
                var barObj = new GameObject("ActionBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                barObj.transform.SetParent(pauseMenuRoot.transform, false);
                var bsRt = barObj.GetComponent<RectTransform>();
                bsRt.anchorMin = new Vector2(0.5f, 0.5f);
                bsRt.anchorMax = new Vector2(0.5f, 0.5f);
                bsRt.sizeDelta = new Vector2(880f, 56f);
                bsRt.anchoredPosition = new Vector2(0f, -265f);

                var hlg = barObj.GetComponent<HorizontalLayoutGroup>();
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.spacing = 20f;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;

                if (btnResume != null) btnResume.transform.SetParent(barObj.transform, false);
                if (btnSettings != null) btnSettings.transform.SetParent(barObj.transform, false);
                if (btnLeave != null) btnLeave.transform.SetParent(barObj.transform, false);

                Transform oldStack = pauseMenuRoot.transform.Find("ButtonStack");
                if (oldStack != null && oldStack != barObj.transform) oldStack.gameObject.SetActive(false);
            }

            StyleActionButton(btnResume, new Vector2(260f, 52f), "RESUME MATCH", new Color(0.2f, 0.8f, 1f, 1f));
            StyleActionButton(btnSettings, new Vector2(220f, 52f), "SETTINGS", new Color(1f, 0.82f, 0.2f, 1f));
            StyleActionButton(btnLeave, new Vector2(220f, 52f), "LEAVE MATCH", new Color(1f, 0.45f, 0.45f, 1f));
        }

        private void StyleActionButton(Button btn, Vector2 size, string text, Color accentColor)
        {
            if (btn == null) return;
            var rt = btn.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = size;

            var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.text = text;
                txt.fontSize = 17;
                txt.fontStyle = FontStyles.Bold;
                txt.color = accentColor;
                txt.alignment = TextAlignmentOptions.Center;
            }

            var img = btn.GetComponent<Image>();
            if (img != null) img.color = new Color(0.08f, 0.12f, 0.22f, 0.95f);
            if (btn.GetComponent<UIJuice>() == null) btn.gameObject.AddComponent<UIJuice>();
        }
    }
}
