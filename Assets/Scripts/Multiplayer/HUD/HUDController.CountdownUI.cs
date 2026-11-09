using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Shootopia
{
    public partial class HUDController
    {
public void EnsurePreMatchCountdownUI()
        {
            if (preMatchRoot != null && preMatchCountdownText != null) return;

            // Check if already in canvas hierarchy
            var existing = transform.Find("PreMatchCountdownRoot");
            if (existing != null)
            {
                preMatchRoot = existing.gameObject;
                preMatchGroup = existing.GetComponent<CanvasGroup>();
                preMatchCountdownText = existing.Find("CountdownNumber")?.GetComponent<TextMeshProUGUI>();
                preMatchSubText = existing.Find("SubText")?.GetComponent<TextMeshProUGUI>();
                if (preMatchRoot != null && preMatchCountdownText != null)
                {
                    preMatchRoot.SetActive(false);
                    return;
                }
            }

            // Create PreMatchCountdownRoot in exact center of screen
            var rootObj = new GameObject("PreMatchCountdownRoot", typeof(RectTransform), typeof(CanvasGroup));
            rootObj.transform.SetParent(transform, false);
            var rt = rootObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 35f);
            rt.sizeDelta = new Vector2(480f, 260f);

            var cg = rootObj.GetComponent<CanvasGroup>();
            cg.alpha = 1f;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            // Background Card
            var bgObj = new GameObject("BackgroundPanel", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(rootObj.transform, false);
            var bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.pivot = new Vector2(0.5f, 0.5f);
            bgRt.anchoredPosition = Vector2.zero;
            bgRt.sizeDelta = Vector2.zero;
            var bgImg = bgObj.GetComponent<Image>();
            bgImg.color = new Color(0.04f, 0.07f, 0.15f, 0.90f);
            bgImg.raycastTarget = false;

            // Sample an existing sliced sprite if present on canvas
            var sampleImg = GetComponentInChildren<Image>();
            if (sampleImg != null && sampleImg.sprite != null)
            {
                bgImg.sprite = sampleImg.sprite;
                bgImg.type = Image.Type.Sliced;
            }

            // Glowing Accent Top & Bottom lines
            var topBarObj = new GameObject("TopAccent", typeof(RectTransform), typeof(Image));
            topBarObj.transform.SetParent(bgObj.transform, false);
            var topBarRt = topBarObj.GetComponent<RectTransform>();
            topBarRt.anchorMin = new Vector2(0.12f, 1f);
            topBarRt.anchorMax = new Vector2(0.88f, 1f);
            topBarRt.pivot = new Vector2(0.5f, 1f);
            topBarRt.anchoredPosition = new Vector2(0f, -6f);
            topBarRt.sizeDelta = new Vector2(0f, 3f);
            var topImg = topBarObj.GetComponent<Image>();
            topImg.color = new Color(0.0f, 0.94f, 1.0f, 0.9f);
            topImg.raycastTarget = false;

            var botBarObj = new GameObject("BottomAccent", typeof(RectTransform), typeof(Image));
            botBarObj.transform.SetParent(bgObj.transform, false);
            var botBarRt = botBarObj.GetComponent<RectTransform>();
            botBarRt.anchorMin = new Vector2(0.12f, 0f);
            botBarRt.anchorMax = new Vector2(0.88f, 0f);
            botBarRt.pivot = new Vector2(0.5f, 0f);
            botBarRt.anchoredPosition = new Vector2(0f, 6f);
            botBarRt.sizeDelta = new Vector2(0f, 3f);
            var botImg = botBarObj.GetComponent<Image>();
            botImg.color = new Color(0.0f, 0.94f, 1.0f, 0.9f);
            botImg.raycastTarget = false;

            // Subtitle text
            var subObj = new GameObject("SubText", typeof(RectTransform), typeof(TextMeshProUGUI));
            subObj.transform.SetParent(rootObj.transform, false);
            var subRt = subObj.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0f, 0.68f);
            subRt.anchorMax = new Vector2(1f, 0.96f);
            subRt.pivot = new Vector2(0.5f, 0.5f);
            subRt.anchoredPosition = Vector2.zero;
            subRt.sizeDelta = Vector2.zero;
            var subTmp = subObj.GetComponent<TextMeshProUGUI>();
            subTmp.text = "MATCH STARTING IN";
            subTmp.fontSize = 22f;
            subTmp.fontStyle = FontStyles.Bold;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = new Color(0.0f, 0.94f, 1.0f, 0.95f);
            subTmp.characterSpacing = 8f;
            subTmp.raycastTarget = false;

            // Main Countdown Number / Text
            var numObj = new GameObject("CountdownNumber", typeof(RectTransform), typeof(TextMeshProUGUI));
            numObj.transform.SetParent(rootObj.transform, false);
            var numRt = numObj.GetComponent<RectTransform>();
            numRt.anchorMin = new Vector2(0f, 0.05f);
            numRt.anchorMax = new Vector2(1f, 0.72f);
            numRt.pivot = new Vector2(0.5f, 0.5f);
            numRt.anchoredPosition = Vector2.zero;
            numRt.sizeDelta = Vector2.zero;
            var numTmp = numObj.GetComponent<TextMeshProUGUI>();
            numTmp.text = "3";
            numTmp.fontSize = 110f;
            numTmp.fontStyle = FontStyles.Bold;
            numTmp.alignment = TextAlignmentOptions.Center;
            numTmp.color = new Color(0.0f, 0.94f, 1.0f, 1.0f);
            numTmp.raycastTarget = false;

            // Match font with timerText or scoreText if assigned
            if (timerText != null && timerText.font != null)
            {
                subTmp.font = timerText.font;
                numTmp.font = timerText.font;
            }
            else if (scoreText != null && scoreText.font != null)
            {
                subTmp.font = scoreText.font;
                numTmp.font = scoreText.font;
            }

            preMatchRoot = rootObj;
            preMatchGroup = cg;
            preMatchCountdownText = numTmp;
            preMatchSubText = subTmp;
            rootObj.SetActive(false);
        }
    }
}
