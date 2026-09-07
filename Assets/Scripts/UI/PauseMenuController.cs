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
    /// Manages cursor locking, audio/sens settings, and graceful match departure.
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

        [Header("Settings Sub-Panel (Space Pack Modal)")]
        [SerializeField] private GameObject settingsSubPanel;
        [SerializeField] private CanvasGroup settingsCanvasGroup;
        [SerializeField] private Button btnCloseSettings;
        [SerializeField] private Slider sliderMasterVol, sliderMusicVol, sliderSFXVol, sliderMouseSens;
        [SerializeField] private TextMeshProUGUI labelMasterVol, labelMusicVol, labelSFXVol, labelMouseSens;
        [SerializeField] private Toggle toggleInvertY, toggleFullscreen;
        [SerializeField] private SpaceGUISwitch switchInvertY, switchFullscreen;
        [SerializeField] private Button btnMinusSens, btnPlusSens;
        [SerializeField] private Button btnActionLeave, btnActionReset, btnActionResume;

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
        [SerializeField] private GameObject tabContentGeneral;
        [SerializeField] private GameObject tabContentTouch;

        private bool _isPaused = false;
        public bool IsPaused => _isPaused;

        private void Awake()
        {
            Instance = this;
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

            if (toggleInvertY != null) toggleInvertY.onValueChanged.AddListener(OnInvertYToggled);
            if (toggleFullscreen != null) toggleFullscreen.onValueChanged.AddListener(OnFullscreenToggled);
            if (switchInvertY != null) switchInvertY.OnValueChanged += OnInvertYToggled;
            if (switchFullscreen != null) switchFullscreen.OnValueChanged += OnFullscreenToggled;

            if (btnMinusSens != null) btnMinusSens.onClick.AddListener(() => AdjustSensitivity(-0.2f));
            if (btnPlusSens != null) btnPlusSens.onClick.AddListener(() => AdjustSensitivity(+0.2f));

            if (btnActionLeave != null) btnActionLeave.onClick.AddListener(OnLeaveMatchClicked);
            if (btnActionReset != null) btnActionReset.onClick.AddListener(ResetSettingsToDefaults);
            if (btnActionResume != null) btnActionResume.onClick.AddListener(() => TogglePause(false));

            if (toggleHaptics != null) toggleHaptics.onValueChanged.AddListener(OnHapticsToggled);
            if (switchHaptics != null) switchHaptics.OnValueChanged += OnHapticsToggled;

            if (btnTabGeneral != null) btnTabGeneral.onClick.AddListener(() => SelectTab(0));
            if (btnTabTouch != null) btnTabTouch.onClick.AddListener(() => SelectTab(1));
        }

        private void InitSettings()
        {
            float master = PlayerPrefs.GetFloat("AudioManager_MasterVolume", 1f);
            float music = PlayerPrefs.GetFloat("AudioManager_MusicVolume", 0.8f);
            float sfx = PlayerPrefs.GetFloat("AudioManager_SFXVolume", 1f);
            float sens = PlayerProfileManager.Instance != null ? PlayerProfileManager.Instance.MouseSensitivity : 1f;
            bool invertY = PlayerPrefs.GetInt("Shootopia_InvertY", 0) == 1;
            bool fullscreen = Screen.fullScreen;

            if (sliderMasterVol != null) { sliderMasterVol.value = master; if (labelMasterVol != null) labelMasterVol.text = $"{Mathf.RoundToInt(master * 100)}%"; }
            if (sliderMusicVol != null) { sliderMusicVol.value = music; if (labelMusicVol != null) labelMusicVol.text = $"{Mathf.RoundToInt(music * 100)}%"; }
            if (sliderSFXVol != null) { sliderSFXVol.value = sfx; if (labelSFXVol != null) labelSFXVol.text = $"{Mathf.RoundToInt(sfx * 100)}%"; }
            if (sliderMouseSens != null) { sliderMouseSens.value = sens; if (labelMouseSens != null) labelMouseSens.text = $"{sens:F1}x"; }

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

        private void ResetSettingsToDefaults()
        {
            if (sliderMasterVol != null) sliderMasterVol.value = 1.0f;
            if (sliderMusicVol != null) sliderMusicVol.value = 0.8f;
            if (sliderSFXVol != null) sliderSFXVol.value = 1.0f;
            OnMouseSensChanged(1.0f);
            if (sliderMouseSens != null) sliderMouseSens.value = 1.0f;
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
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                if (WaitingRoomController.IsLobbyMenuOpen) return;

                if (settingsSubPanel != null && settingsSubPanel.activeSelf)
                {
                    CloseSettingsSubPanel();
                    return;
                }

                TogglePause(!_isPaused);
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
                    UIJuice.PopIn(pauseMenuRoot.GetComponent<RectTransform>(), pauseCanvasGroup);
                    CursorManager.PushUnlock("PauseMenu");
                }
                else
                {
                    UIJuice.PopOut(pauseMenuRoot.GetComponent<RectTransform>(), pauseCanvasGroup);
                    CursorManager.PopUnlock("PauseMenu");
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
                var cg = settingsCanvasGroup != null ? settingsCanvasGroup : settingsSubPanel.GetComponent<CanvasGroup>();
                UIJuice.PopIn(settingsSubPanel.GetComponent<RectTransform>(), cg);
                SelectTab(DeviceProfileManager.IsTouchActive ? 1 : 0);
            }
        }

        public void SelectTab(int tabIndex)
        {
            if (tabContentGeneral != null) tabContentGeneral.SetActive(tabIndex == 0);
            if (tabContentTouch != null) tabContentTouch.SetActive(tabIndex == 1);

            if (btnTabGeneral != null)
            {
                var img = btnTabGeneral.GetComponent<Image>();
                if (img != null) img.color = tabIndex == 0 ? Color.white : new Color(0.65f, 0.65f, 0.75f, 0.75f);
            }
            if (btnTabTouch != null)
            {
                var img = btnTabTouch.GetComponent<Image>();
                if (img != null) img.color = tabIndex == 1 ? Color.white : new Color(0.65f, 0.65f, 0.75f, 0.75f);
            }
        }

        public void CloseSettingsSubPanel()
        {
            if (settingsSubPanel != null)
            {
                var cg = settingsCanvasGroup != null ? settingsCanvasGroup : settingsSubPanel.GetComponent<CanvasGroup>();
                UIJuice.PopOut(settingsSubPanel.GetComponent<RectTransform>(), cg);
                settingsSubPanel.SetActive(false);
            }
            if (_isPaused && pauseMenuRoot != null)
            {
                pauseMenuRoot.SetActive(true);
                UIJuice.PopIn(pauseMenuRoot.GetComponent<RectTransform>(), pauseCanvasGroup);
            }
        }

        private async void OnLeaveMatchClicked()
        {
            TogglePause(false);
            LoadingScreenManager.ShowOverlay("RETURNING TO MAIN MENU...");

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
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

            if (SessionManager.Instance != null)
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

            SceneManager.LoadScene("MainMenu");
        }
    }
}
