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
                    return IsInitialized && AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn && !AuthenticationService.Instance.IsExpired;
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
        private static bool _hasSubscribedToEvents = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
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
            try
            {
                bool result = await _initTask;
                return result;
            }
            finally
            {
                _initTask = null;
            }
        }

        private static async Task<bool> InitializeInternal()
        {
            const int maxRetries = 3;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    if (UnityServices.State == ServicesInitializationState.Uninitialized)
                    {
                        Debug.Log($"[MultiplayerAutoInitializer] Initializing Unity Services (Attempt {attempt}/{maxRetries})...");
                        await UnityServices.InitializeAsync();
                    }
                    else if (UnityServices.State == ServicesInitializationState.Initializing)
                    {
                        int waited = 0;
                        while (UnityServices.State == ServicesInitializationState.Initializing && waited < 4000)
                        {
                            await Task.Delay(100);
                            waited += 100;
                        }
                    }

                    if (AuthenticationService.Instance != null)
                    {
                        if (!_hasSubscribedToEvents)
                        {
                            _hasSubscribedToEvents = true;
                            AuthenticationService.Instance.Expired += OnSessionExpired;
                        }

                        if (!AuthenticationService.Instance.IsSignedIn || AuthenticationService.Instance.IsExpired)
                        {
                            Debug.Log($"[MultiplayerAutoInitializer] Signing in anonymously (Attempt {attempt}/{maxRetries})...");
                            try
                            {
                                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                            }
                            catch (Exception)
                            {
                                Debug.LogWarning("[MultiplayerAutoInitializer] Anonymous sign-in failed with existing token. Clearing token and retrying...");
                                AuthenticationService.Instance.ClearSessionToken();
                                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                            }
                            Debug.Log($"[MultiplayerAutoInitializer] Signed in successfully. Player ID: {AuthenticationService.Instance.PlayerId}");
                        }
                    }

                    Instance?.OnAuthSuccess?.Invoke();
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[MultiplayerAutoInitializer] Init/Auth attempt {attempt}/{maxRetries} failed: {ex.Message}");
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(1000 * attempt);
                    }
                    else
                    {
                        Debug.LogError($"[MultiplayerAutoInitializer] Initialization/Auth failed after {maxRetries} attempts: {ex.Message}");
                        Instance?.OnAuthError?.Invoke(ex.Message);
                        return false;
                    }
                }
            }

            return false;
        }

        private static async void OnSessionExpired()
        {
            Debug.LogWarning("[MultiplayerAutoInitializer] Authentication session expired. Refreshing token...");
            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("[MultiplayerAutoInitializer] Re-authenticated successfully after expiry.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MultiplayerAutoInitializer] Re-auth with existing token failed ({ex.Message}). Clearing token and re-authenticating...");
                try
                {
                    AuthenticationService.Instance.ClearSessionToken();
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    Debug.Log("[MultiplayerAutoInitializer] Fresh anonymous sign-in succeeded after expiry.");
                }
                catch (Exception innerEx)
                {
                    Debug.LogError($"[MultiplayerAutoInitializer] Failed to recover expired session: {innerEx.Message}");
                }
            }
        }
    }
}
