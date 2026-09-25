using UnityEngine;
using UnityEngine.UI;

namespace Shootopia
{
    /// <summary>
    /// Dynamic screen-space crosshair composed of a center dot and four gap lines.
    /// Expands slightly when moving, expands more when firing, and shows a brief
    /// hit-marker (4 diagonal ticks) on enemy hit. Stays fixed at screen centre.
    /// Auto-hides during menus, lobby, ADS/scope overlay, and pre-match countdown.
    /// </summary>
    public class CrosshairUI : MonoBehaviour
    {
        public static CrosshairUI Instance { get; private set; }

        [Header("Crosshair Elements")]
        [Tooltip("Parent RectTransform that holds all crosshair pieces.")]
        [SerializeField] private RectTransform crosshairRoot;

        [Header("Size Settings")]
        [Tooltip("Base gap between center and line edges (pixels).")]
        [SerializeField] private float baseGap = 6f;
        [Tooltip("Gap expansion when moving.")]
        [SerializeField] private float moveGapBonus = 4f;
        [Tooltip("Gap expansion when firing.")]
        [SerializeField] private float fireGapBonus = 8f;
        [Tooltip("Line length (pixels).")]
        [SerializeField] private float lineLength = 12f;
        [Tooltip("Line thickness (pixels).")]
        [SerializeField] private float lineThickness = 2f;
        [Tooltip("Center dot size (pixels).")]
        [SerializeField] private float dotSize = 3f;
        [Tooltip("Speed of size lerp.")]
        [SerializeField] private float lerpSpeed = 14f;

        [Header("Hit Marker")]
        [Tooltip("Hit-marker tick length (pixels).")]
        [SerializeField] private float hitTickLength = 10f;
        [Tooltip("Hit-marker offset from center.")]
        [SerializeField] private float hitTickOffset = 8f;
        [Tooltip("Hit-marker thickness (pixels).")]
        [SerializeField] private float hitTickThickness = 2f;

        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.85f);
        [SerializeField] private Color hitColor = new Color(1f, 0.3f, 0.3f, 1f);
        [SerializeField] private Color killColor = new Color(1f, 0.15f, 0.15f, 1f);

        // Runtime UI elements — created in code
        private Image _dot;
        private Image _lineTop, _lineBottom, _lineLeft, _lineRight;
        private Image _hitTL, _hitTR, _hitBL, _hitBR;
        private CanvasGroup _hitMarkerGroup;

        private float _currentGap;
        private float _targetGap;
        private float _hitFlashTimer;
        private bool _hitIsKill;
        private bool _visible = true;

        // Cached references
        private WeaponController _cachedWC;
        private CharacterMotor _cachedMotor;
        private float _cacheTimer;

        private void Awake()
        {
            Instance = this;
            BuildCrosshairUI();
            _currentGap = baseGap;
            _targetGap = baseGap;
        }

        private void OnEnable()
        {
            PlayerHealth.OnLocalPlayerHitConfirmed += OnHitConfirmed;
        }

        private void OnDisable()
        {
            PlayerHealth.OnLocalPlayerHitConfirmed -= OnHitConfirmed;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            UpdateVisibility();
            if (!_visible) return;

            UpdateSpread();
            LayoutLines();
            UpdateHitMarker();
        }

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

        #region Visibility

        private void UpdateVisibility()
        {
            bool shouldShow = true;

            // Hide during menus
            if (WaitingRoomController.IsLobbyMenuOpen || PauseMenuController.IsMenuPaused)
                shouldShow = false;

            // Hide during ADS / scope overlay
            if (shouldShow)
            {
                RefreshCache();
                if (_cachedWC != null && _cachedWC.IsADSActive)
                    shouldShow = false;
            }

            // Hide during pre-match countdown
            if (shouldShow && GameModeBase.Instance != null && GameModeBase.Instance.PreMatchCountdown.Value > 0)
                shouldShow = false;

            if (_visible != shouldShow)
            {
                _visible = shouldShow;
                if (crosshairRoot != null)
                {
                    // Use CanvasGroup or toggle all children
                    foreach (Transform child in crosshairRoot)
                    {
                        child.gameObject.SetActive(_visible);
                    }
                }
            }
        }

