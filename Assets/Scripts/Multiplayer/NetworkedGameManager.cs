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

            // Automatically add ResultsScreenController so it is active in the scene!
            if (gameObject.GetComponent<ResultsScreenController>() == null)
            {
                gameObject.AddComponent<ResultsScreenController>();
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

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

                Debug.Log($"[NetworkedGameManager] Starting match. Selected mode from metadata: {selectedMode}");

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
                        Debug.LogWarning("[NetworkedGameManager] TDMMode component not pre-attached! Adding dynamically (RPCs may fail).");
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
                        Debug.LogWarning("[NetworkedGameManager] FFAMode component not pre-attached! Adding dynamically (RPCs may fail).");
                        _activeGameMode = gameObject.AddComponent<FFAMode>();
                    }
                }

                // Check if active scene is the WaitingRoom (Lobby)
                string activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                if (activeSceneName == "WaitingRoom")
                {
                    Debug.Log("[NetworkedGameManager] Active scene is WaitingRoom. Skipping match start countdown.");
                    return;
                }

                // Start the pre-match countdown sequence in GameScene
                StartCoroutine(PreMatchSequence());
            }
        }

        private System.Collections.IEnumerator PreMatchSequence()
        {
            if (_activeGameMode == null) yield break;

            _activeGameMode.MatchActive.Value = false;
            _activeGameMode.PreMatchCountdown.Value = -1;

            // 1. Wait for players to spawn
            float maxWaitTime = 10f; // maximum wait timeout
            float waitTimer = 0f;
            int expectedPlayers = 1;

            if (SessionManager.Instance != null && SessionManager.Instance.GetCurrentSession() != null)
            {
                expectedPlayers = SessionManager.Instance.GetCurrentSession().Players.Count;
            }
            Debug.Log($"[NetworkedGameManager] Pre-match: Waiting for {expectedPlayers} players to spawn...");

            while (waitTimer < maxWaitTime)
            {
                int spawnedCount = 0;
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    if (client.PlayerObject != null)
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
                _activeGameMode.BroadcastKillFeedClientRpc(killerName, victimName, weaponName);
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
