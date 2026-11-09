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
    public partial class DeviceProfileManager : MonoBehaviour
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

        private void OnApplicationFocus(bool hasFocus)
        {
#if !UNITY_WEBGL
            if (!hasFocus)
            {
                // Throttle background frame rate to 30 FPS to minimize host CPU/GPU load while tabbed out
                Application.targetFrameRate = 30;
            }
            else
            {
                ApplyPlatformPerformanceSettings();
            }
#else
            if (hasFocus)
            {
                ApplyPlatformPerformanceSettings();
            }
#endif
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

    }
}
