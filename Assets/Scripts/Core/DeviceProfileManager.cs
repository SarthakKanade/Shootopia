using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Shootopia
{
    public enum InputControlMode
    {
        Auto = 0,
        DesktopKBM = 1,
        MobileTouch = 2,
        Gamepad = 3
    }

    public enum MobileJoystickMode
    {
        Fixed = 0,
        Dynamic = 1
    }

    /// <summary>
    /// Centralized authority on active input mode and device profile.
    /// Manages automatic platform detection (iOS/Android native, WebGL mobile, Desktop)
    /// and user preference overrides from Settings.
    /// </summary>
    public class DeviceProfileManager : MonoBehaviour
    {
        private const string PrefKeyControlMode = "Shootopia_ControlMode";
        private const string PrefKeyTouchSensitivity = "Shootopia_TouchLookSens";
        private const string PrefKeyTouchSensX = "Shootopia_TouchLookSensX";
        private const string PrefKeyTouchSensY = "Shootopia_TouchLookSensY";
        private const string PrefKeyTouchSensADS = "Shootopia_TouchLookSensADS";
        private const string PrefKeyJoystickMode = "Shootopia_JoystickMode";
        private const string PrefKeyTouchMoveSens = "Shootopia_TouchMoveSens";

        private static DeviceProfileManager _instance;
        private static bool _isQuitting = false;

        public static DeviceProfileManager Instance
        {
            get
            {
                if (_isQuitting) return null;
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<DeviceProfileManager>();
                    if (_instance == null && Application.isPlaying && !_isQuitting)
                    {
                        var go = new GameObject("DeviceProfileManager");
                        _instance = go.AddComponent<DeviceProfileManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        public static event Action<InputControlMode> OnControlModeChanged;
        public static event Action<float> OnTouchSensitivityChanged;
        public static event Action<float, float, float> OnTouchSensitivityAxesChanged;
        public static event Action<MobileJoystickMode> OnJoystickModeChanged;
        public static event Action<float> OnTouchMoveSensChanged;

        [Header("Runtime State")]
        [SerializeField] private InputControlMode _configuredMode = InputControlMode.Auto;
        [SerializeField] private float _touchSensitivity = 1.70f;
        [SerializeField] private float _touchSensitivityX = 1.70f;
        [SerializeField] private float _touchSensitivityY = 1.30f;
        [SerializeField] private float _touchSensitivityADS = 0.65f;
        [SerializeField] private MobileJoystickMode _joystickMode = MobileJoystickMode.Fixed;
        [SerializeField] private float _touchMoveSensitivity = 1.4f;

        private InputControlMode _effectiveMode = InputControlMode.DesktopKBM;

        public static InputControlMode ConfiguredMode
        {
            get => _instance != null ? _instance._configuredMode : InputControlMode.Auto;
            set
            {
                if (Instance != null) Instance.SetConfiguredMode(value);
            }
        }

        public static InputControlMode EffectiveMode => _instance != null ? _instance._effectiveMode : DetectNativeMode();

        public static bool IsTouchActive => EffectiveMode == InputControlMode.MobileTouch;

        /// <summary>
        /// Backward-compatible alias for Horizontal Touch Sensitivity (Yaw).
        /// </summary>
        public static float TouchSensitivity
        {
            get => TouchSensitivityX;
            set => TouchSensitivityX = value;
        }

        public static float TouchSensitivityX
        {
            get => _instance != null ? _instance._touchSensitivityX : 1.70f;
            set
            {
                if (Instance != null) Instance.SetTouchSensitivityX(value);
            }
        }

        public static float TouchSensitivityY
        {
            get => _instance != null ? _instance._touchSensitivityY : 1.30f;
            set
            {
                if (Instance != null) Instance.SetTouchSensitivityY(value);
            }
        }

        public static float TouchSensitivityADS
        {
            get => _instance != null ? _instance._touchSensitivityADS : 0.65f;
            set
            {
                if (Instance != null) Instance.SetTouchSensitivityADS(value);
            }
        }

        public static MobileJoystickMode JoystickMode
        {
            get => _instance != null ? _instance._joystickMode : MobileJoystickMode.Fixed;
            set
            {
                if (Instance != null) Instance.SetJoystickMode(value);
            }
        }

        public static float TouchMoveSensitivity
        {
            get => _instance != null ? _instance._touchMoveSensitivity : 1.4f;
            set
            {
                if (Instance != null) Instance.SetTouchMoveSensitivity(value);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            _isQuitting = false;
            if (_instance == null && Application.isPlaying)
            {
                var go = new GameObject("DeviceProfileManager");
                _instance = go.AddComponent<DeviceProfileManager>();
                DontDestroyOnLoad(go);
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                if (Application.isPlaying) Destroy(gameObject);
                else DestroyImmediate(gameObject);
                return;
            }

            _instance = this;
            _isQuitting = false;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);

            LoadPreferences();
            EvaluateEffectiveMode();
            ApplyPlatformPerformanceSettings();
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

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

        private void EvaluateEffectiveMode()
        {
            InputControlMode previous = _effectiveMode;

            if (_configuredMode == InputControlMode.Auto)
            {
                _effectiveMode = DetectNativeMode();
            }
            else
            {
                _effectiveMode = _configuredMode;
            }

            if (previous != _effectiveMode)
            {
                Debug.Log($"[DeviceProfileManager] Control Mode changed: {previous} -> {_effectiveMode}");
                OnControlModeChanged?.Invoke(_effectiveMode);
            }

            ApplyPlatformPerformanceSettings();
        }

        private void ApplyPlatformPerformanceSettings()
        {
            bool isMobileExperience = Application.isMobilePlatform || _effectiveMode == InputControlMode.MobileTouch;

            if (isMobileExperience)
            {
                // Disable VSync so Application.targetFrameRate takes priority
                QualitySettings.vSyncCount = 0;

                // Query display refresh rate capability (e.g. 90Hz / 120Hz ProMotion screens)
                double refreshRate = Screen.currentResolution.refreshRateRatio.value;
                if (refreshRate >= 115)
                {
                    Application.targetFrameRate = 120;
                }
                else if (refreshRate >= 85)
                {
                    Application.targetFrameRate = 90;
                }
                else
                {
                    Application.targetFrameRate = 60;
                }

                // Strictly lock runtime orientation to Landscape only
                Screen.orientation = ScreenOrientation.AutoRotation;
                Screen.autorotateToPortrait = false;
                Screen.autorotateToPortraitUpsideDown = false;
                Screen.autorotateToLandscapeLeft = true;
                Screen.autorotateToLandscapeRight = true;

                // If native mobile device, auto-switch to Mobile quality level (0) for optimal battery & GPU performance
                if (Application.isMobilePlatform && QualitySettings.GetQualityLevel() != 0)
                {
                    QualitySettings.SetQualityLevel(0, true);
                    Debug.Log("[DeviceProfileManager] Switched Quality Level to Mobile (0)");
                }
            }
            else
            {
                // Desktop: Sync to monitor refresh rate and restore quality level 1 (PC) if at level 0
                QualitySettings.vSyncCount = 1;
                Application.targetFrameRate = -1;

                if (!Application.isMobilePlatform && QualitySettings.GetQualityLevel() == 0)
                {
                    QualitySettings.SetQualityLevel(1, true);
                    Debug.Log("[DeviceProfileManager] Restored Quality Level to PC (1)");
                }
            }
        }

        /// <summary>
        /// Detects platform and device hardware capabilities.
        /// </summary>
        public static InputControlMode DetectNativeMode()
        {
#if UNITY_IOS || UNITY_ANDROID
            return InputControlMode.MobileTouch;
#elif UNITY_WEBGL
            // In WebGL, detect mobile browsers via SystemInfo or Touchscreen presence
            if (SystemInfo.deviceType == DeviceType.Handheld || Application.isMobilePlatform)
            {
                return InputControlMode.MobileTouch;
            }
            if (Touchscreen.current != null && (Mouse.current == null || !Mouse.current.enabled))
            {
                return InputControlMode.MobileTouch;
            }
            return InputControlMode.DesktopKBM;
#else
            // Desktop (Windows/Mac/Linux)
            return InputControlMode.DesktopKBM;
#endif
        }

        private void Update()
        {
            // Dynamic switching in Auto mode if user interacts with touch vs KBM/Gamepad
            if (_configuredMode == InputControlMode.Auto)
            {
                // If a touch was registered on an otherwise desktop/touchscreen device
                if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                {
                    if (_effectiveMode != InputControlMode.MobileTouch)
                    {
                        _effectiveMode = InputControlMode.MobileTouch;
                        OnControlModeChanged?.Invoke(_effectiveMode);
                    }
                }
                // If a keyboard key was pressed
                else if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                {
                    if (_effectiveMode != InputControlMode.DesktopKBM && !Application.isMobilePlatform)
                    {
                        _effectiveMode = InputControlMode.DesktopKBM;
                        OnControlModeChanged?.Invoke(_effectiveMode);
                    }
                }
            }
        }
    }
}
