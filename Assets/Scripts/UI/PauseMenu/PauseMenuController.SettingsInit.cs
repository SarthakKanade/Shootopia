using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Shootopia
{
    public partial class PauseMenuController
    {
private void InitSettings()
        {
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
    }
}
