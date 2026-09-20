using UnityEngine;
using UnityEngine.UI;

namespace Shootopia
{
    /// <summary>
    /// Manages a centered crosshair/reticle on the gameplay HUD.
    /// Auto-hides during menus, lobby, and ADS/scope overlay.
    /// Provides subtle scale feedback when firing.
    /// </summary>
    public class CrosshairUI : MonoBehaviour
    {
        public static CrosshairUI Instance { get; private set; }

        [Header("Crosshair Image")]
        [SerializeField] private Image crosshairImage;

        [Header("Behaviour")]
        [Tooltip("Base size of the crosshair in pixels.")]
        [SerializeField] private float baseSize = 24f;
        [Tooltip("Expanded size when firing.")]
        [SerializeField] private float expandedSize = 34f;
        [Tooltip("Speed of size lerp.")]
        [SerializeField] private float lerpSpeed = 12f;

        [Header("Color")]
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.85f);
        [SerializeField] private Color hitColor = new Color(1f, 0.3f, 0.3f, 1f);

        private RectTransform _rectTransform;
        private float _targetSize;
        private float _currentSize;
        private float _hitFlashTimer;
        private bool _visible = true;

        private void Awake()
        {
            Instance = this;

            if (crosshairImage == null)
                crosshairImage = GetComponent<Image>();

            if (crosshairImage != null)
                _rectTransform = crosshairImage.rectTransform;

            _currentSize = baseSize;
            _targetSize = baseSize;
            ApplySize(_currentSize);
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
            UpdateSize();
            UpdateHitFlash();
        }

        private void UpdateVisibility()
        {
            bool shouldShow = true;

            // Hide during menus
            if (WaitingRoomController.IsLobbyMenuOpen || PauseMenuController.IsMenuPaused)
                shouldShow = false;

            // Hide during ADS / scope overlay
            if (shouldShow)
            {
                var localPlayer = GetLocalPlayerWeaponController();
                if (localPlayer != null && localPlayer.IsADSActive)
                    shouldShow = false;
            }

            // Hide during pre-match countdown
            if (shouldShow && GameModeBase.Instance != null && GameModeBase.Instance.PreMatchCountdown.Value > 0)
                shouldShow = false;

            if (_visible != shouldShow)
            {
                _visible = shouldShow;
                if (crosshairImage != null)
                    crosshairImage.enabled = _visible;
            }
        }

        private void UpdateSize()
        {
            // Check if the local player is firing to expand crosshair
            var wc = GetLocalPlayerWeaponController();
            bool isFiring = wc != null && wc.IsArmed && wc.TimeSinceLastShot < 0.15f;
            _targetSize = isFiring ? expandedSize : baseSize;

            _currentSize = Mathf.Lerp(_currentSize, _targetSize, Time.deltaTime * lerpSpeed);
            ApplySize(_currentSize);
        }

        private void UpdateHitFlash()
        {
            if (_hitFlashTimer > 0f)
            {
                _hitFlashTimer -= Time.deltaTime;
                if (crosshairImage != null)
                {
                    crosshairImage.color = _hitFlashTimer > 0f ? hitColor : normalColor;
                }
            }
        }

        private void OnHitConfirmed(bool isKill)
        {
            _hitFlashTimer = isKill ? 0.4f : 0.2f;
            if (crosshairImage != null) crosshairImage.color = hitColor;
        }

        /// <summary>
        /// Temporarily expand the crosshair (e.g., on weapon fire).
        /// </summary>
        public void Pulse()
        {
            _currentSize = expandedSize;
        }

        private void ApplySize(float size)
        {
            if (_rectTransform != null)
            {
                _rectTransform.sizeDelta = new Vector2(size, size);
            }
        }

        private WeaponController _cachedWeaponController;
        private float _cacheTimer;

        private WeaponController GetLocalPlayerWeaponController()
        {
            _cacheTimer -= Time.deltaTime;
            if (_cachedWeaponController != null && _cacheTimer > 0f)
                return _cachedWeaponController;

            _cacheTimer = 1.0f; // Re-cache every second

            if (Unity.Netcode.NetworkManager.Singleton != null &&
                Unity.Netcode.NetworkManager.Singleton.LocalClient != null &&
                Unity.Netcode.NetworkManager.Singleton.LocalClient.PlayerObject != null)
            {
                _cachedWeaponController = Unity.Netcode.NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<WeaponController>();
            }

            return _cachedWeaponController;
        }
    }
}
