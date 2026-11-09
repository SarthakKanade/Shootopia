using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Shootopia
{
    public static class TacticalControlsSheetBuilder
    {
        public static GameObject BuildControlsSheetView(Transform parent, TMP_FontAsset font = null)
        {
            var rootObj = new GameObject("TabContentControls", typeof(RectTransform));
            rootObj.transform.SetParent(parent, false);

            var rRt = rootObj.GetComponent<RectTransform>();
            rRt.anchorMin = Vector2.zero;
            rRt.anchorMax = Vector2.one;
            rRt.sizeDelta = Vector2.zero;
            rRt.anchoredPosition = Vector2.zero;

            var scrollObj = new GameObject("ControlsScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image), typeof(Mask));
            scrollObj.transform.SetParent(rootObj.transform, false);

            var sRt = scrollObj.GetComponent<RectTransform>();
            sRt.anchorMin = Vector2.zero;
            sRt.anchorMax = Vector2.one;
            sRt.sizeDelta = new Vector2(-16f, -16f);
            sRt.anchoredPosition = Vector2.zero;

            var sImg = scrollObj.GetComponent<Image>();
            sImg.color = new Color(0.02f, 0.04f, 0.09f, 0.6f);

            var mask = scrollObj.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            var contentObj = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            contentObj.transform.SetParent(scrollObj.transform, false);

            var cRt = contentObj.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0f, 1f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.sizeDelta = new Vector2(0f, 0f);

            var hlg = contentObj.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(12, 12, 12, 12);
            hlg.spacing = 16f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = false;

            var csf = contentObj.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var sr = scrollObj.GetComponent<ScrollRect>();
            sr.content = cRt;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;

            // Left Card: KEYBOARD & MOUSE
            BuildControlsCategoryCard(contentObj.transform, "KEYBOARD & MOUSE", new Color(0.18f, 0.85f, 1.0f, 1f), new (string, string)[]
            {
                ("Movement", "W, A, S, D"),
                ("Look / Aim", "Mouse Look"),
                ("Sprint / Dash", "Left Shift"),
                ("Jump", "Spacebar"),
                ("Tactical Roll / Evade", "Left Ctrl / C"),
                ("Fire Weapon", "Left Mouse Button"),
                ("Aim Down Sights (ADS)", "Right Mouse Button"),
                ("Melee Strike", "V / Middle Click"),
                ("Reload Weapon", "R"),
                ("Switch Weapon", "1, 2 / Scroll Wheel"),
                ("Deployable Trip Mine", "F"),
                ("Tactical Throwable", "G"),
                ("Kinetic Shield", "E"),
                ("Optical Invisibility", "Q"),
                ("Tactical Scoreboard", "Tab (Hold)"),
                ("Tactical Pause Menu", "Esc / P")
            }, font);

            // Right Card: GAMEPAD CONTROLLER
            BuildControlsCategoryCard(contentObj.transform, "GAMEPAD CONTROLLER", new Color(1.0f, 0.82f, 0.25f, 1f), new (string, string)[]
            {
                ("Movement", "Left Stick"),
                ("Look / Aim", "Right Stick"),
                ("Sprint / Dash", "L3 (Left Stick Click)"),
                ("Jump", "A / Cross Button"),
                ("Tactical Roll / Evade", "B / Circle Button"),
                ("Fire Weapon", "RT / R2 (Right Trigger)"),
                ("Aim Down Sights (ADS)", "LT / L2 (Left Trigger)"),
                ("Melee Strike", "R3 (Right Stick Click)"),
                ("Reload Weapon", "X / Square Button"),
                ("Switch Weapon", "Y / Triangle Button"),
                ("Deployable Trip Mine", "RB / R1 (Right Bumper)"),
                ("Tactical Throwable", "LB / L1 (Left Bumper)"),
                ("Kinetic Shield", "D-Pad Left"),
                ("Optical Invisibility", "D-Pad Right"),
                ("Tactical Scoreboard", "View / Back / Select"),
                ("Tactical Pause Menu", "Menu / Start / Options")
            }, font);

            return rootObj;
        }

        private static void BuildControlsCategoryCard(Transform parent, string headerTitle, Color accentColor, (string action, string binding)[] bindings, TMP_FontAsset font)
        {
            var cardObj = new GameObject($"Card_{headerTitle}", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            cardObj.transform.SetParent(parent, false);

            var cImg = cardObj.GetComponent<Image>();
            cImg.color = new Color(0.04f, 0.08f, 0.16f, 0.92f);

            var vlg = cardObj.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 14, 14);
            vlg.spacing = 8f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var headerObj = new GameObject("Header", typeof(RectTransform), typeof(TextMeshProUGUI));
            headerObj.transform.SetParent(cardObj.transform, false);

            var hRt = headerObj.GetComponent<RectTransform>();
            hRt.sizeDelta = new Vector2(0f, 32f);

            var hTmp = headerObj.GetComponent<TextMeshProUGUI>();
            hTmp.text = headerTitle;
            hTmp.fontSize = 17;
            hTmp.fontStyle = FontStyles.Bold;
            hTmp.color = accentColor;
            hTmp.alignment = TextAlignmentOptions.MidlineLeft;
            if (font != null) hTmp.font = font;

            for (int i = 0; i < bindings.Length; i++)
            {
                var rowObj = new GameObject($"Row_{i}", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
                rowObj.transform.SetParent(cardObj.transform, false);

                var rRt = rowObj.GetComponent<RectTransform>();
                rRt.sizeDelta = new Vector2(0f, 30f);

                var rImg = rowObj.GetComponent<Image>();
                rImg.color = (i % 2 == 0) ? new Color(0.07f, 0.12f, 0.22f, 0.6f) : new Color(0.04f, 0.07f, 0.14f, 0.4f);

                var rHlg = rowObj.GetComponent<HorizontalLayoutGroup>();
                rHlg.padding = new RectOffset(12, 12, 4, 4);
                rHlg.childAlignment = TextAnchor.MiddleLeft;
                rHlg.childControlWidth = false;
                rHlg.childControlHeight = true;
                rHlg.childForceExpandWidth = false;
                rHlg.spacing = 10;

                var actionObj = new GameObject("Action", typeof(RectTransform), typeof(TextMeshProUGUI));
                actionObj.transform.SetParent(rowObj.transform, false);
                var aRt = actionObj.GetComponent<RectTransform>();
                aRt.sizeDelta = new Vector2(190f, 24f);

                var aTmp = actionObj.GetComponent<TextMeshProUGUI>();
                aTmp.text = bindings[i].action;
                aTmp.fontSize = 13;
                aTmp.fontStyle = FontStyles.Normal;
                aTmp.color = new Color(0.85f, 0.92f, 1f, 0.95f);
                aTmp.alignment = TextAlignmentOptions.MidlineLeft;
                if (font != null) aTmp.font = font;

                var badgeObj = new GameObject("Badge", typeof(RectTransform), typeof(Image));
                badgeObj.transform.SetParent(rowObj.transform, false);
                var bRt = badgeObj.GetComponent<RectTransform>();
                bRt.sizeDelta = new Vector2(170f, 22f);

                var bImg = badgeObj.GetComponent<Image>();
                bImg.color = new Color(0.12f, 0.18f, 0.32f, 0.9f);

                var keyObj = new GameObject("KeyText", typeof(RectTransform), typeof(TextMeshProUGUI));
                keyObj.transform.SetParent(badgeObj.transform, false);
                var kRt = keyObj.GetComponent<RectTransform>();
                kRt.anchorMin = Vector2.zero;
                kRt.anchorMax = Vector2.one;
                kRt.sizeDelta = Vector2.zero;

                var kTmp = keyObj.GetComponent<TextMeshProUGUI>();
                kTmp.text = bindings[i].binding;
                kTmp.fontSize = 12;
                kTmp.fontStyle = FontStyles.Bold;
                kTmp.color = accentColor;
                kTmp.alignment = TextAlignmentOptions.Center;
                if (font != null) kTmp.font = font;
            }
        }
    }
}
