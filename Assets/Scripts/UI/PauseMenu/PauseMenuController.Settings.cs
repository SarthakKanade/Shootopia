using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Shootopia
{
    public partial class PauseMenuController
    {
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
    }
}

