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
    public class PauseMenuController : MonoBehaviour
    {
        public static PauseMenuController Instance { get; private set; }
        public static bool IsMenuPaused { get; private set; } = false;

        [Header("Pause Menu UI")]
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
            ApplyTacticalPauseStyling();
            ClampMenuScales();
        }

        private void OnEnable()
        {
            RegisterCallbacks();
            InitSettings();
            TogglePause(false);
        }

        private void RegisterCallbacks()
        {
            if (btnResume != null) btnResume.onClick.AddListener(() => TogglePause(false));
            if (btnSettings != null) btnSettings.onClick.AddListener(OpenSettingsSubPanel);
            if (btnCloseSettings != null) btnCloseSettings.onClick.AddListener(CloseSettingsSubPanel);
            if (btnLeave != null) btnLeave.onClick.AddListener(OnLeaveMatchClicked);

            if (sliderMasterVol != null) sliderMasterVol.onValueChanged.AddListener(OnMasterVolumeChanged);
            if (sliderMusicVol != null) sliderMusicVol.onValueChanged.AddListener(OnMusicVolumeChanged);
            if (sliderSFXVol != null) sliderSFXVol.onValueChanged.AddListener(OnSFXVolumeChanged);
            if (sliderMouseSens != null) sliderMouseSens.onValueChanged.AddListener(OnMouseSensChanged);
            if (sliderFOV != null) sliderFOV.onValueChanged.AddListener(OnFOVChanged);

            if (toggleInvertY != null) toggleInvertY.onValueChanged.AddListener(OnInvertYToggled);
            if (toggleFullscreen != null) toggleFullscreen.onValueChanged.AddListener(OnFullscreenToggled);
            if (switchInvertY != null) switchInvertY.OnValueChanged += OnInvertYToggled;
            if (switchFullscreen != null) switchFullscreen.OnValueChanged += OnFullscreenToggled;

            if (btnMinusSens != null) btnMinusSens.onClick.AddListener(() => AdjustSensitivity(-0.2f));
            if (btnPlusSens != null) btnPlusSens.onClick.AddListener(() => AdjustSensitivity(+0.2f));

            if (btnActionLeave != null) btnActionLeave.onClick.AddListener(OnLeaveMatchClicked);
            if (btnActionReset != null) btnActionReset.onClick.AddListener(ResetSettingsToDefaults);
            if (btnActionControls != null) btnActionControls.onClick.AddListener(OpenControlsModal);
            if (btnActionResume != null) btnActionResume.onClick.AddListener(() => TogglePause(false));

            if (toggleHaptics != null) toggleHaptics.onValueChanged.AddListener(OnHapticsToggled);
            if (switchHaptics != null) switchHaptics.OnValueChanged += OnHapticsToggled;

            if (btnTabGeneral != null) btnTabGeneral.onClick.AddListener(() => SelectTab(0));
            if (btnTabTouch != null) btnTabTouch.onClick.AddListener(() => SelectTab(1));
            if (btnTabControls != null) btnTabControls.onClick.AddListener(() => SelectTab(2));
        }

        private void InitSettings()
        {
            // Remove Touch and Controls heading tab buttons from top header
            Transform tabBar = settingsSubPanel != null ? settingsSubPanel.transform.Find("Tab_Bar") : null;
            if (tabBar != null)
            {
                Transform touchTab = tabBar.Find("BtnTabTouch");
                if (touchTab != null) touchTab.gameObject.SetActive(false);

                Transform controlsTab = tabBar.Find("BtnTabControls");
                if (controlsTab != null) controlsTab.gameObject.SetActive(false);

                Transform generalTab = tabBar.Find("BtnTabGeneral");
                if (generalTab != null)
                {
                    var rt = generalTab.GetComponent<RectTransform>();
                    if (rt != null) rt.anchoredPosition = new Vector2(0f, 0f);
                    var txt = generalTab.GetComponentInChildren<TextMeshProUGUI>();
                    if (txt != null) txt.text = "SETTINGS";
                }
            }

            if (tabContentTouch != null) tabContentTouch.SetActive(false);
            if (tabContentControls != null) tabContentControls.SetActive(false);
            if (tabContentGeneral != null) tabContentGeneral.SetActive(true);

            // Wire Bottom Bar buttons to match Main Menu: RESET, CONTROLS, RESUME
            Transform bottomContainer = settingsSubPanel != null ? settingsSubPanel.transform.Find("Bottom_UI_Container") : null;
            if (bottomContainer != null)
            {
                Transform btn1 = bottomContainer.Find("BtnAction1");
                Transform btn2 = bottomContainer.Find("BtnAction2");
                Transform btn3 = bottomContainer.Find("BtnAction3");

                if (btn1 != null)
                {
                    btnActionReset = btn1.GetComponent<Button>();
                    var txt = btn1.GetComponentInChildren<TextMeshProUGUI>();
                    if (txt != null) txt.text = "RESET";
                    if (btnActionReset != null)
                    {
                        btnActionReset.onClick.RemoveAllListeners();
                        btnActionReset.onClick.AddListener(ResetSettingsToDefaults);
                    }
                }

                if (btn2 != null)
                {
                    btnActionControls = btn2.GetComponent<Button>();
                    var txt = btn2.GetComponentInChildren<TextMeshProUGUI>();
                    if (txt != null) txt.text = "CONTROLS";
                    if (btnActionControls != null)
                    {
                        btnActionControls.onClick.RemoveAllListeners();
                        btnActionControls.onClick.AddListener(OpenControlsModal);
                    }
                }

                if (btn3 != null)
                {
                    btnActionResume = btn3.GetComponent<Button>();
                    var txt = btn3.GetComponentInChildren<TextMeshProUGUI>();
                    if (txt != null) txt.text = "RESUME";
                    if (btnActionResume != null)
                    {
                        btnActionResume.onClick.RemoveAllListeners();
                        btnActionResume.onClick.AddListener(() => TogglePause(false));
                    }
                }
            }

            // Fallback bindings if serialized fields were not hooked up
            if (settingsSubPanel != null)
            {
                if (btnActionLeave == null)
                    btnActionLeave = settingsSubPanel.transform.Find("BtnActionLeaveTopLeft")?.GetComponent<Button>();

                if (sliderMasterVol == null)
                    sliderMasterVol = settingsSubPanel.transform.Find("TabContent_General/Slider_Container/Sound_Slider/Slider")?.GetComponent<Slider>();
                if (labelMasterVol == null)
                    labelMasterVol = settingsSubPanel.transform.Find("TabContent_General/Slider_Container/Sound_Slider/Val_TXT")?.GetComponent<TextMeshProUGUI>();

                if (sliderFOV == null)
                    sliderFOV = settingsSubPanel.transform.Find("TabContent_General/Slider_Container/Row_FOV/Slider")?.GetComponent<Slider>();
                if (labelFOV == null)
                    labelFOV = settingsSubPanel.transform.Find("TabContent_General/Slider_Container/Row_FOV/Val_TXT")?.GetComponent<TextMeshProUGUI>();
            }

            // Enable Top-Left Leave button in in-game pause settings
            if (btnActionLeave != null)
            {
                btnActionLeave.gameObject.SetActive(true);
                btnActionLeave.onClick.RemoveAllListeners();
                btnActionLeave.onClick.AddListener(OnLeaveMatchClicked);
            }

            float master = PlayerPrefs.GetFloat("AudioManager_MasterVolume", 1f);
            float music = PlayerPrefs.GetFloat("AudioManager_MusicVolume", 0.8f);
            float sfx = PlayerPrefs.GetFloat("AudioManager_SFXVolume", 1f);
            float sens = PlayerProfileManager.Instance != null ? PlayerProfileManager.Instance.MouseSensitivity : 1f;
            float fov = PlayerPrefs.GetFloat(ThirdPersonCameraController.PREFS_KEY_FOV, ThirdPersonCameraController.DEFAULT_FOV);
            if (Mathf.Approximately(fov, 80f) && !PlayerPrefs.HasKey("Shootopia_UserSetFOV"))
            {
                fov = ThirdPersonCameraController.DEFAULT_FOV;
            }
            bool invertY = (PlayerProfileManager.Instance != null && PlayerProfileManager.Instance.InvertY)
                        || (PlayerPrefs.GetInt("Shootopia_InvertY", 0) == 1);
            bool fullscreen = Screen.fullScreen;

            if (sliderMasterVol != null)
            {
                sliderMasterVol.value = master;
                sliderMasterVol.onValueChanged.RemoveListener(OnMasterVolumeChanged);
                sliderMasterVol.onValueChanged.AddListener(OnMasterVolumeChanged);
                if (labelMasterVol != null) labelMasterVol.text = $"{Mathf.RoundToInt(master * 100)}%";
            }
            if (sliderMusicVol != null && sliderMusicVol != sliderFOV) { sliderMusicVol.value = music; if (labelMusicVol != null) labelMusicVol.text = $"{Mathf.RoundToInt(music * 100)}%"; }
            if (sliderSFXVol != null) { sliderSFXVol.value = sfx; if (labelSFXVol != null) labelSFXVol.text = $"{Mathf.RoundToInt(sfx * 100)}%"; }
            if (sliderMouseSens != null) { sliderMouseSens.value = sens; if (labelMouseSens != null) labelMouseSens.text = $"{sens:F1}x"; }

            if (sliderFOV != null)
            {
                sliderFOV.minValue = ThirdPersonCameraController.MIN_FOV;
                sliderFOV.maxValue = ThirdPersonCameraController.MAX_FOV;
                sliderFOV.wholeNumbers = true;
                sliderFOV.value = fov;
                sliderFOV.onValueChanged.RemoveListener(OnFOVChanged);
                sliderFOV.onValueChanged.AddListener(OnFOVChanged);
                if (labelFOV != null) labelFOV.text = $"{Mathf.RoundToInt(fov)}°";
            }

            if (toggleInvertY != null) toggleInvertY.isOn = invertY;
            if (toggleFullscreen != null) toggleFullscreen.isOn = fullscreen;
            if (switchInvertY != null) switchInvertY.SetState(invertY, false);
            if (switchFullscreen != null) switchFullscreen.SetState(fullscreen, false);

            if (dropdownControlMode != null)
            {
                dropdownControlMode.ClearOptions();
                dropdownControlMode.AddOptions(new System.Collections.Generic.List<string> { "Auto (Recommended)", "Desktop (Keyboard & Mouse)", "Mobile (Touch Controls)" });
                dropdownControlMode.value = (int)DeviceProfileManager.ConfiguredMode;
                dropdownControlMode.onValueChanged.AddListener(OnControlModeSelected);
            }

            if (sliderTouchSens != null)
            {
                sliderTouchSens.minValue = 0.5f;
                sliderTouchSens.maxValue = 5.0f;
                sliderTouchSens.value = DeviceProfileManager.TouchSensitivityX;
                sliderTouchSens.onValueChanged.AddListener(OnTouchSensChanged);
                if (labelTouchSens != null) labelTouchSens.text = $"{DeviceProfileManager.TouchSensitivityX:F1}x";
            }

            if (sliderTouchSensX != null)
            {
                sliderTouchSensX.minValue = 0.5f;
                sliderTouchSensX.maxValue = 4.0f;
                sliderTouchSensX.value = DeviceProfileManager.TouchSensitivityX;
                sliderTouchSensX.onValueChanged.AddListener(OnTouchSensXChanged);
                if (labelTouchSensX != null) labelTouchSensX.text = $"{DeviceProfileManager.TouchSensitivityX:F1}x";
            }

            if (sliderTouchSensY != null)
            {
                sliderTouchSensY.minValue = 0.5f;
                sliderTouchSensY.maxValue = 4.0f;
                sliderTouchSensY.value = DeviceProfileManager.TouchSensitivityY;
                sliderTouchSensY.onValueChanged.AddListener(OnTouchSensYChanged);
                if (labelTouchSensY != null) labelTouchSensY.text = $"{DeviceProfileManager.TouchSensitivityY:F1}x";
            }

            if (sliderTouchSensADS != null)
            {
                sliderTouchSensADS.minValue = 0.2f;
                sliderTouchSensADS.maxValue = 1.5f;
                sliderTouchSensADS.value = DeviceProfileManager.TouchSensitivityADS;
                sliderTouchSensADS.onValueChanged.AddListener(OnTouchSensADSChanged);
                if (labelTouchSensADS != null) labelTouchSensADS.text = $"{DeviceProfileManager.TouchSensitivityADS:F2}x";
            }

            if (dropdownJoystickMode != null)
            {
                dropdownJoystickMode.ClearOptions();
                dropdownJoystickMode.AddOptions(new System.Collections.Generic.List<string> { "Fixed (Stationary)", "Dynamic (Follows Finger)" });
                dropdownJoystickMode.value = (int)DeviceProfileManager.JoystickMode;
                dropdownJoystickMode.onValueChanged.AddListener(OnJoystickModeSelected);
            }

            if (sliderTouchMoveSens != null)
            {
                sliderTouchMoveSens.minValue = 0.5f;
                sliderTouchMoveSens.maxValue = 2.5f;
                sliderTouchMoveSens.value = DeviceProfileManager.TouchMoveSensitivity;
                sliderTouchMoveSens.onValueChanged.AddListener(OnTouchMoveSensChanged);
                if (labelTouchMoveSens != null) labelTouchMoveSens.text = $"{DeviceProfileManager.TouchMoveSensitivity:F1}x";
            }

            bool hapticsOn = MobileHapticsManager.IsHapticsEnabled;
            if (toggleHaptics != null) toggleHaptics.isOn = hapticsOn;
            if (switchHaptics != null) switchHaptics.SetState(hapticsOn, false);

            if (switchJoystickMode != null)
            {
                bool isDynamic = DeviceProfileManager.JoystickMode == MobileJoystickMode.Dynamic;
                switchJoystickMode.SetState(isDynamic, false);
                switchJoystickMode.OnValueChanged += (val) =>
                {
                    DeviceProfileManager.JoystickMode = val ? MobileJoystickMode.Dynamic : MobileJoystickMode.Fixed;
                };
            }
        }

        private void OnControlModeSelected(int index)
        {
            DeviceProfileManager.ConfiguredMode = (InputControlMode)index;
        }

        private void OnTouchSensChanged(float val)
        {
            DeviceProfileManager.TouchSensitivityX = val;
            DeviceProfileManager.TouchSensitivityY = Mathf.Clamp(val * 0.75f, 0.5f, 4.0f);
            if (labelTouchSens != null) labelTouchSens.text = $"{val:F1}x";
            if (sliderTouchSensX != null) sliderTouchSensX.value = DeviceProfileManager.TouchSensitivityX;
            if (sliderTouchSensY != null) sliderTouchSensY.value = DeviceProfileManager.TouchSensitivityY;
        }

        private void OnTouchSensXChanged(float val)
        {
            DeviceProfileManager.TouchSensitivityX = val;
            if (labelTouchSensX != null) labelTouchSensX.text = $"{val:F1}x";
            if (labelTouchSens != null) labelTouchSens.text = $"{val:F1}x";
        }

        private void OnTouchSensYChanged(float val)
        {
            DeviceProfileManager.TouchSensitivityY = val;
            if (labelTouchSensY != null) labelTouchSensY.text = $"{val:F1}x";
        }

        private void OnTouchSensADSChanged(float val)
        {
            DeviceProfileManager.TouchSensitivityADS = val;
            if (labelTouchSensADS != null) labelTouchSensADS.text = $"{val:F2}x";
        }

        private void OnHapticsToggled(bool val)
        {
            MobileHapticsManager.SetHapticsEnabled(val);
            if (toggleHaptics != null) toggleHaptics.isOn = val;
            if (switchHaptics != null && switchHaptics.IsOn != val) switchHaptics.SetState(val, false);
        }

        private void OnJoystickModeSelected(int index)
        {
            DeviceProfileManager.JoystickMode = (MobileJoystickMode)index;
        }

        private void OnTouchMoveSensChanged(float val)
        {
            DeviceProfileManager.TouchMoveSensitivity = val;
            if (labelTouchMoveSens != null) labelTouchMoveSens.text = $"{val:F1}x";
        }

        private void OnMasterVolumeChanged(float val)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetChannelVolume(AudioChannel.Master, val);
            if (labelMasterVol == null && settingsSubPanel != null)
                labelMasterVol = settingsSubPanel.transform.Find("TabContent_General/Slider_Container/Sound_Slider/Val_TXT")?.GetComponent<TextMeshProUGUI>();
            if (labelMasterVol != null) labelMasterVol.text = $"{Mathf.RoundToInt(val * 100)}%";
        }

        private void OnMusicVolumeChanged(float val)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetChannelVolume(AudioChannel.Music, val);
            if (labelMusicVol != null) labelMusicVol.text = $"{Mathf.RoundToInt(val * 100)}%";
        }

        private void OnSFXVolumeChanged(float val)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetChannelVolume(AudioChannel.SFX, val);
            if (labelSFXVol != null) labelSFXVol.text = $"{Mathf.RoundToInt(val * 100)}%";
        }

        private void OnMouseSensChanged(float val)
        {
            if (PlayerProfileManager.Instance != null)
            {
                PlayerProfileManager.Instance.SetMouseSensitivity(val);
            }
            if (labelMouseSens != null) labelMouseSens.text = $"{val:F1}x";
        }

        private void AdjustSensitivity(float delta)
        {
            float current = PlayerProfileManager.Instance != null ? PlayerProfileManager.Instance.MouseSensitivity : 1.0f;
            float newSens = Mathf.Clamp((float)System.Math.Round(current + delta, 1), 0.2f, 3.0f);
            OnMouseSensChanged(newSens);
            if (sliderMouseSens != null) sliderMouseSens.value = newSens;
        }

        private void OnFOVChanged(float val)
        {
            if (labelFOV == null && settingsSubPanel != null)
                labelFOV = settingsSubPanel.transform.Find("TabContent_General/Slider_Container/Row_FOV/Val_TXT")?.GetComponent<TextMeshProUGUI>();
            if (labelFOV != null) labelFOV.text = $"{Mathf.RoundToInt(val)}°";
            PlayerPrefs.SetFloat(ThirdPersonCameraController.PREFS_KEY_FOV, val);
            PlayerPrefs.SetInt("Shootopia_UserSetFOV", 1);
            PlayerPrefs.Save();
            if (ThirdPersonCameraController.Instance != null)
            {
                ThirdPersonCameraController.Instance.SetUserFOV(val);
            }
        }

        private void ResetSettingsToDefaults()
        {
            if (sliderMasterVol != null) sliderMasterVol.value = 1.0f;
            if (sliderMusicVol != null) sliderMusicVol.value = 0.8f;
            if (sliderSFXVol != null) sliderSFXVol.value = 1.0f;
            OnMouseSensChanged(1.0f);
            if (sliderMouseSens != null) sliderMouseSens.value = 1.0f;
            if (sliderFOV != null) sliderFOV.value = ThirdPersonCameraController.DEFAULT_FOV;
            OnFOVChanged(ThirdPersonCameraController.DEFAULT_FOV);
            if (switchInvertY != null) switchInvertY.SetState(false, true);
            if (switchFullscreen != null) switchFullscreen.SetState(true, true);

            if (DeviceProfileManager.Instance != null)
            {
                DeviceProfileManager.Instance.ResetTouchSettingsToDefaults();
            }
            if (sliderTouchSensX != null) sliderTouchSensX.value = 1.70f;
            if (sliderTouchSensY != null) sliderTouchSensY.value = 1.30f;
            if (sliderTouchSensADS != null) sliderTouchSensADS.value = 0.65f;
            if (sliderTouchSens != null) sliderTouchSens.value = 1.70f;
            if (sliderTouchMoveSens != null) sliderTouchMoveSens.value = 1.40f;
            if (dropdownJoystickMode != null) dropdownJoystickMode.value = (int)MobileJoystickMode.Fixed;
            OnHapticsToggled(true);
        }

        private void OnInvertYToggled(bool val)
        {
            if (PlayerProfileManager.Instance != null)
            {
                PlayerProfileManager.Instance.SetInvertY(val);
            }
            PlayerPrefs.SetInt("Shootopia_InvertY", val ? 1 : 0);
            PlayerPrefs.Save();
            if (toggleInvertY != null) toggleInvertY.isOn = val;
            if (switchInvertY != null && switchInvertY.IsOn != val) switchInvertY.SetState(val, false);
        }

        private void OnFullscreenToggled(bool val)
        {
            Screen.fullScreen = val;
            if (toggleFullscreen != null) toggleFullscreen.isOn = val;
            if (switchFullscreen != null && switchFullscreen.IsOn != val) switchFullscreen.SetState(val, false);
        }

        private void OnDisable()
        {
            if (_isPaused)
            {
                CursorManager.PopUnlock("PauseMenu");
                _isPaused = false;
                IsMenuPaused = false;
            }
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;

            bool togglePressed = false;
            if (keyboard != null)
            {
                togglePressed = keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame;
            }
            if (gamepad != null)
            {
                togglePressed |= gamepad.startButton.wasPressedThisFrame;
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

        public void OpenSettingsSubPanel()
        {
            if (pauseMenuRoot != null) pauseMenuRoot.SetActive(false);
            if (settingsSubPanel != null)
            {
                settingsSubPanel.SetActive(true);
                ClampMenuScales();
                InitSettings();
                var cg = settingsCanvasGroup != null ? settingsCanvasGroup : settingsSubPanel.GetComponent<CanvasGroup>();
                UIJuice.PopIn(settingsSubPanel.GetComponent<RectTransform>(), cg);
                SelectTab(0);
            }
        }

        public void SelectTab(int tabIndex)
        {
            if (tabContentGeneral != null) tabContentGeneral.SetActive(true);
            if (tabContentTouch != null) tabContentTouch.SetActive(false);
            if (tabContentControls != null) tabContentControls.SetActive(false);

            if (btnTabGeneral != null)
            {
                var img = btnTabGeneral.GetComponent<Image>();
                if (img != null) img.color = Color.white;
            }
        }

        public void CloseSettingsSubPanel()
        {
            CloseControlsModal();
            if (settingsSubPanel != null)
            {
                var cg = settingsCanvasGroup != null ? settingsCanvasGroup : settingsSubPanel.GetComponent<CanvasGroup>();
                UIJuice.PopOut(settingsSubPanel.GetComponent<RectTransform>(), cg);
                settingsSubPanel.SetActive(false);
            }
            if (_isPaused && pauseMenuRoot != null)
            {
                pauseMenuRoot.SetActive(true);
                ClampMenuScales();
                RefreshLiveScoreboardData();
                UIJuice.PopIn(pauseMenuRoot.GetComponent<RectTransform>(), pauseCanvasGroup);
            }
        }

        // ──────────────────────── CONTROLS REFERENCE ────────────────────────

        public void OpenControlsModal()
        {
            EnsureControlsModalUI();
            if (modalControls != null)
            {
                modalControls.SetActive(true);
                var cg = controlsCanvasGroup != null ? controlsCanvasGroup : modalControls.GetComponent<CanvasGroup>();
                if (cg != null) UIJuice.PopIn(modalControls.GetComponent<RectTransform>(), cg);
            }
        }

        public void CloseControlsModal()
        {
            if (modalControls != null)
            {
                var cg = controlsCanvasGroup != null ? controlsCanvasGroup : modalControls.GetComponent<CanvasGroup>();
                if (cg != null) UIJuice.PopOut(modalControls.GetComponent<RectTransform>(), cg);
                modalControls.SetActive(false);
            }
        }

        private void EnsureControlsModalUI()
        {
            if (modalControls != null) return;

            Transform canvasTr = settingsSubPanel != null ? settingsSubPanel.transform.parent : transform;
            Transform existing = canvasTr.Find("ModalControlsSheet");
            if (existing != null)
            {
                modalControls = existing.gameObject;
                controlsCanvasGroup = modalControls.GetComponent<CanvasGroup>();
                btnCloseControls = modalControls.GetComponentInChildren<Button>();
                if (btnCloseControls != null)
                {
                    btnCloseControls.onClick.RemoveAllListeners();
                    btnCloseControls.onClick.AddListener(CloseControlsModal);
                }
                return;
            }

            var modalObj = new GameObject("ModalControlsSheet", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            modalObj.transform.SetParent(canvasTr, false);
            modalObj.transform.SetAsLastSibling();

            var mRt = modalObj.GetComponent<RectTransform>();
            mRt.anchorMin = new Vector2(0.5f, 0.5f);
            mRt.anchorMax = new Vector2(0.5f, 0.5f);
            mRt.sizeDelta = new Vector2(1060f, 660f);
            mRt.anchoredPosition = Vector2.zero;

            var mImg = modalObj.GetComponent<Image>();
            mImg.color = new Color(0.18f, 0.72f, 1.0f, 0.45f);

            controlsCanvasGroup = modalObj.GetComponent<CanvasGroup>();

            var backdropObj = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
            backdropObj.transform.SetParent(modalObj.transform, false);
            var bRt = backdropObj.GetComponent<RectTransform>();
            bRt.anchorMin = Vector2.zero;
            bRt.anchorMax = Vector2.one;
            bRt.sizeDelta = new Vector2(-16f, -16f);
            bRt.anchoredPosition = Vector2.zero;
            var bImg = backdropObj.GetComponent<Image>();
            bImg.color = new Color(0.04f, 0.07f, 0.14f, 0.96f);
            bImg.raycastTarget = false;

            var titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(modalObj.transform, false);
            var tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.5f, 1f);
            tRt.anchorMax = new Vector2(0.5f, 1f);
            tRt.pivot = new Vector2(0.5f, 1f);
            tRt.sizeDelta = new Vector2(980f, 40f);
            tRt.anchoredPosition = new Vector2(0f, -22f);

            var tTmp = titleObj.GetComponent<TextMeshProUGUI>();
            tTmp.text = "CONTROLS & KEYBINDINGS";
            tTmp.fontSize = 28;
            tTmp.fontStyle = FontStyles.Bold;
            tTmp.color = new Color(0.92f, 0.97f, 1f, 1f);
            tTmp.alignment = TextAlignmentOptions.Center;

            var subObj = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            subObj.transform.SetParent(modalObj.transform, false);
            var sRt = subObj.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.5f, 1f);
            sRt.anchorMax = new Vector2(0.5f, 1f);
            sRt.pivot = new Vector2(0.5f, 1f);
            sRt.sizeDelta = new Vector2(980f, 24f);
            sRt.anchoredPosition = new Vector2(0f, -60f);

            var sTmp = subObj.GetComponent<TextMeshProUGUI>();
            sTmp.text = "FULL INPUT SPECIFICATION • KEYBOARD / MOUSE & GAMEPAD";
            sTmp.fontSize = 14;
            sTmp.fontStyle = FontStyles.Normal;
            sTmp.color = new Color(0.6f, 0.78f, 0.95f, 1f);
            sTmp.alignment = TextAlignmentOptions.Center;

            var sheetContainer = new GameObject("SheetContainer", typeof(RectTransform));
            sheetContainer.transform.SetParent(modalObj.transform, false);
            var scRt = sheetContainer.GetComponent<RectTransform>();
            scRt.anchorMin = new Vector2(0.5f, 0.5f);
            scRt.anchorMax = new Vector2(0.5f, 0.5f);
            scRt.sizeDelta = new Vector2(1000f, 480f);
            scRt.anchoredPosition = new Vector2(0f, -10f);

            BuildControlsSheetView(sheetContainer.transform, tTmp.font);

            var closeBtnObj = new GameObject("BtnCloseControls", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIJuice));
            closeBtnObj.transform.SetParent(modalObj.transform, false);
            var cRt = closeBtnObj.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0f);
            cRt.anchorMax = new Vector2(0.5f, 0f);
            cRt.pivot = new Vector2(0.5f, 0f);
            cRt.sizeDelta = new Vector2(260f, 50f);
            cRt.anchoredPosition = new Vector2(0f, 24f);

            var cImg = closeBtnObj.GetComponent<Image>();
            cImg.color = new Color(0.12f, 0.25f, 0.45f, 0.95f);

            var cbTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            cbTxtObj.transform.SetParent(closeBtnObj.transform, false);
            var cbtRt = cbTxtObj.GetComponent<RectTransform>();
            cbtRt.anchorMin = Vector2.zero;
            cbtRt.anchorMax = Vector2.one;
            cbtRt.sizeDelta = Vector2.zero;

            var cbtTmp = cbTxtObj.GetComponent<TextMeshProUGUI>();
            cbtTmp.text = "BACK TO SETTINGS";
            cbtTmp.fontSize = 16;
            cbtTmp.fontStyle = FontStyles.Bold;
            cbtTmp.color = new Color(0.2f, 0.85f, 1.0f, 1f);
            cbtTmp.alignment = TextAlignmentOptions.Center;

            btnCloseControls = closeBtnObj.GetComponent<Button>();
            btnCloseControls.onClick.AddListener(CloseControlsModal);

            modalControls = modalObj;
            modalControls.SetActive(false);
        }

        public static GameObject BuildControlsSheetView(Transform parent, TMP_FontAsset font = null)
        {
            var rootObj = new GameObject("TabContentControls", typeof(RectTransform));
            rootObj.transform.SetParent(parent, false);

            var rRt = rootObj.GetComponent<RectTransform>();
            rRt.anchorMin = Vector2.zero;
            rRt.anchorMax = Vector2.one;
            rRt.sizeDelta = Vector2.zero;
            rRt.anchoredPosition = Vector2.zero;

            var scrollObj = new GameObject("ControlsScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image), typeof(Mask));
            scrollObj.transform.SetParent(rootObj.transform, false);

            var sRt = scrollObj.GetComponent<RectTransform>();
            sRt.anchorMin = Vector2.zero;
            sRt.anchorMax = Vector2.one;
            sRt.sizeDelta = new Vector2(-16f, -16f);
            sRt.anchoredPosition = Vector2.zero;

            var sImg = scrollObj.GetComponent<Image>();
            sImg.color = new Color(0.02f, 0.04f, 0.09f, 0.6f);

            var mask = scrollObj.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            var contentObj = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            contentObj.transform.SetParent(scrollObj.transform, false);

            var cRt = contentObj.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0f, 1f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.sizeDelta = new Vector2(0f, 0f);

            var hlg = contentObj.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(12, 12, 12, 12);
            hlg.spacing = 16f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = false;

            var csf = contentObj.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var sr = scrollObj.GetComponent<ScrollRect>();
            sr.content = cRt;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;

            // Left Card: KEYBOARD & MOUSE
            BuildControlsCategoryCard(contentObj.transform, "KEYBOARD & MOUSE", new Color(0.18f, 0.85f, 1.0f, 1f), new (string, string)[]
            {
                ("Movement", "W, A, S, D"),
                ("Look / Aim", "Mouse Look"),
                ("Sprint / Dash", "Left Shift"),
                ("Jump", "Spacebar"),
                ("Tactical Roll / Evade", "Left Ctrl / C"),
                ("Fire Weapon", "Left Mouse Button"),
                ("Aim Down Sights (ADS)", "Right Mouse Button"),
                ("Melee Strike", "V / Middle Click"),
                ("Reload Weapon", "R"),
                ("Switch Weapon", "1, 2 / Scroll Wheel"),
                ("Deployable Trip Mine", "F"),
                ("Tactical Throwable", "G"),
                ("Kinetic Shield", "E"),
                ("Optical Invisibility", "Q"),
                ("Tactical Scoreboard", "Tab (Hold)"),
                ("Tactical Pause Menu", "Esc / P")
            }, font);

            // Right Card: GAMEPAD CONTROLLER
            BuildControlsCategoryCard(contentObj.transform, "GAMEPAD CONTROLLER", new Color(1.0f, 0.82f, 0.25f, 1f), new (string, string)[]
            {
                ("Movement", "Left Stick"),
                ("Look / Aim", "Right Stick"),
                ("Sprint / Dash", "L3 (Left Stick Click)"),
                ("Jump", "A / Cross Button"),
                ("Tactical Roll / Evade", "B / Circle Button"),
                ("Fire Weapon", "RT / R2 (Right Trigger)"),
                ("Aim Down Sights (ADS)", "LT / L2 (Left Trigger)"),
                ("Melee Strike", "R3 (Right Stick Click)"),
                ("Reload Weapon", "X / Square Button"),
                ("Switch Weapon", "Y / Triangle Button"),
                ("Deployable Trip Mine", "RB / R1 (Right Bumper)"),
                ("Tactical Throwable", "LB / L1 (Left Bumper)"),
                ("Kinetic Shield", "D-Pad Left"),
                ("Optical Invisibility", "D-Pad Right"),
                ("Tactical Scoreboard", "View / Back / Select"),
                ("Tactical Pause Menu", "Menu / Start / Options")
            }, font);

            return rootObj;
        }

        private static void BuildControlsCategoryCard(Transform parent, string headerTitle, Color accentColor, (string action, string binding)[] bindings, TMP_FontAsset font)
        {
            var cardObj = new GameObject($"Card_{headerTitle}", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            cardObj.transform.SetParent(parent, false);

            var cImg = cardObj.GetComponent<Image>();
            cImg.color = new Color(0.04f, 0.08f, 0.16f, 0.92f);

            var vlg = cardObj.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 14, 14);
            vlg.spacing = 8f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var headerObj = new GameObject("Header", typeof(RectTransform), typeof(TextMeshProUGUI));
            headerObj.transform.SetParent(cardObj.transform, false);

            var hRt = headerObj.GetComponent<RectTransform>();
            hRt.sizeDelta = new Vector2(0f, 32f);

            var hTmp = headerObj.GetComponent<TextMeshProUGUI>();
            hTmp.text = headerTitle;
            hTmp.fontSize = 17;
            hTmp.fontStyle = FontStyles.Bold;
            hTmp.color = accentColor;
            hTmp.alignment = TextAlignmentOptions.MidlineLeft;
            if (font != null) hTmp.font = font;

            for (int i = 0; i < bindings.Length; i++)
            {
                var rowObj = new GameObject($"Row_{i}", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
                rowObj.transform.SetParent(cardObj.transform, false);

                var rRt = rowObj.GetComponent<RectTransform>();
                rRt.sizeDelta = new Vector2(0f, 30f);

                var rImg = rowObj.GetComponent<Image>();
                rImg.color = (i % 2 == 0) ? new Color(0.07f, 0.12f, 0.22f, 0.6f) : new Color(0.04f, 0.07f, 0.14f, 0.4f);

                var rHlg = rowObj.GetComponent<HorizontalLayoutGroup>();
                rHlg.padding = new RectOffset(12, 12, 4, 4);
                rHlg.childAlignment = TextAnchor.MiddleLeft;
                rHlg.childControlWidth = false;
                rHlg.childControlHeight = true;
                rHlg.childForceExpandWidth = false;
                rHlg.spacing = 10;

                var actionObj = new GameObject("Action", typeof(RectTransform), typeof(TextMeshProUGUI));
                actionObj.transform.SetParent(rowObj.transform, false);
                var aRt = actionObj.GetComponent<RectTransform>();
                aRt.sizeDelta = new Vector2(190f, 24f);

                var aTmp = actionObj.GetComponent<TextMeshProUGUI>();
                aTmp.text = bindings[i].action;
                aTmp.fontSize = 13;
                aTmp.fontStyle = FontStyles.Normal;
                aTmp.color = new Color(0.85f, 0.92f, 1f, 0.95f);
                aTmp.alignment = TextAlignmentOptions.MidlineLeft;
                if (font != null) aTmp.font = font;

                var badgeObj = new GameObject("Badge", typeof(RectTransform), typeof(Image));
                badgeObj.transform.SetParent(rowObj.transform, false);
                var bRt = badgeObj.GetComponent<RectTransform>();
                bRt.sizeDelta = new Vector2(170f, 22f);

                var bImg = badgeObj.GetComponent<Image>();
                bImg.color = new Color(0.12f, 0.18f, 0.32f, 0.9f);

                var keyObj = new GameObject("KeyText", typeof(RectTransform), typeof(TextMeshProUGUI));
                keyObj.transform.SetParent(badgeObj.transform, false);
                var kRt = keyObj.GetComponent<RectTransform>();
                kRt.anchorMin = Vector2.zero;
                kRt.anchorMax = Vector2.one;
                kRt.sizeDelta = Vector2.zero;

                var kTmp = keyObj.GetComponent<TextMeshProUGUI>();
                kTmp.text = bindings[i].binding;
                kTmp.fontSize = 12;
                kTmp.fontStyle = FontStyles.Bold;
                kTmp.color = accentColor;
                kTmp.alignment = TextAlignmentOptions.Center;
                if (font != null) kTmp.font = font;
            }
        }

        // ──────────────────────── TACTICAL PAUSE & SCOREBOARD STYLING ────────────────────────

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

        private void ApplyTacticalPauseStyling()
        {
            if (_isTacticalLayoutBuilt || pauseMenuRoot == null) return;
            _isTacticalLayoutBuilt = true;

            var rootRt = pauseMenuRoot.GetComponent<RectTransform>();
            if (rootRt != null)
            {
                rootRt.sizeDelta = new Vector2(1000f, 680f);
                rootRt.anchoredPosition = Vector2.zero;
            }

            var rootImg = pauseMenuRoot.GetComponent<Image>();
            if (rootImg != null)
            {
                rootImg.color = new Color(0.18f, 0.72f, 1.0f, 0.45f);
            }

            // Dark inner backdrop matching WaitingRoom / Results screen
            Transform backdropTr = pauseMenuRoot.transform.Find("ModalBackdrop");
            if (backdropTr == null)
            {
                var backdropObj = new GameObject("ModalBackdrop", typeof(RectTransform), typeof(Image));
                backdropObj.transform.SetParent(pauseMenuRoot.transform, false);
                backdropObj.transform.SetSiblingIndex(0);

                var bRt = backdropObj.GetComponent<RectTransform>();
                bRt.anchorMin = Vector2.zero;
                bRt.anchorMax = Vector2.one;
                bRt.sizeDelta = new Vector2(-16f, -16f);
                bRt.anchoredPosition = Vector2.zero;

                var bImg = backdropObj.GetComponent<Image>();
                bImg.color = new Color(0.04f, 0.07f, 0.14f, 0.96f);
                bImg.raycastTarget = false;
            }

            // Reposition Title
            Transform titleTr = pauseMenuRoot.transform.Find("Title");
            if (titleTr != null)
            {
                var tRt = titleTr.GetComponent<RectTransform>();
                if (tRt != null)
                {
                    tRt.anchorMin = new Vector2(0.5f, 0.5f);
                    tRt.anchorMax = new Vector2(0.5f, 0.5f);
                    tRt.sizeDelta = new Vector2(880f, 44f);
                    tRt.anchoredPosition = new Vector2(0f, 275f);
                }

                var tmp = titleTr.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = "TACTICAL PAUSE";
                    tmp.fontSize = 32;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.color = new Color(0.92f, 0.97f, 1f, 1f);
                    tmp.alignment = TextAlignmentOptions.Center;
                    pauseModeTitleText = tmp;
                }
            }

            // Subtitle
            Transform subTr = pauseMenuRoot.transform.Find("PauseSubtitle");
            if (subTr == null)
            {
                var subObj = new GameObject("PauseSubtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
                subObj.transform.SetParent(pauseMenuRoot.transform, false);

                var sRt = subObj.GetComponent<RectTransform>();
                sRt.anchorMin = new Vector2(0.5f, 0.5f);
                sRt.anchorMax = new Vector2(0.5f, 0.5f);
                sRt.sizeDelta = new Vector2(880f, 26f);
                sRt.anchoredPosition = new Vector2(0f, 238f);

                var sTmp = subObj.GetComponent<TextMeshProUGUI>();
                sTmp.text = "LIVE MATCH OVERVIEW • PLAYER LEADERBOARD";
                sTmp.fontSize = 15;
                sTmp.fontStyle = FontStyles.Normal;
                sTmp.color = new Color(0.6f, 0.78f, 0.95f, 1f);
                sTmp.alignment = TextAlignmentOptions.Center;
                if (pauseModeTitleText != null) sTmp.font = pauseModeTitleText.font;
                pauseSubtitleText = sTmp;
            }

            // Scoreboard Table Column Headers
            Transform headerBarTr = pauseMenuRoot.transform.Find("ScoreTableHeader");
            if (headerBarTr == null)
            {
                var headerObj = new GameObject("ScoreTableHeader", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
                headerObj.transform.SetParent(pauseMenuRoot.transform, false);

                var hRt = headerObj.GetComponent<RectTransform>();
                hRt.anchorMin = new Vector2(0.5f, 0.5f);
                hRt.anchorMax = new Vector2(0.5f, 0.5f);
                hRt.sizeDelta = new Vector2(880f, 36f);
                hRt.anchoredPosition = new Vector2(0f, 195f);

                var hImg = headerObj.GetComponent<Image>();
                hImg.color = new Color(0.06f, 0.08f, 0.16f, 0.96f);

                var hlg = headerObj.GetComponent<HorizontalLayoutGroup>();
                hlg.padding = new RectOffset(24, 24, 4, 4);
                hlg.childAlignment = TextAnchor.MiddleLeft;
                hlg.childControlWidth = false;
                hlg.childControlHeight = true;
                hlg.spacing = 10;

                CreateHeaderCol("RankHeader", headerObj.transform, "RANK", 65f, TextAlignmentOptions.MidlineLeft);
                CreateHeaderCol("PlayerHeader", headerObj.transform, "PLAYER", 320f, TextAlignmentOptions.MidlineLeft);
                CreateHeaderCol("KDHeader", headerObj.transform, "KILLS / DEATHS", 195f, TextAlignmentOptions.Center);
                CreateHeaderCol("ScoreHeader", headerObj.transform, "SCORE", 135f, TextAlignmentOptions.MidlineRight);
                CreateHeaderCol("PingHeader", headerObj.transform, "PING", 95f, TextAlignmentOptions.MidlineRight);
            }

            // Scroll View for live score rows
            Transform scrollTr = pauseMenuRoot.transform.Find("PauseScoreScrollView");
            if (scrollTr == null)
            {
                var scrollObj = new GameObject("PauseScoreScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image), typeof(Mask));
                scrollObj.transform.SetParent(pauseMenuRoot.transform, false);

                var sRt = scrollObj.GetComponent<RectTransform>();
                sRt.anchorMin = new Vector2(0.5f, 0.5f);
                sRt.anchorMax = new Vector2(0.5f, 0.5f);
                sRt.sizeDelta = new Vector2(880f, 350f);
                sRt.anchoredPosition = new Vector2(0f, -5f);

                var sImg = scrollObj.GetComponent<Image>();
                sImg.color = new Color(0.02f, 0.04f, 0.08f, 0.65f);

                var mask = scrollObj.GetComponent<Mask>();
                mask.showMaskGraphic = true;

                var contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                contentObj.transform.SetParent(scrollObj.transform, false);

                var cRt = contentObj.GetComponent<RectTransform>();
                cRt.anchorMin = new Vector2(0f, 1f);
                cRt.anchorMax = new Vector2(1f, 1f);
                cRt.pivot = new Vector2(0.5f, 1f);
                cRt.sizeDelta = new Vector2(0f, 0f);

                var vlg = contentObj.GetComponent<VerticalLayoutGroup>();
                vlg.padding = new RectOffset(8, 8, 8, 8);
                vlg.spacing = 6f;
                vlg.childControlWidth = true;
                vlg.childControlHeight = false;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;

                var csf = contentObj.GetComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                var sr = scrollObj.GetComponent<ScrollRect>();
                sr.content = cRt;
                sr.horizontal = false;
                sr.vertical = true;
                sr.movementType = ScrollRect.MovementType.Clamped;

                scoreTableContent = contentObj.transform;
            }

            // Bottom Action Button Stack
            Transform actionBarTr = pauseMenuRoot.transform.Find("ActionBar");
            if (actionBarTr == null)
            {
                var barObj = new GameObject("ActionBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                barObj.transform.SetParent(pauseMenuRoot.transform, false);
                actionBarTr = barObj.transform;

                var bsRt = barObj.GetComponent<RectTransform>();
                bsRt.anchorMin = new Vector2(0.5f, 0.5f);
                bsRt.anchorMax = new Vector2(0.5f, 0.5f);
                bsRt.sizeDelta = new Vector2(880f, 56f);
                bsRt.anchoredPosition = new Vector2(0f, -265f);

                var hlg = barObj.GetComponent<HorizontalLayoutGroup>();
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.spacing = 20f;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;

                if (btnResume != null) btnResume.transform.SetParent(barObj.transform, false);
                if (btnSettings != null) btnSettings.transform.SetParent(barObj.transform, false);
                if (btnLeave != null) btnLeave.transform.SetParent(barObj.transform, false);

                Transform oldStack = pauseMenuRoot.transform.Find("ButtonStack");
                if (oldStack != null && oldStack != barObj.transform) oldStack.gameObject.SetActive(false);
            }

            StyleActionButton(btnResume, new Vector2(260f, 52f), "RESUME MATCH", new Color(0.2f, 0.8f, 1f, 1f));
            StyleActionButton(btnSettings, new Vector2(220f, 52f), "SETTINGS", new Color(1f, 0.82f, 0.2f, 1f));
            StyleActionButton(btnLeave, new Vector2(220f, 52f), "LEAVE MATCH", new Color(1f, 0.45f, 0.45f, 1f));
        }

        private void CreateHeaderCol(string name, Transform parent, string text, float width, TextAlignmentOptions align)
        {
            var colObj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            colObj.transform.SetParent(parent, false);

            var rt = colObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, 28f);

            var tmp = colObj.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 14;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.85f, 0.95f, 1f, 1f);
            tmp.alignment = align;
            if (pauseModeTitleText != null) tmp.font = pauseModeTitleText.font;
        }

        private void StyleActionButton(Button btn, Vector2 size, string text, Color accentColor)
        {
            if (btn == null) return;
            var rt = btn.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = size;

            var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.text = text;
                txt.fontSize = 17;
                txt.fontStyle = FontStyles.Bold;
                txt.color = accentColor;
                txt.alignment = TextAlignmentOptions.Center;
            }

            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = new Color(0.08f, 0.12f, 0.22f, 0.95f);
            }

            if (btn.GetComponent<UIJuice>() == null)
            {
                btn.gameObject.AddComponent<UIJuice>();
            }
        }

        private void RefreshLiveScoreboardData()
        {
            if (scoreTableContent == null) return;

            // Clear old rows
            foreach (Transform child in scoreTableContent)
            {
                Destroy(child.gameObject);
            }

            var activeMode = NetworkedGameManager.Instance != null ? NetworkedGameManager.Instance.ActiveGameMode : GameModeBase.Instance;
            if (activeMode != null && pauseSubtitleText != null)
            {
                string modeStr = (activeMode is TDMMode) ? "TEAM DEATHMATCH" : "FREE FOR ALL";
                pauseSubtitleText.text = $"LIVE MATCH OVERVIEW • {modeStr}";
            }

            var playerEntries = new List<ScoreRowData>();
            ulong localClientId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClientsList != null)
            {
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    ulong id = client.ClientId;
                    string pName = PlayerNameRegistry.GetName(id);
                    int kills = (activeMode != null) ? activeMode.GetPlayerKills(id) : 0;
                    int deaths = (activeMode != null) ? activeMode.GetPlayerDeaths(id) : 0;
                    int score = kills * 100 - deaths * 25;
                    if (score < 0) score = 0;
                    int ping = NetworkPingUtility.GetCurrentPing(id);

                    playerEntries.Add(new ScoreRowData
                    {
                        ClientId = id,
                        PlayerName = pName,
                        Kills = kills,
                        Deaths = deaths,
                        Score = score,
                        Ping = ping,
                        IsLocal = (id == localClientId)
                    });
                }
            }

            // Sort descending by kills then score
            playerEntries.Sort((a, b) => b.Kills != a.Kills ? b.Kills.CompareTo(a.Kills) : b.Score.CompareTo(a.Score));

            if (scoreRowPrefab == null)
            {
                scoreRowPrefab = Resources.Load<GameObject>("ScoreboardRow");
                if (scoreRowPrefab == null && ScoreboardController.Instance != null)
                {
                    var f = typeof(ScoreboardController).GetField("rowPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    scoreRowPrefab = f?.GetValue(ScoreboardController.Instance) as GameObject;
                }
            }

            for (int i = 0; i < playerEntries.Count; i++)
            {
                CreateScoreboardRow(i + 1, playerEntries[i]);
            }
        }

        private void CreateScoreboardRow(int rank, ScoreRowData entry)
        {
            if (scoreTableContent == null) return;
            bool isHost = entry.IsLocal && NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;

            if (scoreRowPrefab != null)
            {
                var rowObj = Instantiate(scoreRowPrefab, scoreTableContent);
                var texts = rowObj.GetComponentsInChildren<TextMeshProUGUI>();
                var bgImg = rowObj.GetComponent<Image>();

                if (texts.Length >= 4)
                {
                    texts[0].text = $"#{rank}";
                    texts[1].text = entry.PlayerName;
                    texts[2].text = $"{entry.Kills} K / {entry.Deaths} D";
                    texts[3].text = $"{entry.Score} PTS";
                }

                if (texts.Length >= 5)
                {
                    texts[4].text = NetworkPingUtility.FormatPingString(entry.Ping, isHost);
                    texts[4].color = NetworkPingUtility.GetPingColor(entry.Ping);
                }
                else
                {
                    CreatePingCol(rowObj.transform, entry.Ping, isHost, 95f);
                }

                if (entry.IsLocal && bgImg != null)
                {
                    bgImg.color = new Color(0.18f, 0.72f, 1.0f, 0.35f);
                }
            }
            else
            {
                // Resilient fallback row creation
                var rowObj = new GameObject($"Row_{rank}", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
                rowObj.transform.SetParent(scoreTableContent, false);

                var rRt = rowObj.GetComponent<RectTransform>();
                rRt.sizeDelta = new Vector2(860f, 40f);

                var bgImg = rowObj.GetComponent<Image>();
                bgImg.color = entry.IsLocal ? new Color(0.18f, 0.72f, 1.0f, 0.35f) : new Color(0.06f, 0.09f, 0.18f, 0.8f);

                var hlg = rowObj.GetComponent<HorizontalLayoutGroup>();
                hlg.padding = new RectOffset(20, 20, 4, 4);
                hlg.childAlignment = TextAnchor.MiddleLeft;
                hlg.childControlWidth = false;
                hlg.childControlHeight = true;
                hlg.spacing = 10;

                CreateHeaderCol("Rank", rowObj.transform, $"#{rank}", 65f, TextAlignmentOptions.MidlineLeft);
                CreateHeaderCol("Player", rowObj.transform, entry.PlayerName, 320f, TextAlignmentOptions.MidlineLeft);
                CreateHeaderCol("KD", rowObj.transform, $"{entry.Kills} K / {entry.Deaths} D", 195f, TextAlignmentOptions.Center);
                CreateHeaderCol("Score", rowObj.transform, $"{entry.Score} PTS", 135f, TextAlignmentOptions.MidlineRight);
                CreatePingCol(rowObj.transform, entry.Ping, isHost, 95f);
            }
        }

        private void CreatePingCol(Transform parent, int ping, bool isHost, float width)
        {
            var colObj = new GameObject("Ping", typeof(RectTransform), typeof(TextMeshProUGUI));
            colObj.transform.SetParent(parent, false);

            var rt = colObj.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(width, 28f);

            var tmp = colObj.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = NetworkPingUtility.FormatPingString(ping, isHost);
                tmp.fontSize = 14;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = NetworkPingUtility.GetPingColor(ping);
                tmp.alignment = TextAlignmentOptions.MidlineRight;
                if (pauseModeTitleText != null) tmp.font = pauseModeTitleText.font;
            }
        }

        private struct ScoreRowData
        {
            public ulong ClientId;
            public string PlayerName;
            public int Kills;
            public int Deaths;
            public int Score;
            public int Ping;
            public bool IsLocal;
        }

        private async void OnLeaveMatchClicked()
        {
            TogglePause(false);
            LoadingScreenManager.ShowOverlay("RETURNING TO MAIN MENU...");

            if (SessionManager.Instance != null && SessionManager.Instance.GetCurrentSession() != null)
            {
                try
                {
                    await SessionManager.Instance.LeaveSession();
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[PauseMenuController] Leave session note: {ex.Message}");
                }
            }
            else
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.ShutdownInProgress)
                {
                    try
                    {
                        NetworkManager.Singleton.Shutdown();
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[PauseMenuController] Shutdown note: {ex.Message}");
                    }
                }
                SceneManager.LoadScene("MainMenu");
            }
        }
    }
}
