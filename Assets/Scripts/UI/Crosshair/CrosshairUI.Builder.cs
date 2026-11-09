using UnityEngine;
using UnityEngine.UI;

namespace Shootopia
{
    public partial class CrosshairUI
    {
#region UI Construction

        private void BuildCrosshairUI()
        {
            if (crosshairRoot == null)
            {
                crosshairRoot = GetComponent<RectTransform>();
            }

            // Destroy any legacy single-image crosshair child
            foreach (Transform child in crosshairRoot)
            {
                Destroy(child.gameObject);
            }

            // Center dot
            _dot = CreateRect("Dot", crosshairRoot, dotSize, dotSize, normalColor);
            _dot.rectTransform.anchoredPosition = Vector2.zero;

            // Four cardinal lines
            _lineTop = CreateRect("LineTop", crosshairRoot, lineThickness, lineLength, normalColor);
            _lineBottom = CreateRect("LineBottom", crosshairRoot, lineThickness, lineLength, normalColor);
            _lineLeft = CreateRect("LineLeft", crosshairRoot, lineLength, lineThickness, normalColor);
            _lineRight = CreateRect("LineRight", crosshairRoot, lineLength, lineThickness, normalColor);

            // Hit marker — 4 diagonal ticks
            GameObject hitGroup = new GameObject("HitMarker", typeof(RectTransform), typeof(CanvasGroup));
            hitGroup.transform.SetParent(crosshairRoot, false);
            var hitRT = hitGroup.GetComponent<RectTransform>();
            hitRT.anchorMin = hitRT.anchorMax = hitRT.pivot = new Vector2(0.5f, 0.5f);
            hitRT.sizeDelta = Vector2.zero;
            _hitMarkerGroup = hitGroup.GetComponent<CanvasGroup>();
            _hitMarkerGroup.alpha = 0f;

            _hitTL = CreateRect("HitTL", hitRT, hitTickThickness, hitTickLength, hitColor);
            _hitTR = CreateRect("HitTR", hitRT, hitTickThickness, hitTickLength, hitColor);
            _hitBL = CreateRect("HitBL", hitRT, hitTickThickness, hitTickLength, hitColor);
            _hitBR = CreateRect("HitBR", hitRT, hitTickThickness, hitTickLength, hitColor);

            // Rotate hit ticks 45° diagonals
            _hitTL.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            _hitTR.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -45f);
            _hitBL.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -45f);
            _hitBR.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            // Position hit ticks
            float d = hitTickOffset * 0.707f; // cos(45°)
            _hitTL.rectTransform.anchoredPosition = new Vector2(-d, d);
            _hitTR.rectTransform.anchoredPosition = new Vector2(d, d);
            _hitBL.rectTransform.anchoredPosition = new Vector2(-d, -d);
            _hitBR.rectTransform.anchoredPosition = new Vector2(d, -d);
        }

        private Image CreateRect(string name, RectTransform parent, float width, float height, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = Vector2.zero;

            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        #endregion
    }
}
