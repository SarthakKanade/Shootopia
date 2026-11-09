using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Shootopia
{
    /// <summary>
    /// Shared procedural builder for futuristic tactical cybernetic UI components.
    /// Unifies scoreboard styling across in-game Tactical Scoreboard and Pause Menu overlays.
    /// </summary>
    public static class TacticalUIBuilder
    {
        public static readonly Color ThemeCyanBorder = new Color(0.18f, 0.72f, 1.0f, 0.45f);
        public static readonly Color ThemeDarkBackdrop = new Color(0.04f, 0.07f, 0.14f, 0.96f);
        public static readonly Color ThemeHeaderBg = new Color(0.06f, 0.08f, 0.16f, 0.96f);
        public static readonly Color TextWhite = new Color(0.92f, 0.97f, 1f, 1f);
        public static readonly Color TextMuted = new Color(0.6f, 0.78f, 0.95f, 1f);

        public static void SetupBackdrop(GameObject root, Vector2 size)
        {
            if (root == null) return;
            var rootRt = root.GetComponent<RectTransform>();
            if (rootRt != null)
            {
                rootRt.sizeDelta = size;
                rootRt.anchoredPosition = Vector2.zero;
            }

            var rootImg = root.GetComponent<Image>();
            if (rootImg != null) rootImg.color = ThemeCyanBorder;

            if (root.transform.Find("ModalBackdrop") == null)
            {
                var backdropObj = new GameObject("ModalBackdrop", typeof(RectTransform), typeof(Image));
                backdropObj.transform.SetParent(root.transform, false);
                backdropObj.transform.SetSiblingIndex(0);

                var bRt = backdropObj.GetComponent<RectTransform>();
                bRt.anchorMin = Vector2.zero;
                bRt.anchorMax = Vector2.one;
                bRt.sizeDelta = new Vector2(-16f, -16f);
                bRt.anchoredPosition = Vector2.zero;

                var bImg = backdropObj.GetComponent<Image>();
                bImg.color = ThemeDarkBackdrop;
                bImg.raycastTarget = false;
            }
        }

        public static void CreateHeaderCol(string name, Transform parent, string text, float width, TextAlignmentOptions align, TMP_FontAsset font = null)
        {
            if (parent == null) return;
            var colObj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            colObj.transform.SetParent(parent, false);

            var rt = colObj.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(width, 28f);

            var le = colObj.GetComponent<LayoutElement>();
            if (le != null) { le.preferredWidth = width; le.minWidth = width; }

            var tmp = colObj.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = text;
                tmp.fontSize = 14;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = TextWhite;
                tmp.alignment = align;
                if (font != null) tmp.font = font;
            }
        }

        public static Transform EnsureScrollRowsContainer(GameObject root, float height = 400f, float posY = -40f)
        {
            if (root == null) return null;

            Transform scrollTr = root.transform.Find("ScoreScrollView");
            if (scrollTr == null)
            {
                var scrollObj = new GameObject("ScoreScrollView", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
                scrollObj.transform.SetParent(root.transform, false);
                scrollTr = scrollObj.transform;

                var img = scrollObj.GetComponent<Image>();
                img.color = new Color(0.02f, 0.04f, 0.08f, 0.65f);
            }

            var sRt = scrollTr.GetComponent<RectTransform>();
            if (sRt != null)
            {
                sRt.anchorMin = new Vector2(0.5f, 0.5f);
                sRt.anchorMax = new Vector2(0.5f, 0.5f);
                sRt.sizeDelta = new Vector2(880f, height);
                sRt.anchoredPosition = new Vector2(0f, posY);
            }

            Transform content = scrollTr.Find("Content") ?? scrollTr.Find("Viewport/Content");
            if (content == null)
            {
                var contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                contentObj.transform.SetParent(scrollTr, false);
                content = contentObj.transform;
            }

            var cRt = content.GetComponent<RectTransform>();
            if (cRt != null)
            {
                cRt.anchorMin = new Vector2(0f, 1f);
                cRt.anchorMax = new Vector2(1f, 1f);
                cRt.pivot = new Vector2(0.5f, 1f);
                cRt.sizeDelta = Vector2.zero;
            }

            var vlg = content.GetComponent<VerticalLayoutGroup>() ?? content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.spacing = 6f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = content.GetComponent<ContentSizeFitter>() ?? content.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var sr = scrollTr.GetComponent<ScrollRect>() ?? scrollTr.gameObject.AddComponent<ScrollRect>();
            sr.content = cRt;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;

            return content;
        }

        public static GameObject CreatePingWidget(Transform parent, int ping, bool isHost, float width, TMP_FontAsset font = null)
        {
            var pingCol = new GameObject("PingCol", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            pingCol.transform.SetParent(parent, false);

            var pRt = pingCol.GetComponent<RectTransform>();
            if (pRt != null) pRt.sizeDelta = new Vector2(width, 36f);

            var le = pingCol.GetComponent<LayoutElement>();
            if (le != null) { le.preferredWidth = width; le.minWidth = width; }

            var phlg = pingCol.GetComponent<HorizontalLayoutGroup>();
            phlg.childAlignment = TextAnchor.MiddleRight;
            phlg.childControlWidth = false;
            phlg.childControlHeight = true;
            phlg.spacing = 6;

            var dotObj = new GameObject("PingDot", typeof(RectTransform), typeof(Image));
            dotObj.transform.SetParent(pingCol.transform, false);
            var dotRt = dotObj.GetComponent<RectTransform>();
            if (dotRt != null) dotRt.sizeDelta = new Vector2(9f, 9f);
            var dotImg = dotObj.GetComponent<Image>();

            Color pingColColor;
            string pingStr;
            if (isHost)
            {
                pingColColor = new Color(0.25f, 0.95f, 0.5f, 1f);
                pingStr = "HOST";
            }
            else if (ping <= 50)
            {
                pingColColor = new Color(0.25f, 0.95f, 0.5f, 1f);
                pingStr = $"{ping}ms";
            }
            else if (ping <= 110)
            {
                pingColColor = new Color(1.0f, 0.85f, 0.25f, 1f);
                pingStr = $"{ping}ms";
            }
            else
            {
                pingColColor = new Color(1.0f, 0.3f, 0.3f, 1f);
                pingStr = $"{ping}ms";
            }

            if (dotImg != null) dotImg.color = pingColColor;

            var textObj = new GameObject("PingText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(pingCol.transform, false);
            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = pingStr;
                tmp.fontSize = 13;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = pingColColor;
                tmp.alignment = TextAlignmentOptions.MidlineRight;
                if (font != null) tmp.font = font;
            }

            return pingCol;
        }
    }
}

