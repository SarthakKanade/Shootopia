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
    public partial class CrosshairUI : MonoBehaviour
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
        [SerializeField] private Color enemyTargetColor = new Color(1f, 0.2f, 0.2f, 1f);

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
        private bool _isTargetingEnemy = false;
        private float _targetCheckTimer = 0f;

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
            UpdateTargetDetection();
            UpdateHitMarker();
        }

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
