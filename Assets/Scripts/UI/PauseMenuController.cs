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
