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
        // ──────────────────────── POWER-UP TIMERS HUD ────────────────────────

        public void EnsurePowerUpTimersUI()
        {
            if (powerUpRoot != null && shieldBarRoot != null && invisibilityBarRoot != null && rollBarRoot != null) return;

            Transform existing = transform.Find("PowerUpStatusGroup");
            if (existing != null)
            {
                powerUpRoot = existing.gameObject;
                shieldBarRoot = existing.Find("ShieldCard")?.gameObject;
                shieldFill = existing.Find("ShieldCard/Fill")?.GetComponent<Image>();
                shieldText = existing.Find("ShieldCard/Label")?.GetComponent<TextMeshProUGUI>();
                shieldCanvasGroup = shieldBarRoot?.GetComponent<CanvasGroup>();

                invisibilityBarRoot = existing.Find("InvisCard")?.gameObject;
                invisibilityFill = existing.Find("InvisCard/Fill")?.GetComponent<Image>();
                invisibilityText = existing.Find("InvisCard/Label")?.GetComponent<TextMeshProUGUI>();
                invisibilityCanvasGroup = invisibilityBarRoot?.GetComponent<CanvasGroup>();

                rollBarRoot = existing.Find("RollCard")?.gameObject;
                rollFill = existing.Find("RollCard/Fill")?.GetComponent<Image>();
                rollText = existing.Find("RollCard/Label")?.GetComponent<TextMeshProUGUI>();
                rollCanvasGroup = rollBarRoot?.GetComponent<CanvasGroup>();

                if (rollBarRoot == null)
                {
                    CreateRollCard(existing.gameObject);
                }

                if (shieldBarRoot != null && invisibilityBarRoot != null && rollBarRoot != null)
                {
                    shieldBarRoot.SetActive(false);
                    invisibilityBarRoot.SetActive(false);
                    rollBarRoot.SetActive(false);
                    return;
                }
            }

            // Create PowerUpStatusGroup anchored just above HealthFrame (bottom-left)
            var rootObj = new GameObject("PowerUpStatusGroup", typeof(RectTransform), typeof(VerticalLayoutGroup));
            rootObj.transform.SetParent(transform, false);
            var rt = rootObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(32f, 118f);
            rt.sizeDelta = new Vector2(310f, 106f);

            var vlg = rootObj.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 6f;
            vlg.childAlignment = TextAnchor.LowerLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            powerUpRoot = rootObj;

            // 1. Force Field Shield Card
            var shieldCard = new GameObject("ShieldCard", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            shieldCard.transform.SetParent(rootObj.transform, false);
            var scRt = shieldCard.GetComponent<RectTransform>();
            scRt.sizeDelta = new Vector2(310f, 28f);

            var scBg = shieldCard.GetComponent<Image>();
            scBg.color = new Color(0.04f, 0.08f, 0.16f, 0.92f);
            scBg.raycastTarget = false;
            shieldCanvasGroup = shieldCard.GetComponent<CanvasGroup>();

            // Cyan left accent border
            var scAccent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            scAccent.transform.SetParent(shieldCard.transform, false);
            var accRt = scAccent.GetComponent<RectTransform>();
            accRt.anchorMin = new Vector2(0f, 0f);
            accRt.anchorMax = new Vector2(0f, 1f);
            accRt.pivot = new Vector2(0f, 0.5f);
            accRt.anchoredPosition = Vector2.zero;
            accRt.sizeDelta = new Vector2(4f, 0f);
            var accImg = scAccent.GetComponent<Image>();
            accImg.color = new Color(0f, 0.94f, 1f, 0.95f);
            accImg.raycastTarget = false;

            // Fill Bar
            var scFillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            scFillObj.transform.SetParent(shieldCard.transform, false);
            var scFillRt = scFillObj.GetComponent<RectTransform>();
            scFillRt.anchorMin = Vector2.zero;
            scFillRt.anchorMax = Vector2.one;
            scFillRt.pivot = new Vector2(0f, 0.5f);
            scFillRt.anchoredPosition = Vector2.zero;
            scFillRt.sizeDelta = Vector2.zero;
            shieldFill = scFillObj.GetComponent<Image>();
            shieldFill.color = new Color(0f, 0.82f, 1f, 0.35f);
            shieldFill.type = Image.Type.Filled;
            shieldFill.fillMethod = Image.FillMethod.Horizontal;
            shieldFill.fillOrigin = 0;
            shieldFill.fillAmount = 1f;
            shieldFill.raycastTarget = false;

            // Label
            var scLabelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            scLabelObj.transform.SetParent(shieldCard.transform, false);
            var scLabelRt = scLabelObj.GetComponent<RectTransform>();
            scLabelRt.anchorMin = Vector2.zero;
            scLabelRt.anchorMax = Vector2.one;
            scLabelRt.anchoredPosition = new Vector2(10f, 0f);
            scLabelRt.sizeDelta = new Vector2(-16f, 0f);
            shieldText = scLabelObj.GetComponent<TextMeshProUGUI>();
            shieldText.text = "<color=#00F0FF>SHIELD</color>  50/50 HP  •  15.0s";
            shieldText.fontSize = 13;
            shieldText.fontStyle = FontStyles.Bold;
            shieldText.alignment = TextAlignmentOptions.MidlineLeft;
            shieldText.color = Color.white;
            shieldText.raycastTarget = false;

            shieldBarRoot = shieldCard;
            shieldBarRoot.SetActive(false);

            // 2. Invisibility Cloak Card
            var invisCard = new GameObject("InvisCard", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            invisCard.transform.SetParent(rootObj.transform, false);
            var icRt = invisCard.GetComponent<RectTransform>();
            icRt.sizeDelta = new Vector2(310f, 28f);

            var icBg = invisCard.GetComponent<Image>();
            icBg.color = new Color(0.09f, 0.04f, 0.18f, 0.92f);
            icBg.raycastTarget = false;
            invisibilityCanvasGroup = invisCard.GetComponent<CanvasGroup>();

            // Purple left accent border
            var icAccent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            icAccent.transform.SetParent(invisCard.transform, false);
            var icAccRt = icAccent.GetComponent<RectTransform>();
            icAccRt.anchorMin = new Vector2(0f, 0f);
            icAccRt.anchorMax = new Vector2(0f, 1f);
            icAccRt.pivot = new Vector2(0f, 0.5f);
            icAccRt.anchoredPosition = Vector2.zero;
            icAccRt.sizeDelta = new Vector2(4f, 0f);
            var icAccImg = icAccent.GetComponent<Image>();
            icAccImg.color = new Color(0.72f, 0.38f, 1f, 0.95f);
            icAccImg.raycastTarget = false;

            // Fill Bar
            var icFillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            icFillObj.transform.SetParent(invisCard.transform, false);
            var icFillRt = icFillObj.GetComponent<RectTransform>();
            icFillRt.anchorMin = Vector2.zero;
            icFillRt.anchorMax = Vector2.one;
            icFillRt.pivot = new Vector2(0f, 0.5f);
            icFillRt.anchoredPosition = Vector2.zero;
            icFillRt.sizeDelta = Vector2.zero;
            invisibilityFill = icFillObj.GetComponent<Image>();
            invisibilityFill.color = new Color(0.68f, 0.32f, 1f, 0.35f);
            invisibilityFill.type = Image.Type.Filled;
            invisibilityFill.fillMethod = Image.FillMethod.Horizontal;
            invisibilityFill.fillOrigin = 0;
            invisibilityFill.fillAmount = 1f;
            invisibilityFill.raycastTarget = false;

            // Label
            var icLabelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            icLabelObj.transform.SetParent(invisCard.transform, false);
            var icLabelRt = icLabelObj.GetComponent<RectTransform>();
            icLabelRt.anchorMin = Vector2.zero;
            icLabelRt.anchorMax = Vector2.one;
            icLabelRt.anchoredPosition = new Vector2(10f, 0f);
            icLabelRt.sizeDelta = new Vector2(-16f, 0f);
            invisibilityText = icLabelObj.GetComponent<TextMeshProUGUI>();
            invisibilityText.text = "<color=#D47BFF>STEALTH ACTIVE</color>  •  10.0s";
            invisibilityText.fontSize = 13;
            invisibilityText.fontStyle = FontStyles.Bold;
            invisibilityText.alignment = TextAlignmentOptions.MidlineLeft;
            invisibilityText.color = Color.white;
            invisibilityText.raycastTarget = false;

            invisibilityBarRoot = invisCard;
            invisibilityBarRoot.SetActive(false);

            // 3. Roll Cooldown Card
            CreateRollCard(rootObj);
        }
    }
}
