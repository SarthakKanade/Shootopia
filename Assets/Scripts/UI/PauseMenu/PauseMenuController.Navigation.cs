using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

namespace Shootopia
{
    public partial class PauseMenuController
    {
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

        private void OnDisable()
        {
            GameModeBase.OnPlayerStatsUpdated -= OnPlayerStatsUpdatedHandler;
            if (_isPaused)
            {
                CursorManager.PopUnlock("PauseMenu");
                _isPaused = false;
                IsMenuPaused = false;
            }
        }
    }
}
