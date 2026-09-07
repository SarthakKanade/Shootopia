using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// Adjusts RectTransform anchors to match the device's hardware Safe Area
    /// (accounting for notches, punch-hole cameras, Dynamic Islands, and system navigation bars).
    /// Runs in editor for Device Simulator responsiveness and prevents division-by-zero errors.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        [Header("Optional Margins (Normalized [0..0.1])")]
        [Range(0f, 0.05f)]
        [SerializeField] private float additionalPadding = 0f;

        private RectTransform _rectTransform;
        private Rect _lastSafeArea = Rect.zero;
        private Vector2Int _lastScreenSize = Vector2Int.zero;
        private ScreenOrientation _lastOrientation = ScreenOrientation.Unknown;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void OnEnable()
        {
            ApplySafeArea();
        }

        private void Update()
        {
            if (_lastSafeArea != Screen.safeArea || 
                _lastScreenSize.x != Screen.width || 
                _lastScreenSize.y != Screen.height ||
                _lastOrientation != Screen.orientation)
            {
                ApplySafeArea();
            }
        }

        public void ForceUpdate()
        {
            ApplySafeArea();
        }

        private void ApplySafeArea()
        {
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null) return;

            int screenWidth = Screen.width;
            int screenHeight = Screen.height;

            if (screenWidth <= 0 || screenHeight <= 0) return;

            Rect safeArea = Screen.safeArea;
            _lastSafeArea = safeArea;
            _lastScreenSize = new Vector2Int(screenWidth, screenHeight);
            _lastOrientation = Screen.orientation;

            // Convert safe area rectangle from absolute pixels to normalized anchor coordinates
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= screenWidth;
            anchorMin.y /= screenHeight;
            anchorMax.x /= screenWidth;
            anchorMax.y /= screenHeight;

            // Apply optional additional margin padding if configured
            if (additionalPadding > 0f)
            {
                anchorMin.x = Mathf.Clamp01(anchorMin.x + additionalPadding);
                anchorMin.y = Mathf.Clamp01(anchorMin.y + additionalPadding);
                anchorMax.x = Mathf.Clamp01(anchorMax.x - additionalPadding);
                anchorMax.y = Mathf.Clamp01(anchorMax.y - additionalPadding);
            }

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
        }
    }
}
