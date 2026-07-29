using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Multiplayer;
using Unity.Services.Relay.Models;

namespace Shootopia
{
    /// <summary>
    /// Bootstraps the Netcode for GameObjects (NGO) NetworkManager when the gameplay scene loads.
    /// Configures the UnityTransport using UGS Session Relay allocations.
    /// </summary>
    public class NetworkBootstrapper : MonoBehaviour
    {
        private bool _isInitializing = false;

        private void Start()
        {
            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.OnSessionJoined.AddListener(OnSessionReady);
                SessionManager.Instance.OnSessionCreated.AddListener(OnSessionReady);
            }

            TryInitialize();
        }

        private void OnSessionReady()
        {
            TryInitialize();
        }

        private void TryInitialize()
        {
            if (_isInitializing) return;

            if (SessionManager.Instance == null)
            {
                Debug.LogWarning("[NetworkBootstrapper] SessionManager.Instance is null. Auto-starting Host for local testing...");
                if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
                {
                    _isInitializing = true;
                    NetworkManager.Singleton.StartHost();
                }
                return;
            }

            var session = SessionManager.Instance.GetCurrentSession();
            if (session == null)
            {
                Debug.Log("[NetworkBootstrapper] No active UGS Session found yet. Waiting for session initialization...");
                return;
            }

            if (NetworkManager.Singleton == null)
            {
                Debug.LogWarning("[NetworkBootstrapper] NetworkManager.Singleton is null!");
                return;
            }

            _isInitializing = true;

            // Subscribe to NGO connection callbacks to track final client IDs
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
            NetworkManager.Singleton.ConnectionApprovalCallback = ConnectionApproval;

            if (NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
            }

            if (NetworkManager.Singleton.IsListening)
            {
                string role = NetworkManager.Singleton.IsHost ? "Host" : "Client";
                Debug.Log($"[NetworkBootstrapper] Already connected as {role}. Local Client ID: {NetworkManager.Singleton.LocalClientId}");
                
                string activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                if (NetworkManager.Singleton.IsServer && activeSceneName == "WaitingRoom")
                {
                    Debug.Log("[NetworkBootstrapper] Already listening in WaitingRoom. Spawning players.");
                    SpawnPlayersForConnectedClients();
                }
                return;
            }

            ConfigureAndConnect(session);
        }

