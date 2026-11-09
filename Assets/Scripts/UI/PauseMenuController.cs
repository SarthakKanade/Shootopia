using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

namespace Shootopia
{
    /// <summary>
    /// Master UGUI Controller for the In-Match Tactical Pause Menu (ESC overlay).
    /// Manages live scoreboard table, match overview, audio/sens settings, and match departure.
    /// </summary>
    public partial class PauseMenuController : MonoBehaviour
    {
        public static PauseMenuController Instance { get; private set; }
        public static bool IsMenuPaused { get; private set; } = false;

        [Header("Pause Menu UI")]
        [SerializeField] private GameObject dimOverlay;
        [SerializeField] private GameObject pauseMenuRoot;
        [SerializeField] private CanvasGroup pauseCanvasGroup;
        [SerializeField] private Button btnResume;
        [SerializeField] private Button btnSettings;
        [SerializeField] private Button btnLeave;

        [Header("Tactical Scoreboard Table")]
        [SerializeField] private GameObject scoreRowPrefab;
        [SerializeField] private Transform scoreTableContent;
        [SerializeField] private TextMeshProUGUI pauseModeTitleText;
        [SerializeField] private TextMeshProUGUI pauseSubtitleText;

        private float _lastScoreboardRefreshTime = 0f;
        private bool _isTacticalLayoutBuilt = false;

        [Header("Settings Sub-Panel (Space Pack Modal)")]
        [SerializeField] private GameObject settingsSubPanel;
        [SerializeField] private CanvasGroup settingsCanvasGroup;
        [SerializeField] private Button btnCloseSettings;
        [SerializeField] private Slider sliderMasterVol, sliderMusicVol, sliderSFXVol, sliderMouseSens;
        [SerializeField] private TextMeshProUGUI labelMasterVol, labelMusicVol, labelSFXVol, labelMouseSens;
        [SerializeField] private Slider sliderFOV;
        [SerializeField] private TextMeshProUGUI labelFOV;
        [SerializeField] private Toggle toggleInvertY, toggleFullscreen;
        [SerializeField] private SpaceGUISwitch switchInvertY, switchFullscreen;
        [SerializeField] private Button btnMinusSens, btnPlusSens;
        [SerializeField] private Button btnActionLeave, btnActionReset, btnActionResume, btnActionControls;

        [Header("Controls Modal Sheet")]
        [SerializeField] private GameObject modalControls;
        [SerializeField] private CanvasGroup controlsCanvasGroup;
        [SerializeField] private Button btnCloseControls;

        [Header("Cross-Platform Controls Settings")]
        [SerializeField] private TMP_Dropdown dropdownControlMode;
        [SerializeField] private Slider sliderTouchSens;
        [SerializeField] private TextMeshProUGUI labelTouchSens;
        [SerializeField] private TMP_Dropdown dropdownJoystickMode;
        [SerializeField] private Slider sliderTouchMoveSens;
        [SerializeField] private TextMeshProUGUI labelTouchMoveSens;

        [Header("Split Touch Sensitivity & Haptics")]
        [SerializeField] private Slider sliderTouchSensX;
        [SerializeField] private TextMeshProUGUI labelTouchSensX;
        [SerializeField] private Slider sliderTouchSensY;
        [SerializeField] private TextMeshProUGUI labelTouchSensY;
        [SerializeField] private Slider sliderTouchSensADS;
        [SerializeField] private TextMeshProUGUI labelTouchSensADS;
        [SerializeField] private Toggle toggleHaptics;
        [SerializeField] private SpaceGUISwitch switchHaptics;
        [SerializeField] private SpaceGUISwitch switchJoystickMode;

        [Header("Settings Tabs")]
        [SerializeField] private Button btnTabGeneral;
        [SerializeField] private Button btnTabTouch;
        [SerializeField] private Button btnTabControls;
        [SerializeField] private GameObject tabContentGeneral;
        [SerializeField] private GameObject tabContentTouch;
        [SerializeField] private GameObject tabContentControls;

        private bool _isPaused = false;
        public bool IsPaused => _isPaused;

        private void Awake()
        {
            Instance = this;
            EnsureDimOverlay();
            if (dimOverlay != null) dimOverlay.SetActive(false);
            ApplyTacticalPauseStyling();
            ClampMenuScales();
        }

