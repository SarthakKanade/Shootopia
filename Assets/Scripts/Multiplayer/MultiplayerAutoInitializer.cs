using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Unity.Services.Core;
using Unity.Services.Authentication;

namespace Shootopia
{
    /// <summary>
    /// Automatically initializes Unity Gaming Services and authenticates anonymously on scene load.
    /// Allows any scene (especially MainMenu.unity) to function independently without requiring Bootstrap.unity.
    /// </summary>
    public class MultiplayerAutoInitializer : MonoBehaviour
    {
        public static MultiplayerAutoInitializer Instance { get; private set; }

        public static bool IsInitialized => UnityServices.State == ServicesInitializationState.Initialized;
        public static bool IsAuthenticated
        {
            get
            {
                try
                {
                    return IsInitialized && AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn;
                }
                catch
                {
                    return false;
                }
            }
        }

        [Header("Events")]
        public UnityEvent OnAuthSuccess;
        public UnityEvent<string> OnAuthError;

        private static Task<bool> _initTask;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private async void Start()
        {
            await EnsureInitializedAsync();
        }

        /// <summary>
        /// Guarantees that UGS and Anonymous Auth are ready. Safe to call multiple times from anywhere.
        /// </summary>
        public static async Task<bool> EnsureInitializedAsync()
        {
            if (IsAuthenticated) return true;

            if (_initTask != null)
            {
                return await _initTask;
            }

            _initTask = InitializeInternal();
            bool result = await _initTask;
            _initTask = null;
            return result;
        }

        private static async Task<bool> InitializeInternal()
        {
            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                {
                    Debug.Log("[MultiplayerAutoInitializer] Initializing Unity Services...");
                    await UnityServices.InitializeAsync();
                }

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    Debug.Log("[MultiplayerAutoInitializer] Signing in anonymously...");
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    Debug.Log($"[MultiplayerAutoInitializer] Signed in successfully. Player ID: {AuthenticationService.Instance.PlayerId}");
                }

                Instance?.OnAuthSuccess?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MultiplayerAutoInitializer] Initialization/Auth failed: {ex.Message}");
                Instance?.OnAuthError?.Invoke(ex.Message);
                return false;
            }
        }
    }
}
