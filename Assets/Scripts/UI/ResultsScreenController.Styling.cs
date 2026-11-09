using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Shootopia
{
    public partial class ResultsScreenController
    {
private void ApplyWaitingRoomStyling()
        {
            if (_isStyled || resultsRoot == null) return;
            _isStyled = true;

            var rootRt = resultsRoot.GetComponent<RectTransform>();
            if (rootRt != null)
            {
                rootRt.sizeDelta = new Vector2(1000f, 680f);
            }

            // Outer border image
            var rootImg = resultsRoot.GetComponent<Image>();
            if (rootImg != null)
            {
                rootImg.color = ColorFrameCyan;
                rootImg.type = Image.Type.Sliced;
                if (cyberFrameSprite != null) rootImg.sprite = cyberFrameSprite;
            }

            // Dark inner backdrop
            Transform backdropTr = resultsRoot.transform.Find("ModalBackdrop");
            if (backdropTr == null)
            {
                var backdropObj = new GameObject("ModalBackdrop", typeof(RectTransform), typeof(Image));
                backdropObj.transform.SetParent(resultsRoot.transform, false);
                backdropObj.transform.SetSiblingIndex(0); // keep at back
                backdropTr = backdropObj.transform;

                var bRt = backdropObj.GetComponent<RectTransform>();
                bRt.anchorMin = Vector2.zero;
                bRt.anchorMax = Vector2.one;
                bRt.sizeDelta = new Vector2(-16f, -16f);
                bRt.anchoredPosition = Vector2.zero;

                var bImg = backdropObj.GetComponent<Image>();
                bImg.color = ColorDarkBackdrop;
                bImg.raycastTarget = false;
            }

            // Header titles positioning
            if (outcomeBannerText != null)
            {
                var bannerRt = outcomeBannerText.GetComponent<RectTransform>();
                if (bannerRt != null) bannerRt.anchoredPosition = new Vector2(0f, 265f);
                outcomeBannerText.fontSize = 38;
            }

            if (winnerInfoText != null)
            {
                var winnerRt = winnerInfoText.GetComponent<RectTransform>();
                if (winnerRt != null) winnerRt.anchoredPosition = new Vector2(0f, 220f);
                winnerInfoText.fontSize = 20;
            }

            // Setup Column Header Row above scroll area
            Transform headerBarTr = resultsRoot.transform.Find("ScoreTableHeader");
            if (headerBarTr == null)
            {
                var headerObj = new GameObject("ScoreTableHeader", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
                headerObj.transform.SetParent(resultsRoot.transform, false);
                headerBarTr = headerObj.transform;

                var hRt = headerObj.GetComponent<RectTransform>();
                if (hRt != null)
                {
                    hRt.anchorMin = new Vector2(0.5f, 0.5f);
                    hRt.anchorMax = new Vector2(0.5f, 0.5f);
                    hRt.sizeDelta = new Vector2(880f, 36f);
                    hRt.anchoredPosition = new Vector2(0f, 175f);
                }

                var hImg = headerObj.GetComponent<Image>();
                if (hImg != null) hImg.color = ColorHeaderBg;

                var hlg = headerObj.GetComponent<HorizontalLayoutGroup>();
                if (hlg != null)
                {
                    hlg.padding = new RectOffset(24, 24, 4, 4);
                    hlg.childAlignment = TextAnchor.MiddleLeft;
                    hlg.childControlWidth = false;
                    hlg.childControlHeight = true;
                    hlg.spacing = 10;
                }

                CreateHeaderCol("RankHeader", headerBarTr, "RANK", 70f, TextAlignmentOptions.MidlineLeft);
                CreateHeaderCol("PlayerHeader", headerBarTr, "PLAYER", 360f, TextAlignmentOptions.MidlineLeft);
                CreateHeaderCol("KDHeader", headerBarTr, "KILLS / DEATHS", 220f, TextAlignmentOptions.Center);
                CreateHeaderCol("ScoreHeader", headerBarTr, "SCORE", 160f, TextAlignmentOptions.MidlineRight);
            }

            // Adjust Scroll Area position and size
            Transform scrollTr = resultsRoot.transform.Find("ResultsScrollView");
            if (scrollTr != null)
            {
                var sRt = scrollTr.GetComponent<RectTransform>();
                if (sRt != null)
                {
                    sRt.sizeDelta = new Vector2(880f, 340f);
                    sRt.anchoredPosition = new Vector2(0f, -20f);
                }
            }

            // Style Buttons with WaitingRoom appearance and Juice
            StyleActionButton(btnReturnToLobby, goldButtonSprite, new Vector2(260f, 60f), new Vector2(-155f, -250f), "RETURN TO LOBBY", new Color(0.12f, 0.06f, 0f, 1f));
            StyleActionButton(btnMainMenu, blueButtonSprite, new Vector2(240f, 60f), new Vector2(165f, -250f), "MAIN MENU", Color.white);
        }

        private void CreateHeaderCol(string name, Transform parent, string text, float width, TextAlignmentOptions align)
        {
            if (parent == null) return;
            var colObj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            colObj.transform.SetParent(parent, false);

            var rt = colObj.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(width, 28f);

            var tmp = colObj.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = text;
                tmp.fontSize = 14;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = ColorHeaderText;
                tmp.alignment = align;
                if (outcomeBannerText != null) tmp.font = outcomeBannerText.font;
            }
        }

        private void StyleActionButton(Button btn, Sprite buttonSprite, Vector2 size, Vector2 pos, string labelText, Color textColor)
        {
            if (btn == null) return;

            var rt = btn.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = size;
                rt.anchoredPosition = pos;
            }

            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.type = Image.Type.Sliced;
                if (buttonSprite != null) img.sprite = buttonSprite;
            }

            var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.text = labelText;
                txt.fontSize = 18;
                txt.color = textColor;
                txt.fontStyle = FontStyles.Bold;
                txt.alignment = TextAlignmentOptions.Center;
            }

            // Ensure UIJuice spring component is attached for snappy tactile feedback
            if (btn.GetComponent<UIJuice>() == null)
            {
                btn.gameObject.AddComponent<UIJuice>();
            }
        }
    }
}
