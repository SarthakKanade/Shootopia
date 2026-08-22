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
    public class NetworkBootstrapper : MonoBehaviour
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
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

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
            if (scene.name == "Map1" || scene.name == "GameScene" || scene.name == "Shootopia_Sandbox")
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
                    Vector3 safePos = new Vector3(120f, 16f, 2.5f);
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
                if (IsValidSpawnScene(scene.name))
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
                Debug.Log($"[NetworkBootstrapper] Client {sceneEvent.ClientId} finished loading '{sceneEvent.SceneName}'. Positioning player...");
                SpawnPlayerForClient(sceneEvent.ClientId, cleanRespawn: false);
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
                SpawnPlayerForClient(clientId);
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

            PlayerNameRegistry.Unregister(clientId);
        }

        public void SpawnAllConnectedPlayers(bool cleanRespawn = false)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                SpawnPlayerForClient(client.ClientId, cleanRespawn);
            }
        }

        public void SpawnPlayerForClient(ulong clientId, bool cleanRespawn = false)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

            string currentSceneName = SceneManager.GetActiveScene().name;
            if (!IsValidSpawnScene(currentSceneName)) return;

            // Resolve spawn position with safe platform fallback (never spawn in lava at 0,0,0)
            Vector3 spawnPos = new Vector3(120f, 16f, 2.5f);
            Quaternion spawnRot = Quaternion.identity;

            if (SpawnPointManager.Instance != null)
            {
                SpawnPointType spawnType = SpawnPointType.FFA;
                if (GameModeBase.Instance is TDMMode tdm)
                {
                    int team = tdm.GetPlayerTeam(clientId);
                    if (team == 0) spawnType = SpawnPointType.TeamBlue;
                    else if (team == 1) spawnType = SpawnPointType.TeamRed;
                }

                Transform sp = SpawnPointManager.Instance.GetSpawnPoint(spawnType);
                if (sp != null)
                {
                    spawnPos = sp.position;
                    spawnRot = sp.rotation;
                }
            }
            else if (currentSceneName == "WaitingRoom")
            {
                spawnPos = new Vector3(15f, 0.5f, 15f);
            }

            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                if (client.PlayerObject != null)
                {
                    bool sceneMismatch = client.PlayerObject.gameObject.scene != SceneManager.GetActiveScene();
                    bool notSpawned = !client.PlayerObject.IsSpawned;

                    if (cleanRespawn || sceneMismatch || notSpawned)
                    {
                        Debug.Log($"[NetworkBootstrapper] Cleaning up existing player object for client {clientId} (cleanRespawn={cleanRespawn}, sceneMismatch={sceneMismatch}, notSpawned={notSpawned}).");
                        try
                        {
                            if (client.PlayerObject.IsSpawned)
                            {
                                client.PlayerObject.Despawn(true);
                            }
                            else
                            {
                                Destroy(client.PlayerObject.gameObject);
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogWarning($"[NetworkBootstrapper] Exception while despawning old player object for client {clientId}: {ex.Message}");
                        }
                    }
                    else
                    {
                        Debug.Log($"[NetworkBootstrapper] Client {clientId} has spawned player. Repositioning to {spawnPos} in '{currentSceneName}'.");
                        var health = client.PlayerObject.GetComponent<PlayerHealth>();
                        if (health != null)
                        {
                            health.ResetStateForMatch(spawnPos);
                        }
                        else
                        {
                            var cc = client.PlayerObject.GetComponent<CharacterController>();
                            if (cc != null) cc.enabled = false;
                            client.PlayerObject.transform.position = spawnPos;
                            client.PlayerObject.transform.rotation = spawnRot;
                            if (cc != null) cc.enabled = true;
                        }
                        return;
                    }
                }
            }

            var playerPrefab = NetworkManager.Singleton.NetworkConfig.PlayerPrefab;
            if (playerPrefab == null)
            {
                Debug.LogError("[NetworkBootstrapper] PlayerPrefab is not assigned in NetworkManager NetworkConfig!");
                return;
            }

            var playerInstance = Instantiate(playerPrefab, spawnPos, spawnRot);
            var netObj = playerInstance.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.SpawnAsPlayerObject(clientId, true);
                Debug.Log($"[NetworkBootstrapper] Successfully spawned fresh player for client {clientId} at {spawnPos} in scene '{currentSceneName}'.");
            }
            else
            {
                Debug.LogError("[NetworkBootstrapper] Instantiated player prefab is missing a NetworkObject component!");
                return;
            }

            AssignPlayerDisplayName(clientId, playerInstance);
        }

        private void AssignPlayerDisplayName(ulong clientId, GameObject playerObject)
        {
            string displayName = ResolveDisplayName(clientId);
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = $"Agent_{clientId}";
            }

            var nameDisplay = playerObject.GetComponent<PlayerNameDisplay>();
            if (nameDisplay != null)
            {
                nameDisplay.DisplayName.Value = displayName;
            }

            PlayerNameRegistry.Register(clientId, displayName);
            Debug.Log($"[NetworkBootstrapper] Assigned display name '{displayName}' to client {clientId}.");
        }

        private string ResolveDisplayName(ulong clientId)
        {
            if (SessionManager.Instance == null) return null;
            var session = SessionManager.Instance.GetCurrentSession();
            if (session == null || session.Players == null) return null;

            int clientIdx = 0;
            for (int i = 0; i < NetworkManager.Singleton.ConnectedClientsList.Count; i++)
            {
                if (NetworkManager.Singleton.ConnectedClientsList[i].ClientId == clientId)
                {
                    clientIdx = i;
                    break;
                }
            }

            int pIdx = 0;
            foreach (var player in session.Players)
            {
                if (player == null) continue;
                if (pIdx == clientIdx)
                {
                    if (player.Properties != null && player.Properties.TryGetValue("DisplayName", out var prop) && !string.IsNullOrEmpty(prop.Value))
                    {
                        return prop.Value;
                    }
                    return $"Agent_{clientId}";
                }
                pIdx++;
            }

            return null;
        }
    }
}