        #endregion

        #region Spread

        private void UpdateSpread()
        {
            RefreshCache();

            float gap = baseGap;

            // Movement expansion
            if (_cachedMotor != null && _cachedMotor.IsMoving)
            {
                gap += moveGapBonus;
            }

            // Fire expansion
            bool isFiring = _cachedWC != null && _cachedWC.IsArmed && _cachedWC.TimeSinceLastShot < 0.15f;
            if (isFiring)
            {
                gap += fireGapBonus;
            }

            _targetGap = gap;
            _currentGap = Mathf.Lerp(_currentGap, _targetGap, Time.deltaTime * lerpSpeed);
        }

        private void LayoutLines()
        {
            // Position cardinal lines around center with current gap
            float halfLine = lineLength * 0.5f;
            float offset = _currentGap + halfLine;

            if (_lineTop != null) _lineTop.rectTransform.anchoredPosition = new Vector2(0f, offset);
            if (_lineBottom != null) _lineBottom.rectTransform.anchoredPosition = new Vector2(0f, -offset);
            if (_lineLeft != null) _lineLeft.rectTransform.anchoredPosition = new Vector2(-offset, 0f);
            if (_lineRight != null) _lineRight.rectTransform.anchoredPosition = new Vector2(offset, 0f);
        }

        #endregion

        #region Hit Marker

        private void UpdateHitMarker()
        {
            if (_hitFlashTimer > 0f)
            {
                _hitFlashTimer -= Time.deltaTime;
                float alpha = Mathf.Clamp01(_hitFlashTimer / 0.15f);
                if (_hitMarkerGroup != null) _hitMarkerGroup.alpha = alpha;

                // Color the hit ticks
                Color tickColor = _hitIsKill ? killColor : hitColor;
                tickColor.a = alpha;
                SetHitTickColor(tickColor);
            }
            else if (_hitMarkerGroup != null && _hitMarkerGroup.alpha > 0f)
            {
                _hitMarkerGroup.alpha = 0f;
            }
        }

        private void OnHitConfirmed(bool isKill)
        {
            _hitIsKill = isKill;
            _hitFlashTimer = isKill ? 0.30f : 0.18f;
            if (_hitMarkerGroup != null) _hitMarkerGroup.alpha = 1f;
            SetHitTickColor(isKill ? killColor : hitColor);
        }

        private void SetHitTickColor(Color c)
        {
            if (_hitTL != null) _hitTL.color = c;
            if (_hitTR != null) _hitTR.color = c;
            if (_hitBL != null) _hitBL.color = c;
            if (_hitBR != null) _hitBR.color = c;
        }

        #endregion

        /// <summary>
        /// Temporarily spike the crosshair spread (e.g., on weapon fire).
        /// </summary>
        public void Pulse()
        {
            _currentGap = baseGap + fireGapBonus;
        }

        #region Cache

        private void RefreshCache()
        {
            _cacheTimer -= Time.deltaTime;
            if (_cachedWC != null && _cacheTimer > 0f) return;
            _cacheTimer = 1.0f;

            if (Unity.Netcode.NetworkManager.Singleton != null &&
                Unity.Netcode.NetworkManager.Singleton.LocalClient != null &&
                Unity.Netcode.NetworkManager.Singleton.LocalClient.PlayerObject != null)
            {
                var playerObj = Unity.Netcode.NetworkManager.Singleton.LocalClient.PlayerObject;
                _cachedWC = playerObj.GetComponent<WeaponController>();
                _cachedMotor = playerObj.GetComponent<CharacterMotor>();
            }
        }

        #endregion
    }
}
