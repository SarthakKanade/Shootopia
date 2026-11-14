using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace Shootopia
{
    /// <summary>
    /// Streamlined player spawner and scene synchronization manager for NGO.
    /// Guarantees that player characters are reliably spawned at spawn points
    /// whenever gameplay or lobby scenes (WaitingRoom, Map1) load.
    /// </summary>
    public partial class NetworkBootstrapper : MonoBehaviour
    {
        private static NetworkBootstrapper _instance;
        public static NetworkBootstrapper Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<NetworkBootstrapper>();
                }
                return _instance;
            }
            private set => _instance = value;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }

            _instance = this;

            // Maintain 2-tick interpolation buffer so opponents smoothly glide across network packet jitter without starvation or jumps
            Unity.Netcode.Components.NetworkTransform.InterpolationBufferTickOffset = Unity.Netcode.Components.ClientNetworkTransform.TargetInterpolationTickOffset;

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            HookNetworkEvents();
        }

        private void OnEnable()
        {
            HookNetworkEvents();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (_instance == this)
            {
                UnhookNetworkEvents();
                _instance = null;
            }
        }

        public void HookNetworkEvents()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
                NetworkManager.Singleton.OnServerStarted += OnServerStarted;
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

                if (NetworkManager.Singleton.SceneManager != null)
                {
                    NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;
                    NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
                }
            }
        }

        private void UnhookNetworkEvents()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;

                if (NetworkManager.Singleton.SceneManager != null)
                {
                    NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;
                }
            }
        }

        private bool IsValidSpawnScene(string sceneName)
        {
            return !string.IsNullOrEmpty(sceneName) && sceneName != "MainMenu" && sceneName != "Bootstrap";
        }

        private void OnServerStarted()
        {
            Debug.Log("[NetworkBootstrapper] NGO Server started. Registering SceneManager and Client callbacks.");
            HookNetworkEvents();

            string currentScene = SceneManager.GetActiveScene().name;
            if (IsValidSpawnScene(currentScene))
            {
                SpawnAllConnectedPlayers();
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[NetworkBootstrapper] Unity Scene loaded: '{scene.name}'");

            LoadingScreenManager.HideOverlay();

            SceneManager.SetActiveScene(scene);

            // Client and Host local immediate safety positioning
            if (MapRegistry.IsGameplayMap(scene.name))
            {
                if (SpawnPointManager.Instance != null)
                {
                    SpawnPointManager.Instance.InitializeSpawnPoints();
                }

                if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.LocalClient.PlayerObject != null)
                {
                    var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
                    var cc = localPlayer.GetComponent<CharacterController>();
                    if (cc != null) cc.enabled = false;
                    Vector3 safePos = Vector3.up * 0.5f;
                    if (SpawnPointManager.Instance != null)
                    {
                        var sp = SpawnPointManager.Instance.GetSpawnPoint(SpawnPointType.FFA);
                        if (sp != null) safePos = sp.position;
                    }
                    localPlayer.transform.position = safePos;
                    if (cc != null) cc.enabled = true;
                    Debug.Log($"[NetworkBootstrapper] Local player instantly positioned at safe point {safePos} on scene '{scene.name}'.");
                }
            }

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                HookNetworkEvents();
                // If NetworkSceneManager is not active (offline/singleplayer testing), fallback to local spawn
                if (NetworkManager.Singleton.SceneManager == null && IsValidSpawnScene(scene.name))
                {
                    SpawnPlayerForClient(NetworkManager.Singleton.LocalClientId);
                }
            }
        }

        private void OnSceneEvent(SceneEvent sceneEvent)
        {
            Debug.Log($"[NetworkBootstrapper] OnSceneEvent: Type={sceneEvent.SceneEventType}, SceneName={sceneEvent.SceneName}, ClientId={sceneEvent.ClientId}");

            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

            string activeSceneName = SceneManager.GetActiveScene().name;
            if (!IsValidSpawnScene(activeSceneName)) return;

            if (sceneEvent.SceneEventType == SceneEventType.LoadComplete)
            {
                Debug.Log($"[NetworkBootstrapper] Client {sceneEvent.ClientId} finished loading '{sceneEvent.SceneName}'. Ensuring player spawned...");
                if (NetworkManager.Singleton.ConnectedClients.TryGetValue(sceneEvent.ClientId, out var client) && (client.PlayerObject == null || !client.PlayerObject.IsSpawned))
                {
                    SpawnPlayerForClient(sceneEvent.ClientId, cleanRespawn: false);
                }
            }
            else if (sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted)
            {
                Debug.Log($"[NetworkBootstrapper] All clients finished loading '{sceneEvent.SceneName}'. Ensuring all players cleanly spawned in scene...");
                SpawnAllConnectedPlayers(cleanRespawn: false);
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            Debug.Log($"[NetworkBootstrapper] Client {clientId} connected to network session.");

            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

            string activeSceneName = SceneManager.GetActiveScene().name;
            if (IsValidSpawnScene(activeSceneName))
            {
                // Only spawn if player object has not already been created (e.g. by scene load event)
                if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                {
                    if (client.PlayerObject == null || !client.PlayerObject.IsSpawned)
                    {
                        SpawnPlayerForClient(clientId);
                    }
                }
                else
                {
                    SpawnPlayerForClient(clientId);
                }
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            Debug.Log($"[NetworkBootstrapper] Client {clientId} disconnected from network session.");

            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                if (client.PlayerObject != null && client.PlayerObject.IsSpawned)
                {
                    try { client.PlayerObject.Despawn(true); } catch { }
                }
            }
        }

    }
}
