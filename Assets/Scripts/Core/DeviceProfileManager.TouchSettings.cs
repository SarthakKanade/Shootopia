using UnityEngine;

namespace Shootopia
{
    public partial class DeviceProfileManager
    {
private void LoadPreferences()
        {
            _configuredMode = (InputControlMode)PlayerPrefs.GetInt(PrefKeyControlMode, (int)InputControlMode.Auto);
            
            if (PlayerPrefs.HasKey(PrefKeyTouchSensX))
            {
                _touchSensitivityX = PlayerPrefs.GetFloat(PrefKeyTouchSensX, 1.70f);
                _touchSensitivityY = PlayerPrefs.GetFloat(PrefKeyTouchSensY, 1.30f);
                _touchSensitivityADS = PlayerPrefs.GetFloat(PrefKeyTouchSensADS, 0.65f);
            }
            else if (PlayerPrefs.HasKey(PrefKeyTouchSensitivity))
            {
                // Seamless legacy migration
                float legacy = PlayerPrefs.GetFloat(PrefKeyTouchSensitivity, 1.70f);
                if (legacy <= 0.5f || Mathf.Approximately(legacy, 2.5f)) legacy = 1.70f;
                _touchSensitivityX = legacy;
                _touchSensitivityY = Mathf.Clamp(legacy * 0.75f, 0.5f, 4.0f);
                _touchSensitivityADS = 0.65f;

                PlayerPrefs.SetFloat(PrefKeyTouchSensX, _touchSensitivityX);
                PlayerPrefs.SetFloat(PrefKeyTouchSensY, _touchSensitivityY);
                PlayerPrefs.SetFloat(PrefKeyTouchSensADS, _touchSensitivityADS);
                PlayerPrefs.Save();
            }
            else
            {
                _touchSensitivityX = 1.70f;
                _touchSensitivityY = 1.30f;
                _touchSensitivityADS = 0.65f;
            }

            _touchSensitivity = _touchSensitivityX;
            _joystickMode = (MobileJoystickMode)PlayerPrefs.GetInt(PrefKeyJoystickMode, (int)MobileJoystickMode.Fixed);
            _touchMoveSensitivity = PlayerPrefs.GetFloat(PrefKeyTouchMoveSens, 1.4f);
        }

        public void SetConfiguredMode(InputControlMode mode)
        {
            if (_configuredMode == mode) return;

            _configuredMode = mode;
            PlayerPrefs.SetInt(PrefKeyControlMode, (int)mode);
            PlayerPrefs.Save();

            EvaluateEffectiveMode();
        }

        public void SetTouchSensitivity(float sensitivity)
        {
            SetTouchSensitivityX(sensitivity);
        }

        public void SetTouchSensitivityX(float sensitivity)
        {
            _touchSensitivityX = Mathf.Clamp(sensitivity, 0.2f, 5.0f);
            _touchSensitivity = _touchSensitivityX;
            PlayerPrefs.SetFloat(PrefKeyTouchSensX, _touchSensitivityX);
            PlayerPrefs.SetFloat(PrefKeyTouchSensitivity, _touchSensitivityX);
            PlayerPrefs.Save();

            OnTouchSensitivityChanged?.Invoke(_touchSensitivityX);
            OnTouchSensitivityAxesChanged?.Invoke(_touchSensitivityX, _touchSensitivityY, _touchSensitivityADS);
        }

        public void SetTouchSensitivityY(float sensitivity)
        {
            _touchSensitivityY = Mathf.Clamp(sensitivity, 0.2f, 5.0f);
            PlayerPrefs.SetFloat(PrefKeyTouchSensY, _touchSensitivityY);
            PlayerPrefs.Save();

            OnTouchSensitivityAxesChanged?.Invoke(_touchSensitivityX, _touchSensitivityY, _touchSensitivityADS);
        }

        public void SetTouchSensitivityADS(float sensitivity)
        {
            _touchSensitivityADS = Mathf.Clamp(sensitivity, 0.1f, 2.0f);
            PlayerPrefs.SetFloat(PrefKeyTouchSensADS, _touchSensitivityADS);
            PlayerPrefs.Save();

            OnTouchSensitivityAxesChanged?.Invoke(_touchSensitivityX, _touchSensitivityY, _touchSensitivityADS);
        }

        public void ResetTouchSettingsToDefaults()
        {
            SetTouchSensitivityX(1.70f);
            SetTouchSensitivityY(1.30f);
            SetTouchSensitivityADS(0.65f);
            SetTouchMoveSensitivity(1.40f);
            SetJoystickMode(MobileJoystickMode.Fixed);
        }

        public void SetJoystickMode(MobileJoystickMode mode)
        {
            if (_joystickMode == mode) return;

            _joystickMode = mode;
            PlayerPrefs.SetInt(PrefKeyJoystickMode, (int)mode);
            PlayerPrefs.Save();

            OnJoystickModeChanged?.Invoke(_joystickMode);
        }

        public void SetTouchMoveSensitivity(float sensitivity)
        {
            _touchMoveSensitivity = Mathf.Clamp(sensitivity, 0.5f, 3.0f);
            PlayerPrefs.SetFloat(PrefKeyTouchMoveSens, _touchMoveSensitivity);
            PlayerPrefs.Save();

            OnTouchMoveSensChanged?.Invoke(_touchMoveSensitivity);
        }
    }
}
