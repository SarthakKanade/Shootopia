using Unity.Netcode;
using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// Central manager that instantiates and runs the selected GameModeBase subclass
    /// on the server, based on current session metadata.
    /// Exposes a static ReportKill method for weapons, projectiles, and damage zones.
    /// </summary>
    public class NetworkedGameManager : NetworkBehaviour
    {
        public static NetworkedGameManager Instance { get; private set; }

        private GameModeBase _activeGameMode;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Automatically add Controllers and Handlers so they are active in the scene!
            if (gameObject.GetComponent<ResultsScreenController>() == null)
            {
                gameObject.AddComponent<ResultsScreenController>();
            }
            if (gameObject.GetComponent<ScoreboardController>() == null)
            {
                gameObject.AddComponent<ScoreboardController>();
            }
            if (DisconnectHandler.Instance == null && gameObject.GetComponent<DisconnectHandler>() == null)
            {
                gameObject.AddComponent<DisconnectHandler>();
            }
            if (NetworkNotificationUI.Instance == null && gameObject.GetComponent<NetworkNotificationUI>() == null)
            {
                gameObject.AddComponent<NetworkNotificationUI>();
            }

            if (_activeGameMode == null)
            {
                var ffa = GetComponent<FFAMode>();
                _activeGameMode = ffa != null ? (GameModeBase)ffa : GetComponent<TDMMode>();
            }
        }

        private void Start()
        {
            TrySpawnSelf();
        }

        private void Update()
        {
            if (!IsSpawned)
            {
                TrySpawnSelf();
            }
        }

        private void TrySpawnSelf()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && !IsSpawned)
            {
                var netObj = GetComponent<NetworkObject>();
                if (netObj != null && !netObj.IsSpawned)
                {
                    netObj.Spawn();
                    Debug.Log("[NetworkedGameManager] Auto-spawned NetworkedGameManager NetworkObject on Server/Host.");
                }
            }
        }

        public readonly NetworkVariable<Unity.Collections.FixedString32Bytes> ActiveModeName = new NetworkVariable<Unity.Collections.FixedString32Bytes>("FFA", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public readonly NetworkVariable<bool> MatchStarted = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            ActiveModeName.OnValueChanged += OnActiveModeChanged;
            MatchStarted.OnValueChanged += OnMatchStartedChanged;

            if (IsServer)
            {
                string selectedMode = "FFA"; // Default fallback
                if (SessionManager.Instance != null)
                {
                    var session = SessionManager.Instance.GetCurrentSession();
                    if (session != null && session.Properties != null && session.Properties.TryGetValue("GameMode", out var prop))
                    {
                        selectedMode = prop.Value;
                    }
                }

                Debug.Log($"[NetworkedGameManager] Initializing match. Selected mode from metadata: {selectedMode}");
                ActiveModeName.Value = selectedMode;
                ApplyActiveGameMode(selectedMode);

                // If in a multiplayer session with WaitingRoom UI present, wait for host to start match
                if (SessionManager.Instance != null && SessionManager.Instance.GetCurrentSession() != null
                    && WaitingRoomController.Instance != null)
                {
                    Debug.Log("[NetworkedGameManager] Multiplayer session active with WaitingRoom UI. Waiting in Warmup until host starts match.");
                    if (_activeGameMode != null)
                    {
                        _activeGameMode.MatchActive.Value = false;
                        _activeGameMode.PreMatchCountdown.Value = -1;
                    }
                    return;
                }

                // Start match immediately: either singleplayer/offline or no WaitingRoom UI to gate on
                Debug.Log("[NetworkedGameManager] Starting match sequence directly (no WaitingRoom UI gate).");
                StartCoroutine(PreMatchSequence());
            }
            else
            {
                ApplyActiveGameMode(ActiveModeName.Value.ToString());
            }

            if (MatchStarted.Value)
            {
                CloseLobbyUI();
            }
        }

        /// <summary>
        /// Entry point called by WaitingRoomController when the host clicks START MATCH.
        /// </summary>
        public void StartMatchFromLobby()
        {
            if (IsServer)
            {
                StartMatchFromLobbyServer();
            }
            else
            {
                StartMatchFromLobbyServerRpc();
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void StartMatchFromLobbyServerRpc()
        {
            StartMatchFromLobbyServer();
        }

        private void StartMatchFromLobbyServer()
        {
            if (!IsServer) return;
            Debug.Log("[NetworkedGameManager] Host officially started match from lobby!");

            MatchStarted.Value = true;
            CloseWaitingRoomRpc();

            // Start the official pre-match sequence and spawn players
            StartCoroutine(PreMatchSequence());
        }

        [Rpc(SendTo.Everyone)]
        private void CloseWaitingRoomRpc()
        {
            CloseLobbyUI();
        }

        public static void CloseLobbyUI()
        {
            Debug.Log("[NetworkedGameManager] Closing waiting room UI.");
            if (WaitingRoomController.Instance != null)
            {
                WaitingRoomController.Instance.OnMatchStarted();
            }
            else
            {
                var wrc = Object.FindAnyObjectByType<WaitingRoomController>();
                if (wrc != null)
                {
                    wrc.OnMatchStarted();
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            ActiveModeName.OnValueChanged -= OnActiveModeChanged;
            MatchStarted.OnValueChanged -= OnMatchStartedChanged;

            if (_activeGameMode != null)
            {
                _activeGameMode.MatchActive.OnValueChanged -= OnMatchActiveChanged;
            }
        }

        private void OnMatchStartedChanged(bool previousValue, bool newValue)
        {
            if (newValue)
            {
                Debug.Log("[NetworkedGameManager] MatchStarted became true — closing waiting room UI.");
                CloseLobbyUI();
            }
        }

        /// <summary>
        /// Called on all clients when MatchActive changes. Ensures lobby UI is closed
        /// when the match becomes active, regardless of how it was triggered.
        /// </summary>
        private void OnMatchActiveChanged(bool previousValue, bool newValue)
        {
            if (newValue && !previousValue)
            {
                Debug.Log("[NetworkedGameManager] MatchActive became true — ensuring waiting room is closed.");
                CloseLobbyUI();
            }
        }

        private void OnActiveModeChanged(Unity.Collections.FixedString32Bytes previousValue, Unity.Collections.FixedString32Bytes newValue)
        {
            ApplyActiveGameMode(newValue.ToString());
        }

        public void ApplyActiveGameMode(string selectedMode)
        {
            if (_activeGameMode != null)
            {
                _activeGameMode.MatchActive.OnValueChanged -= OnMatchActiveChanged;
            }

            var ffa = GetComponent<FFAMode>();
            var tdm = GetComponent<TDMMode>();

            if (selectedMode.Equals("TDM", System.StringComparison.OrdinalIgnoreCase))
            {
                if (tdm != null)
                {
                    _activeGameMode = tdm;
                    if (ffa != null) ffa.enabled = false;
                    tdm.enabled = true;
                }
                else
                {
                    Debug.LogWarning("[NetworkedGameManager] TDMMode component not pre-attached! Adding dynamically.");
                    _activeGameMode = gameObject.AddComponent<TDMMode>();
                }
            }
            else
            {
                if (ffa != null)
                {
                    _activeGameMode = ffa;
                    if (tdm != null) tdm.enabled = false;
                    ffa.enabled = true;
                }
                else
                {
                    Debug.LogWarning("[NetworkedGameManager] FFAMode component not pre-attached! Adding dynamically.");
                    _activeGameMode = gameObject.AddComponent<FFAMode>();
                }
            }

            if (_activeGameMode != null)
            {
                _activeGameMode.MatchActive.OnValueChanged += OnMatchActiveChanged;
                if (_activeGameMode.MatchActive.Value || MatchStarted.Value)
                {
                    CloseLobbyUI();
                }
            }

            Debug.Log($"[NetworkedGameManager] Applied GameMode '{selectedMode}' (IsServer: {IsServer})");
        }

        private System.Collections.IEnumerator PreMatchSequence()
        {
            if (_activeGameMode == null) yield break;

            MatchStarted.Value = true;
            CloseWaitingRoomRpc();

            _activeGameMode.MatchActive.Value = false;
            _activeGameMode.PreMatchCountdown.Value = -1;

            // 1. Wait for players to spawn
            float maxWaitTime = 10f; // maximum wait timeout
            float waitTimer = 0f;
            int expectedPlayers = 1;

            if (SessionManager.Instance != null && SessionManager.Instance.GetCurrentSession() != null)
            {
                var session = SessionManager.Instance.GetCurrentSession();
                if (session.Players != null)
                {
                    expectedPlayers = session.Players.Count;
                }
            }
            Debug.Log($"[NetworkedGameManager] Pre-match: Waiting for {expectedPlayers} players to spawn...");

            while (waitTimer < maxWaitTime)
            {
                int spawnedCount = 0;
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    if (client.PlayerObject != null && client.PlayerObject.IsSpawned)
                    {
                        spawnedCount++;
                    }
                }

                if (spawnedCount >= expectedPlayers && spawnedCount > 0)
                {
                    Debug.Log($"[NetworkedGameManager] All {spawnedCount} expected players spawned!");
                    break;
                }

                yield return new WaitForSeconds(0.5f);
                waitTimer += 0.5f;
            }

            if (waitTimer >= maxWaitTime)
            {
                Debug.LogWarning("[NetworkedGameManager] Pre-match wait timeout reached before all expected players spawned.");
            }

            // Guarantee all players are positioned at their match spawn points
            if (NetworkBootstrapper.Instance != null)
            {
                NetworkBootstrapper.Instance.SpawnAllConnectedPlayers();
            }

            // 2. 3-second countdown
            int countdown = 3;
            while (countdown > 0)
            {
                _activeGameMode.PreMatchCountdown.Value = countdown;
                Debug.Log($"[NetworkedGameManager] Countdown: {countdown}...");
                yield return new WaitForSeconds(1.0f);
                countdown--;
            }

            _activeGameMode.PreMatchCountdown.Value = 0;
            _activeGameMode.MatchActive.Value = true;
            CloseWaitingRoomRpc();
            Debug.Log("[NetworkedGameManager] Match started - players released");

            // 3. Start match game mode logic
            _activeGameMode.OnMatchStart();
        }

        /// <summary>
        /// Exposes a static entrypoint for damage systems to report player kills.
        /// </summary>
        public static void ReportKill(ulong killerClientId, ulong victimClientId, string weaponName)
        {
            if (Instance != null)
            {
                Instance.OnPlayerKilled(killerClientId, victimClientId, weaponName);
            }
            else
            {
                Debug.LogWarning("[NetworkedGameManager] ReportKill called but Instance is null!");
            }
        }

        private void OnPlayerKilled(ulong killerClientId, ulong victimClientId, string weaponName)
        {
            if (!IsServer) return;

            if (_activeGameMode != null)
            {
                // Delegate core game mode logic
                _activeGameMode.OnPlayerKilled(killerClientId, victimClientId);

                // Resolve display names from the player registry
                string killerName = ResolvePlayerDisplayName(killerClientId);
                string victimName = ResolvePlayerDisplayName(victimClientId);

                // Broadcast cosmetic killfeed entry to all clients
                _activeGameMode.BroadcastKillFeedClientRpc(killerName, victimName, weaponName, killerClientId, victimClientId);
            }
        }

        /// <summary>
        /// Resolves a player's display name by looking up their PlayerNameDisplay component.
        /// Falls back to "Player {clientId}" if no display name is registered.
        /// </summary>
        private string ResolvePlayerDisplayName(ulong clientId)
        {
            return PlayerNameRegistry.GetName(clientId);
        }

        /// <summary>
        /// Exposes the currently active game mode (useful for UI displays).
        /// </summary>
        public GameModeBase ActiveGameMode => _activeGameMode;
    }
}
