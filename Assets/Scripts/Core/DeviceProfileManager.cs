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
        private const string PrefKeyJoystickMode = "Shootopia_JoystickMode";
        private const string PrefKeyTouchMoveSens = "Shootopia_TouchMoveSens";

        public static DeviceProfileManager Instance { get; private set; }

        public static event Action<InputControlMode> OnControlModeChanged;
        public static event Action<float> OnTouchSensitivityChanged;
        public static event Action<MobileJoystickMode> OnJoystickModeChanged;
        public static event Action<float> OnTouchMoveSensChanged;

        [Header("Runtime State")]
        [SerializeField] private InputControlMode _configuredMode = InputControlMode.Auto;
        [SerializeField] private float _touchSensitivity = 2.5f;
        [SerializeField] private MobileJoystickMode _joystickMode = MobileJoystickMode.Fixed;
        [SerializeField] private float _touchMoveSensitivity = 1.4f;

        private InputControlMode _effectiveMode = InputControlMode.DesktopKBM;

        public static InputControlMode ConfiguredMode
        {
            get => Instance != null ? Instance._configuredMode : InputControlMode.Auto;
            set
            {
                if (Instance != null) Instance.SetConfiguredMode(value);
            }
        }

        public static InputControlMode EffectiveMode => Instance != null ? Instance._effectiveMode : DetectNativeMode();

        public static bool IsTouchActive => EffectiveMode == InputControlMode.MobileTouch;

        public static float TouchSensitivity
        {
            get
            {
                if (Instance != null)
                {
                    if (Instance._touchSensitivity <= 0.5f)
                    {
                        Instance._touchSensitivity = 2.5f;
                        PlayerPrefs.SetFloat(PrefKeyTouchSensitivity, 2.5f);
                    }
                    return Instance._touchSensitivity;
                }
                return 2.5f;
            }
            set
            {
                if (Instance != null) Instance.SetTouchSensitivity(value);
            }
        }

        public static MobileJoystickMode JoystickMode
        {
            get => Instance != null ? Instance._joystickMode : MobileJoystickMode.Fixed;
            set
            {
                if (Instance != null) Instance.SetJoystickMode(value);
            }
        }

        public static float TouchMoveSensitivity
        {
            get => Instance != null ? Instance._touchMoveSensitivity : 1.4f;
            set
            {
                if (Instance != null) Instance.SetTouchMoveSensitivity(value);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (Instance == null)
            {
                var go = new GameObject("DeviceProfileManager");
                Instance = go.AddComponent<DeviceProfileManager>();
                DontDestroyOnLoad(go);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadPreferences();
            EvaluateEffectiveMode();
        }

        private void LoadPreferences()
        {
            _configuredMode = (InputControlMode)PlayerPrefs.GetInt(PrefKeyControlMode, (int)InputControlMode.Auto);
            float savedSens = PlayerPrefs.GetFloat(PrefKeyTouchSensitivity, 2.5f);
            if (savedSens <= 0.5f) savedSens = 2.5f; // Automatically upgrade legacy low default
            _touchSensitivity = savedSens;
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
            _touchSensitivity = Mathf.Clamp(sensitivity, 0.2f, 6.0f);
            PlayerPrefs.SetFloat(PrefKeyTouchSensitivity, _touchSensitivity);
            PlayerPrefs.Save();

            OnTouchSensitivityChanged?.Invoke(_touchSensitivity);
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