        private void EnsureDimOverlay()
        {
            if (dimOverlay != null) return;
            var dt = transform.Find("DimOverlay") ?? (transform.parent != null ? transform.parent.Find("DimOverlay") : null);
            if (dt != null) dimOverlay = dt.gameObject;
        }

        private void OnEnable()
        {
            RegisterCallbacks();
            InitSettings();
            TogglePause(false);
            GameModeBase.OnPlayerStatsUpdated += OnPlayerStatsUpdatedHandler;
        }

        private void OnPlayerStatsUpdatedHandler()
        {
            if (_isPaused)
            {
                RefreshLiveScoreboardData();
            }
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;

            bool isWaitingRoomWarmup = WaitingRoomController.Instance != null && !WaitingRoomController.Instance.IsMatchStarted;

            bool togglePressed = false;
            if (keyboard != null)
            {
                bool tabPressed = keyboard.tabKey.wasPressedThisFrame;
                if (tabPressed && isWaitingRoomWarmup)
                {
                    // Tab is reserved for WaitingRoom lobby modal during warmup
                    return;
                }

                togglePressed = keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame || tabPressed;
            }
            if (gamepad != null)
            {
                togglePressed |= gamepad.startButton.wasPressedThisFrame || gamepad.selectButton.wasPressedThisFrame;
            }

            if (togglePressed)
            {
                if (WaitingRoomController.IsLobbyMenuOpen) return;

                if (modalControls != null && modalControls.activeSelf)
                {
                    CloseControlsModal();
                    return;
                }

                if (settingsSubPanel != null && settingsSubPanel.activeSelf)
                {
                    CloseSettingsSubPanel();
                    return;
                }

                TogglePause(!_isPaused);
            }

            if (_isPaused && (Time.unscaledTime - _lastScoreboardRefreshTime >= 0.5f))
            {
                _lastScoreboardRefreshTime = Time.unscaledTime;
                RefreshLiveScoreboardData();
            }
        }

        public void TogglePause(bool pause)
        {
            _isPaused = pause;
            IsMenuPaused = pause;

            EnsureDimOverlay();
            if (dimOverlay != null) dimOverlay.SetActive(pause);

            if (pauseMenuRoot != null)
            {
                if (pause)
                {
                    ApplyTacticalPauseStyling();
                    ClampMenuScales();
                    RefreshLiveScoreboardData();
                    UIJuice.PopIn(pauseMenuRoot.GetComponent<RectTransform>(), pauseCanvasGroup);
                    CursorManager.PushUnlock("PauseMenu");
                }
                else
                {
                    UIJuice.PopOut(pauseMenuRoot.GetComponent<RectTransform>(), pauseCanvasGroup);
                    CursorManager.PopUnlock("PauseMenu");
                    CloseControlsModal();
                    CloseSettingsSubPanel();
                }
            }
        }

        public void ClampMenuScales()
        {
            if (pauseMenuRoot != null)
            {
                var rootRt = pauseMenuRoot.GetComponent<RectTransform>();
                if (rootRt != null)
                {
                    rootRt.anchoredPosition = Vector2.zero;
                    var canvas = pauseMenuRoot.GetComponentInParent<Canvas>();
                    if (canvas != null)
                    {
                        var cRt = canvas.GetComponent<RectTransform>();
                        if (cRt != null && cRt.rect.height > 0 && cRt.rect.width > 0)
                        {
                            float s = Mathf.Min(1f, (cRt.rect.height * 0.94f) / 680f, (cRt.rect.width * 0.94f) / 1000f);
                            rootRt.localScale = new Vector3(s, s, 1f);
                        }
                    }
                }
            }

            if (settingsSubPanel != null)
            {
                var sRt = settingsSubPanel.GetComponent<RectTransform>();
                if (sRt != null)
                {
                    sRt.anchoredPosition = Vector2.zero; // Strictly center on screen
                    var canvas = settingsSubPanel.GetComponentInParent<Canvas>();
                    if (canvas != null)
                    {
                        var cRt = canvas.GetComponent<RectTransform>();
                        if (cRt != null && cRt.rect.height > 0 && cRt.rect.width > 0)
                        {
                            float s = Mathf.Min(1f, (cRt.rect.height * 0.92f) / 930f, (cRt.rect.width * 0.92f) / 1485f);
                            sRt.localScale = new Vector3(s, s, 1f);
                        }
                    }
                }
            }
        }
    }
}
