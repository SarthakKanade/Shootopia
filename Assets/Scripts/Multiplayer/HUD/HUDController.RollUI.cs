using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Shootopia
{
    public partial class HUDController
    {
        private void CreateRollCard(GameObject parent)
        {
            var rollCard = new GameObject("RollCard", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            rollCard.transform.SetParent(parent.transform, false);
            var rcRt = rollCard.GetComponent<RectTransform>();
            rcRt.sizeDelta = new Vector2(310f, 28f);

            var rcBg = rollCard.GetComponent<Image>();
            rcBg.color = new Color(0.03f, 0.10f, 0.07f, 0.92f);
            rcBg.raycastTarget = false;
            rollCanvasGroup = rollCard.GetComponent<CanvasGroup>();

            // Emerald green left accent border (#2BFFA3)
            var rcAccent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            rcAccent.transform.SetParent(rollCard.transform, false);
            var accRt = rcAccent.GetComponent<RectTransform>();
            accRt.anchorMin = new Vector2(0f, 0f);
            accRt.anchorMax = new Vector2(0f, 1f);
            accRt.pivot = new Vector2(0f, 0.5f);
            accRt.anchoredPosition = Vector2.zero;
            accRt.sizeDelta = new Vector2(4f, 0f);
            var accImg = rcAccent.GetComponent<Image>();
            accImg.color = new Color(0.17f, 1f, 0.64f, 0.95f);
            accImg.raycastTarget = false;

            // Fill Bar
            var rcFillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            rcFillObj.transform.SetParent(rollCard.transform, false);
            var rcFillRt = rcFillObj.GetComponent<RectTransform>();
            rcFillRt.anchorMin = Vector2.zero;
            rcFillRt.anchorMax = Vector2.one;
            rcFillRt.pivot = new Vector2(0f, 0.5f);
            rcFillRt.anchoredPosition = Vector2.zero;
            rcFillRt.sizeDelta = Vector2.zero;
            rollFill = rcFillObj.GetComponent<Image>();
            rollFill.color = new Color(0.17f, 1f, 0.64f, 0.35f);
            rollFill.type = Image.Type.Filled;
            rollFill.fillMethod = Image.FillMethod.Horizontal;
            rollFill.fillOrigin = 0;
            rollFill.fillAmount = 1f;
            rollFill.raycastTarget = false;

            // Label
            var rcLabelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            rcLabelObj.transform.SetParent(rollCard.transform, false);
            var rcLabelRt = rcLabelObj.GetComponent<RectTransform>();
            rcLabelRt.anchorMin = Vector2.zero;
            rcLabelRt.anchorMax = Vector2.one;
            rcLabelRt.anchoredPosition = new Vector2(10f, 0f);
            rcLabelRt.sizeDelta = new Vector2(-16f, 0f);
            rollText = rcLabelObj.GetComponent<TextMeshProUGUI>();
            rollText.text = "<color=#2BFFA3>ROLL</color>  RECHARGING  •  3.0s";
            rollText.fontSize = 13;
            rollText.fontStyle = FontStyles.Bold;
            rollText.alignment = TextAlignmentOptions.MidlineLeft;
            rollText.color = Color.white;
            rollText.raycastTarget = false;

            rollBarRoot = rollCard;
            rollBarRoot.SetActive(false);
        }
    }
}