        private void OnDestroy()
        {
            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.OnSessionJoined.RemoveListener(OnSessionReady);
                SessionManager.Instance.OnSessionCreated.RemoveListener(OnSessionReady);
            }

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
                NetworkManager.Singleton.ConnectionApprovalCallback = null;
                if (NetworkManager.Singleton.SceneManager != null)
                {
                    NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;
                }
            }
        }

        private void ConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            response.Approved = true;
            response.CreatePlayerObject = false; // Disable NGO automatic spawning
            response.Pending = false;
        }

        private void OnSceneEvent(SceneEvent sceneEvent)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            // LoadEventCompleted fires on the server once ALL connected clients have finished
            // loading the scene. This is the only safe moment to spawn player objects, because
            // the client's NGO state machine is fully ready to receive network object messages.
            if (sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted)
            {
                string activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                if (sceneEvent.SceneName == activeSceneName)
                {
                    Debug.Log($"[NetworkBootstrapper] All clients loaded {activeSceneName}. Spawning players.");
                    SpawnPlayersForConnectedClients();
                }
            }
        }

        private void SpawnPlayerForClient(ulong clientId)
        {
            var playerPrefab = NetworkManager.Singleton.NetworkConfig.PlayerPrefab;
            if (playerPrefab == null)
            {
                Debug.LogError("[NetworkBootstrapper] PlayerPrefab is not assigned in NetworkManager!");
                return;
            }

            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                bool needsSpawn = false;
                if (client.PlayerObject == null)
                {
                    needsSpawn = true;
                }
                else if (client.PlayerObject.gameObject.scene.name != UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)
                {
                    Debug.Log($"[NetworkBootstrapper] Client {clientId} has a player object in another scene ('{client.PlayerObject.gameObject.scene.name}'). Destroying old player object and spawning a new one...");
                    
                    var oldNo = client.PlayerObject;
                    if (oldNo.IsSpawned)
                    {
                        oldNo.Despawn(true);
                    }
                    else
                    {
                        Destroy(oldNo.gameObject);
                    }
                    needsSpawn = true;
                }

                if (needsSpawn)
                {
                    int index = 0;
                    for (int i = 0; i < NetworkManager.Singleton.ConnectedClientsList.Count; i++)
                    {
                        if (NetworkManager.Singleton.ConnectedClientsList[i].ClientId == clientId)
                        {
                            index = i;
                            break;
                        }
                    }
                    Vector3 pos = Vector3.zero + new Vector3(index * 2f, 0.5f, 0f);

                    GameObject playerGo = Instantiate(playerPrefab, pos, Quaternion.identity);
                    var no = playerGo.GetComponent<NetworkObject>();
                    no.SpawnAsPlayerObject(clientId, true);
                    Debug.Log($"[NetworkBootstrapper] Spawned player for client {clientId} at {pos}");

                    AssignPlayerDisplayName(clientId, playerGo);
                }
                else
                {
                    Debug.Log($"[NetworkBootstrapper] Client {clientId} already has a player object in this scene: '{client.PlayerObject.name}'");
                }
            }
        }

        private void AssignPlayerDisplayName(ulong clientId, GameObject playerGo)
        {
            var nameDisplay = playerGo.GetComponent<PlayerNameDisplay>();
            if (nameDisplay == null) return;

            string displayName = ResolveDisplayName(clientId);
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = $"Player {clientId}";
            }

            nameDisplay.DisplayName.Value = displayName;
            Debug.Log($"[NetworkBootstrapper] Assigned display name '{displayName}' to client {clientId}");

            // Register in PlayerNameRegistry
            PlayerNameRegistry.Register(clientId, displayName);
        }

        private string ResolveDisplayName(ulong clientId)
        {
            if (SessionManager.Instance == null) return null;

            var session = SessionManager.Instance.GetCurrentSession();
            if (session == null) return null;

            int clientIndex = -1;
            for (int i = 0; i < NetworkManager.Singleton.ConnectedClientsList.Count; i++)
            {
                if (NetworkManager.Singleton.ConnectedClientsList[i].ClientId == clientId)
                {
                    clientIndex = i;
                    break;
                }
            }

            if (clientIndex < 0) return null;

            int playerIndex = 0;
            if (session.Players != null)
            {
                foreach (var player in session.Players)
                {
                    if (player == null) continue;
                    if (playerIndex == clientIndex)
                    {
                        return player.GetPlayerName();
                    }
                    playerIndex++;
                }
            }

            return null;
        }

        private void SpawnPlayersForConnectedClients()
        {
            var playerPrefab = NetworkManager.Singleton.NetworkConfig.PlayerPrefab;
            if (playerPrefab == null)
            {
                Debug.LogError("[NetworkBootstrapper] PlayerPrefab is not assigned in NetworkManager!");
                return;
            }

            Debug.Log("[NetworkBootstrapper] Spawning player characters for all connected clients...");
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                SpawnPlayerForClient(client.ClientId);
            }
        }

        private void ConfigureAndConnect(ISession session)
        {
#if UNITY_WEBGL
            string connectionType = "wss";
#else
            string connectionType = "udp";
#endif

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport == null)
            {
                Debug.LogError("[NetworkBootstrapper] UnityTransport component not found on NetworkManager!");
                return;
            }

            try
            {
                if (session.IsHost)
                {
                    Debug.Log("[NetworkBootstrapper] Configuring Relay host allocation...");
                    var allocation = GetHostAllocation(session);
                    if (allocation == null)
                    {
                        Debug.LogError("[NetworkBootstrapper] Failed to retrieve Host Relay Allocation from session!");
                        return;
                    }

                    var relayServerData = AllocationUtils.ToRelayServerData(allocation, connectionType);
                    transport.SetRelayServerData(relayServerData);

                    Debug.Log("[NetworkBootstrapper] Starting NetworkManager as Host...");
                    if (NetworkManager.Singleton.StartHost())
                    {
                        Debug.Log("[NetworkBootstrapper] StartHost successfully initiated.");
                        string activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                        if (activeSceneName == "WaitingRoom")
                        {
                            SpawnPlayerForClient(NetworkManager.Singleton.LocalClientId);
                        }
                    }
                    else
                    {
                        Debug.LogError("[NetworkBootstrapper] StartHost failed to start!");
                    }
                }
                else
                {
                    Debug.Log("[NetworkBootstrapper] Configuring Relay client join allocation...");
                    var joinAllocation = GetClientJoinAllocation(session);
                    if (joinAllocation == null)
                    {
                        Debug.LogError("[NetworkBootstrapper] Failed to retrieve Client Join Allocation from session!");
                        return;
                    }

                    var relayServerData = AllocationUtils.ToRelayServerData(joinAllocation, connectionType);
                    transport.SetRelayServerData(relayServerData);

                    Debug.Log("[NetworkBootstrapper] Starting NetworkManager as Client...");
                    if (NetworkManager.Singleton.StartClient())
                    {
                        Debug.Log("[NetworkBootstrapper] StartClient successfully initiated.");
                    }
                    else
                    {
                        Debug.LogError("[NetworkBootstrapper] StartClient failed to start!");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkBootstrapper] Error during connection boot-up: {ex.Message}");
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                string role = NetworkManager.Singleton.IsHost ? "Host" : "Client";
                Debug.Log($"[NetworkBootstrapper] Connected as {role}. Local Client ID: {clientId}");
            }
            else
            {
                Debug.Log($"[NetworkBootstrapper] Remote client connected to host. Client ID: {clientId}");
            }

            if (NetworkManager.Singleton.IsServer)
            {
                string activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                if (activeSceneName == "WaitingRoom")
                {
                    Debug.Log($"[NetworkBootstrapper] Client {clientId} connected while in WaitingRoom. Spawning player.");
                    SpawnPlayerForClient(clientId);
                }
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                Debug.LogWarning($"[NetworkBootstrapper] Local client disconnected from network session. Client ID: {clientId}");
            }
            else
            {
                Debug.Log($"[NetworkBootstrapper] Remote client disconnected from host. Client ID: {clientId}");
            }
        }

        private Allocation GetHostAllocation(ISession session)
        {
            var network = session.Network;
            if (network == null) return null;

            var relayHandlerField = network.GetType().GetField("m_RelayHandler", BindingFlags.NonPublic | BindingFlags.Instance);
            if (relayHandlerField == null) return null;

            var relayHandler = relayHandlerField.GetValue(network);
            if (relayHandler == null) return null;

            var allocationField = relayHandler.GetType().GetField("m_Allocation", BindingFlags.NonPublic | BindingFlags.Instance);
            if (allocationField == null) return null;

            return allocationField.GetValue(relayHandler) as Allocation;
        }

        private JoinAllocation GetClientJoinAllocation(ISession session)
        {
            var network = session.Network;
            if (network == null) return null;

            var relayHandlerField = network.GetType().GetField("m_RelayHandler", BindingFlags.NonPublic | BindingFlags.Instance);
            if (relayHandlerField == null) return null;

            var relayHandler = relayHandlerField.GetValue(network);
            if (relayHandler == null) return null;

            var joinAllocationField = relayHandler.GetType().GetField("m_JoinAllocation", BindingFlags.NonPublic | BindingFlags.Instance);
            if (joinAllocationField == null) return null;

            return joinAllocationField.GetValue(relayHandler) as JoinAllocation;
        }
    }
}
