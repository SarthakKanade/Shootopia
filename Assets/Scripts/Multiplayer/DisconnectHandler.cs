using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

namespace Shootopia
{
    /// <summary>
    /// Handles client and host disconnection events robustly.
    /// Manages cleanup of disconnected player objects, updates nameplate & score registries,
    /// logs disconnect system messages, and gracefully handles host shutdown for remaining clients.
    /// </summary>
    public class DisconnectHandler : MonoBehaviour
    {
        public static DisconnectHandler Instance { get; private set; }

        private bool _isHandlingHostDisconnect = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            RegisterCallbacks();
        }

        private void OnEnable()
        {
            RegisterCallbacks();
        }

        private void RegisterCallbacks()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            }
        }

        private void OnDisable()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            // Log required exact log string
            Debug.Log("[DisconnectHandler] Client disconnected: " + clientId);

            if (NetworkManager.Singleton == null) return;

            ulong localClientId = NetworkManager.Singleton.LocalClientId;
            bool isHost = NetworkManager.Singleton.IsHost;
            bool isServer = NetworkManager.Singleton.IsServer;

            // Scenario A: Host disconnect / Local client disconnect / Host shut down session
            if ((!isHost && clientId == localClientId) || (!isHost && clientId == 0))
            {
                HandleHostLeft();
                return;
            }

            // Scenario B: Remote client disconnected from server/host during match
            if (isServer && clientId != localClientId)
            {
                HandleRemoteClientDisconnected(clientId);
            }
        }

        private void HandleRemoteClientDisconnected(ulong clientId)
        {
            // 1. Resolve display name before unregistering
            string playerName = PlayerNameRegistry.GetName(clientId);
            if (string.IsNullOrEmpty(playerName) || playerName == "Player " + clientId)
            {
                playerName = $"Player {clientId}";
            }

            // 2. Log system message to kill feed
            string systemMsg = $"{playerName} disconnected";
            if (GameModeBase.Instance != null)
            {
                GameModeBase.Instance.BroadcastSystemMessageClientRpc(systemMsg);
            }
            else
            {
                Debug.Log($"[DisconnectHandler] System message: {systemMsg}");
            }

            // 3. Unregister nameplate / player name from registry
            PlayerNameRegistry.Unregister(clientId);

            // 4. Clean up network player object safely if still active
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                if (client.PlayerObject != null && client.PlayerObject.IsSpawned)
                {
                    try
                    {
                        Debug.Log($"[DisconnectHandler] Cleaning up NetworkObject for disconnected client {clientId}");
                        client.PlayerObject.Despawn(true);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[DisconnectHandler] Exception during NetworkObject cleanup for client {clientId}: {ex.Message}");
                    }
                }
            }
        }

        private void HandleHostLeft()
        {
            if (_isHandlingHostDisconnect) return;
            _isHandlingHostDisconnect = true;

            Debug.LogWarning("[DisconnectHandler] Host left or local client disconnected. Returning to Main Menu gracefully.");

            // 1. Show clear UI notification toast
            NetworkNotificationUI.ShowToast("Host has left the match", duration: 5f, isError: true);

            // 2. Unlock cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // 3. Gracefully return to Main Menu
            StartCoroutine(GracefulReturnToMainMenuRoutine());
        }

        private IEnumerator GracefulReturnToMainMenuRoutine()
        {
            yield return new WaitForSecondsRealtime(0.5f);

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                try
                {
                    NetworkManager.Singleton.Shutdown();
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[DisconnectHandler] NetworkManager shutdown note: {ex.Message}");
                }
            }

            if (SessionManager.Instance != null)
            {
                _ = SessionManager.Instance.LeaveSession();
            }

            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene != "MainMenu")
            {
                LoadingScreenManager.LoadScene("MainMenu", "CONNECTION TERMINATED");
            }

            _isHandlingHostDisconnect = false;
        }
    }
}
