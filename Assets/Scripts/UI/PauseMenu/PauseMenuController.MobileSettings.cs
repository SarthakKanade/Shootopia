using UnityEngine;

namespace Shootopia
{
    public partial class PauseMenuController
    {
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
    }
}
