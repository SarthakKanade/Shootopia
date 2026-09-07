using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// Centralized cross-platform mobile haptics and tactile feedback manager.
    /// Drives multi-tier vibration effects on Android (API 26+ VibrationEffect)
    /// and iOS with zero desktop/editor overhead and motor rate throttling.
    /// </summary>
    public static class MobileHapticsManager
    {
        private const string PrefKeyHaptics = "Shootopia_HapticsEnabled";
        private static bool _hapticsEnabled = true;
        private static bool _isLoaded = false;
        private static float _lastShotHapticTime = -1f;

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject _vibrator;
        private static AndroidJavaClass _vibrationEffectClass;
        private static bool _androidInitialized = false;

        private static void InitializeAndroid()
        {
            if (_androidInitialized) return;
            _androidInitialized = true;

            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    _vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
                _vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MobileHapticsManager] Android JNI init error: {ex.Message}");
            }
        }
#endif

        public static bool IsHapticsEnabled
        {
            get
            {
                if (!_isLoaded)
                {
                    _hapticsEnabled = PlayerPrefs.GetInt(PrefKeyHaptics, 1) == 1;
                    _isLoaded = true;
                }
                return _hapticsEnabled;
            }
            set
            {
                _hapticsEnabled = value;
                _isLoaded = true;
                PlayerPrefs.SetInt(PrefKeyHaptics, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static void SetHapticsEnabled(bool enabled) => IsHapticsEnabled = enabled;

        /// <summary>
        /// Crisp light tap (15ms) for UI button presses, tabs, or jumps.
        /// </summary>
        public static void TriggerLightTap()
        {
            Vibrate(15, 80);
        }

        /// <summary>
        /// Medium tactile pulse (30ms) for weapon firing, rate-throttled for rapid automatics.
        /// </summary>
        public static void TriggerShotFired()
        {
            float now = Time.unscaledTime;
            if (now - _lastShotHapticTime < 0.075f) return; // Prevent motor stalling
            _lastShotHapticTime = now;

            Vibrate(30, 160);
        }

        /// <summary>
        /// High-amplitude pulse (50ms) when taking enemy damage.
        /// </summary>
        public static void TriggerDamageTaken()
        {
            Vibrate(50, 200);
        }

        /// <summary>
        /// Heavy rumble (120ms) for nearby explosions, grenades, or player death.
        /// </summary>
        public static void TriggerExplosion()
        {
            Vibrate(120, 255);
        }

        private static void Vibrate(long durationMs, int amplitude)
        {
            if (!IsHapticsEnabled) return;

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                InitializeAndroid();
                if (_vibrator != null && _vibrator.Call<bool>("hasVibrator"))
                {
                    if (_vibrationEffectClass != null)
                    {
                        using (var effect = _vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", durationMs, amplitude))
                        {
                            _vibrator.Call("vibrate", effect);
                            return;
                        }
                    }
                    _vibrator.Call("vibrate", durationMs);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MobileHapticsManager] Vibrate execution error: {ex.Message}");
            }
#elif UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}
