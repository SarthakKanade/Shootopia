using System;
using UnityEngine;
using UnityEngine.Events;
using Unity.Services.Core;
using Unity.Services.Authentication;

namespace Shootopia
{
    /// <summary>
    /// Initializes Unity Gaming Services and performs anonymous authentication on startup.
    /// Exposes an OnAuthComplete event that other systems can subscribe to before initiating session/lobby actions.
    /// </summary>
    public class MultiplayerBootstrapper : MonoBehaviour
    {
        [Header("Scene Flow")]
        [SerializeField] private string nextSceneName = "MainMenu";
        [SerializeField] private bool autoLoadNextScene = true;

        [Header("Events")]
        public UnityEvent OnAuthComplete;

        private async void Awake()
        {
            try
            {
                Debug.Log("[Bootstrapper] Initializing Unity Services...");
                await UnityServices.InitializeAsync();

                if (AuthenticationService.Instance.IsSignedIn)
                {
                    Debug.Log($"[Bootstrapper] Already signed in as: {AuthenticationService.Instance.PlayerId}");
                    HandleAuthSuccess();
                    return;
                }

                Debug.Log("[Bootstrapper] Signing in anonymously...");
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"[Bootstrapper] Signed in as: {AuthenticationService.Instance.PlayerId}");
                HandleAuthSuccess();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Bootstrapper] Authentication failed: {ex.Message}");
            }
        }

        private void HandleAuthSuccess()
        {
            OnAuthComplete?.Invoke();

            if (autoLoadNextScene && !string.IsNullOrEmpty(nextSceneName))
            {
                Debug.Log($"[Bootstrapper] Auth complete. Warping to {nextSceneName}...");
                LoadingScreenManager.LoadScene(nextSceneName, "AUTHENTICATION VERIFIED");
            }
        }
    }
}

/*
VERIFICATION:
- Attach to a Bootstrap GameObject in the first scene.
- Enter Play mode — console logs a Player ID string.
- Exit and re-enter Play mode — same Player ID appears again.
*/
