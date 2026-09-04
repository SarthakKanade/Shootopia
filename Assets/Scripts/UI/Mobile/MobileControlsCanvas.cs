using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

namespace Shootopia
{
    /// <summary>
    /// Master controller for the Mobile Touch Controls UI overlay.
    /// Gathers virtual inputs from the joystick, touch look zone, and action buttons,
    /// and injects them directly into the local player's PlayerInputRouter.
    /// Automatically manages visibility based on DeviceProfileManager.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class MobileControlsCanvas : MonoBehaviour
    {
        public static MobileControlsCanvas Instance { get; private set; }

        [Header("Movement & Camera")]
        [SerializeField] private TouchVirtualJoystick movementJoystick;
        [SerializeField] private TouchLookZone lookZone;

        [Header("Combat Actions")]
        [SerializeField] private TouchActionButton fireButton;
        [SerializeField] private TouchActionButton altFireButton;
        [SerializeField] private TouchActionButton jumpButton;
        [SerializeField] private TouchActionButton dashButton;

        [Header("Navigation & Menu")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button scoreboardButton;

        [Header("Visual Display")]
        [SerializeField] private CanvasGroup canvasGroup;

        private PlayerInputRouter _localInputRouter;
        private CharacterMotor _localMotor;

        private void Awake()
        {
            Instance = this;
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

            if (fireButton != null)
            {
                fireButton.AllowDragToAim = true;
                fireButton.OnDragLook += (delta) =>
                {
                    EnsureLocalPlayerReference();
                    if (_localInputRouter != null)
                    {
                        _localInputRouter.ExternalLookDelta += delta * DeviceProfileManager.TouchSensitivity;
                    }
                };
            }

            if (altFireButton != null)
            {
                altFireButton.SetToggle(true);
            }

            if (jumpButton != null)
            {
                jumpButton.OnPressed += () =>
                {
                    EnsureLocalPlayerReference();
                    _localInputRouter?.QueueJump();
                };
            }

            if (dashButton != null)
            {
                dashButton.OnPressed += () =>
                {
                    EnsureLocalPlayerReference();
                    _localInputRouter?.QueueDash();
                };
            }

            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(OnPauseClicked);
            }
            if (scoreboardButton != null)
            {
                scoreboardButton.onClick.AddListener(OnScoreboardClicked);
            }
        }

        private void OnEnable()
        {
            DeviceProfileManager.OnControlModeChanged += HandleControlModeChanged;
            UpdateVisibility(DeviceProfileManager.EffectiveMode);
        }

        private void OnDisable()
        {
            DeviceProfileManager.OnControlModeChanged -= HandleControlModeChanged;
        }

        private void HandleControlModeChanged(InputControlMode mode)
        {
            UpdateVisibility(mode);
        }

        private void UpdateVisibility(InputControlMode mode)
        {
            bool showTouch = (mode == InputControlMode.MobileTouch);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = showTouch ? 1f : 0f;
                canvasGroup.interactable = showTouch;
                canvasGroup.blocksRaycasts = showTouch;
            }
        }

        public void ResetAltFireToggle()
        {
            if (altFireButton != null && altFireButton.IsPressed)
            {
                altFireButton.ResetState();
            }
        }

        private void OnPauseClicked()
        {
            if (PauseMenuController.Instance != null)
            {
                PauseMenuController.Instance.TogglePause(!PauseMenuController.Instance.IsPaused);
            }
        }

        private void OnScoreboardClicked()
        {
            if (ScoreboardController.Instance != null)
            {
                ScoreboardController.Instance.ToggleScoreboard();
            }
        }

        private void Update()
        {
            // If touch mode is disabled, skip injecting external intent
            if (!DeviceProfileManager.IsTouchActive) return;

            EnsureLocalPlayerReference();
            if (_localInputRouter == null) return;

            // Construct mobile touch intent
            var intent = new PlayerInputIntent();

            if (movementJoystick != null)
            {
                intent.Move = movementJoystick.InputValue;
            }

            if (fireButton != null) intent.Fire = fireButton.IsPressed;
            if (altFireButton != null) intent.AltFire = altFireButton.IsPressed;
            if (jumpButton != null) intent.Jump = jumpButton.ConsumeWasPressed();
            if (dashButton != null) intent.Dash = dashButton.ConsumeWasPressed();

            // Inject into PlayerInputRouter
            _localInputRouter.ExternalIntent = intent;

            if (lookZone != null)
            {
                Vector2 lookDelta = lookZone.ConsumeLookDelta();
                if (lookDelta.sqrMagnitude > 0.0001f)
                {
                    _localInputRouter.ExternalLookDelta += lookDelta;
                }
            }
        }

        private void EnsureLocalPlayerReference()
        {
            if (_localInputRouter != null && _localInputRouter.IsOwner) return;

            // Find local owned player
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null)
            {
                var localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
                if (localPlayer != null)
                {
                    _localInputRouter = localPlayer.GetComponent<PlayerInputRouter>();
                    _localMotor = localPlayer.GetComponent<CharacterMotor>();
                    return;
                }
            }

            // Fallback for offline / sandbox testing
            foreach (var router in FindObjectsByType<PlayerInputRouter>(FindObjectsSortMode.None))
            {
                if (router.IsOwner || !router.IsSpawned)
                {
                    _localInputRouter = router;
                    _localMotor = router.GetComponent<CharacterMotor>();
                    break;
                }
            }
        }
    }
}
