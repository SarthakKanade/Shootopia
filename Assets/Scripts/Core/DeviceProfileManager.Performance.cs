using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Shootopia
{
    public partial class DeviceProfileManager
    {
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

        private float _autoModeCheckTimer = 0f;

        private void ApplyPlatformPerformanceSettings()
        {
            bool isWebGL = Application.platform == RuntimePlatform.WebGLPlayer;
            bool isMobileExperience = Application.isMobilePlatform || _effectiveMode == InputControlMode.MobileTouch;

            if (isWebGL || isMobileExperience)
            {
                // Lower device load and thermal throttling by capping target frame rate to a stable 60 FPS
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 60;

                // Potato & Mobile Thermal Optimization Profile:
                QualitySettings.skinWeights = SkinWeights.TwoBones;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
                QualitySettings.lodBias = 1.0f;
                QualitySettings.realtimeGICPUUsage = 25;
                QualitySettings.particleRaycastBudget = 64;

                // Ensure Potato / Mobile quality level (0) with Mobile_RPAsset is active
                if (QualitySettings.GetQualityLevel() != 0)
                {
                    QualitySettings.SetQualityLevel(0, true);
                    Debug.Log("[DeviceProfileManager] Activated Potato/Mobile Quality (Level 0)");
                }
            }
            else
            {
                // Desktop / Editor: Hardware VSync + stable 60 FPS cap to prevent thermal overdrive
                QualitySettings.vSyncCount = 1;
                Application.targetFrameRate = 60;
            }

            if (isMobileExperience)
            {
                Screen.orientation = ScreenOrientation.AutoRotation;
                Screen.autorotateToPortrait = false;
                Screen.autorotateToPortraitUpsideDown = false;
                Screen.autorotateToLandscapeLeft = true;
                Screen.autorotateToLandscapeRight = true;
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
            return InputControlMode.DesktopKBM;
#endif
        }

        private void Update()
        {
            if (_configuredMode != InputControlMode.Auto) return;

            // Throttled dynamic check (4 Hz) instead of iterating every keyboard key every frame
            _autoModeCheckTimer += Time.unscaledDeltaTime;
            if (_autoModeCheckTimer < 0.25f) return;
            _autoModeCheckTimer = 0f;

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                if (_effectiveMode != InputControlMode.MobileTouch)
                {
                    _effectiveMode = InputControlMode.MobileTouch;
                    OnControlModeChanged?.Invoke(_effectiveMode);
                }
            }
            else if (Keyboard.current != null && Keyboard.current.anyKey.isPressed && !Application.isMobilePlatform)
            {
                if (_effectiveMode != InputControlMode.DesktopKBM)
                {
                    _effectiveMode = InputControlMode.DesktopKBM;
                    OnControlModeChanged?.Invoke(_effectiveMode);
                }
            }
        }
    }
}
