using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Shootopia
{
    public partial class MainMenuController
    {
private void InitSettingsValues()
        {
            _selectedSkin = PlayerProfileManager.Instance != null 
                ? PlayerProfileManager.Instance.SelectedModelIndex 
                : PlayerPrefs.GetInt("Shootopia_SelectedModelIndex", 0);

            if (btnSkinAgent != null)
            {
                var txt = btnSkinAgent.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = "VANGUARD";
            }
            if (btnSkinRobot != null)
            {
                var txt = btnSkinRobot.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = "VALKYRIE";
            }
            if (AvatarStageController.Instance != null)
            {
                AvatarStageController.Instance.SetSkin(_selectedSkin);
            }

            // Clean up any accidental leave buttons from modalSettings in MainMenu
            if (modalSettings != null)
            {
                Transform leaveTop = modalSettings.transform.Find("BtnActionLeaveTopLeft");
                if (leaveTop != null) leaveTop.gameObject.SetActive(false);
                Transform oldLeaveCorner = modalSettings.transform.Find("BtnActionLeaveCorner");
                if (oldLeaveCorner != null) Destroy(oldLeaveCorner.gameObject);

                Transform bottomContainer = modalSettings.transform.Find("Bottom_UI_Container");
                if (bottomContainer != null)
                {
                    Transform oldBottomLeave = bottomContainer.Find("BtnActionLeaveCorner") ?? bottomContainer.Find("BtnActionLeaveTopLeft");
                    if (oldBottomLeave != null) Destroy(oldBottomLeave.gameObject);

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
                        btnActionApply = btn3.GetComponent<Button>();
                        var txt = btn3.GetComponentInChildren<TextMeshProUGUI>();
                        if (txt != null) txt.text = "APPLY";
                        if (btnActionApply != null)
                        {
                            btnActionApply.onClick.RemoveAllListeners();
                            btnActionApply.onClick.AddListener(CloseTopModal);
                        }
                    }
                }
            }

            if (modalSettings != null)
            {
                if (sliderMasterVol == null) sliderMasterVol = modalSettings.transform.Find("TabContent_General/Slider_Container/Sound_Slider/Slider")?.GetComponent<Slider>();
                if (labelMasterVol == null) labelMasterVol = modalSettings.transform.Find("TabContent_General/Slider_Container/Sound_Slider/Val_TXT")?.GetComponent<TextMeshProUGUI>();
                if (sliderFOV == null) sliderFOV = modalSettings.transform.Find("TabContent_General/Slider_Container/Row_FOV/Slider")?.GetComponent<Slider>();
                if (labelFOV == null) labelFOV = modalSettings.transform.Find("TabContent_General/Slider_Container/Row_FOV/Val_TXT")?.GetComponent<TextMeshProUGUI>();
            }

            float master = PlayerPrefs.GetFloat("AudioManager_MasterVolume", 1.0f);
            float music = PlayerPrefs.GetFloat("AudioManager_MusicVolume", 0.65f);
            float sfx = PlayerPrefs.GetFloat("AudioManager_SFXVolume", 1.0f);
            float sens = PlayerProfileManager.Instance != null ? PlayerProfileManager.Instance.MouseSensitivity : 1.0f;
            float fov = PlayerPrefs.GetFloat(ThirdPersonCameraController.PREFS_KEY_FOV, ThirdPersonCameraController.DEFAULT_FOV);
            if (Mathf.Approximately(fov, 80f) && !PlayerPrefs.HasKey("Shootopia_UserSetFOV"))
            {
                fov = ThirdPersonCameraController.DEFAULT_FOV;
            }
            bool invertY = (PlayerProfileManager.Instance != null && PlayerProfileManager.Instance.InvertY)
                        || (PlayerPrefs.GetInt("Shootopia_InvertY", 0) == 1);
            bool fullscreen = Screen.fullScreen;

            if (sliderMasterVol != null) { sliderMasterVol.value = master; OnMasterVolChanged(master); }
            if (sliderMusicVol != null && sliderMusicVol != sliderFOV) { sliderMusicVol.value = music; OnMusicVolChanged(music); }
            if (sliderSFXVol != null) { sliderSFXVol.value = sfx; OnSFXVolChanged(sfx); }
            if (sliderMouseSens != null) { sliderMouseSens.value = sens; OnMouseSensChanged(sens); }
            if (labelMouseSens != null) labelMouseSens.text = $"{sens:0.0}x";

            if (sliderFOV != null)
            {
                sliderFOV.minValue = ThirdPersonCameraController.MIN_FOV;
                sliderFOV.maxValue = ThirdPersonCameraController.MAX_FOV;
                sliderFOV.wholeNumbers = true;
                sliderFOV.value = fov;
                sliderFOV.onValueChanged.RemoveListener(OnFOVChanged);
                sliderFOV.onValueChanged.AddListener(OnFOVChanged);
                if (labelFOV != null) labelFOV.text = $"{Mathf.RoundToInt(fov)}\u00B0";
            }

            if (toggleInvertY != null) toggleInvertY.isOn = invertY;
            if (toggleFullscreen != null) toggleFullscreen.isOn = fullscreen;
            if (switchInvertY != null) switchInvertY.SetState(invertY, false);
            if (switchFullscreen != null) switchFullscreen.SetState(fullscreen, false);
        }

        private void OnMasterVolChanged(float val)
        {
            if (labelMasterVol != null) labelMasterVol.text = $"{Mathf.RoundToInt(val * 100)}%";
            if (AudioManager.Instance != null) AudioManager.Instance.SetChannelVolume(AudioChannel.Master, val);
        }

        private void OnMusicVolChanged(float val)
        {
            if (labelMusicVol != null) labelMusicVol.text = $"{Mathf.RoundToInt(val * 100)}%";
            if (AudioManager.Instance != null) AudioManager.Instance.SetChannelVolume(AudioChannel.Music, val);
        }

        private void OnSFXVolChanged(float val)
        {
            if (labelSFXVol != null) labelSFXVol.text = $"{Mathf.RoundToInt(val * 100)}%";
            if (AudioManager.Instance != null) AudioManager.Instance.SetChannelVolume(AudioChannel.SFX, val);
        }

        private void OnMouseSensChanged(float val)
        {
            if (PlayerProfileManager.Instance != null)
            {
                PlayerProfileManager.Instance.SetMouseSensitivity(val);
            }
            if (labelMouseSens != null) labelMouseSens.text = $"{val:0.0}x";
        }

        private void OnFOVChanged(float val)
        {
            if (labelFOV != null) labelFOV.text = $"{Mathf.RoundToInt(val)}\u00B0";
            PlayerPrefs.SetFloat(ThirdPersonCameraController.PREFS_KEY_FOV, val);
            PlayerPrefs.SetInt("Shootopia_UserSetFOV", 1);
            PlayerPrefs.Save();
            if (ThirdPersonCameraController.Instance != null)
            {
                ThirdPersonCameraController.Instance.SetUserFOV(val);
            }
        }

        private void AdjustSensitivity(float delta)
        {
            float current = PlayerProfileManager.Instance != null ? PlayerProfileManager.Instance.MouseSensitivity : 1.0f;
            float newSens = Mathf.Clamp((float)Math.Round(current + delta, 1), 0.2f, 3.0f);
            OnMouseSensChanged(newSens);
            if (sliderMouseSens != null) sliderMouseSens.value = newSens;
        }

        private void ResetSettingsToDefaults()
        {
            if (sliderMasterVol != null) sliderMasterVol.value = 1.0f;
            if (sliderMusicVol != null) sliderMusicVol.value = 0.65f;
            if (sliderSFXVol != null) sliderSFXVol.value = 1.0f;
            OnMouseSensChanged(1.0f);
            if (sliderMouseSens != null) sliderMouseSens.value = 1.0f;
            if (sliderFOV != null) sliderFOV.value = ThirdPersonCameraController.DEFAULT_FOV;
            OnFOVChanged(ThirdPersonCameraController.DEFAULT_FOV);
            if (switchInvertY != null) switchInvertY.SetState(false, true);
            if (switchFullscreen != null) switchFullscreen.SetState(true, true);
        }

        private void OnInvertYChanged(bool val)
        {
            if (PlayerProfileManager.Instance != null) PlayerProfileManager.Instance.SetInvertY(val);
            PlayerPrefs.SetInt("Shootopia_InvertY", val ? 1 : 0);
            PlayerPrefs.Save();
            if (toggleInvertY != null) toggleInvertY.isOn = val;
            if (switchInvertY != null && switchInvertY.IsOn != val) switchInvertY.SetState(val, false);
        }

        private void OnFullscreenChanged(bool val)
        {
            Screen.fullScreen = val;
            if (toggleFullscreen != null) toggleFullscreen.isOn = val;
            if (switchFullscreen != null && switchFullscreen.IsOn != val) switchFullscreen.SetState(val, false);
        }

        // ──────────────────────── MODAL STACK & UI HELPERS ────────────────────────
    }
}
