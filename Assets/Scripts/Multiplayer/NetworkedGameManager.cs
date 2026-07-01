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

                _activeGameMode.OnMatchStart();
            }
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

                // Broadcast cosmetic killfeed entry to all clients
                string killerName = $"Player {killerClientId}";
                string victimName = $"Player {victimClientId}";
                _activeGameMode.BroadcastKillFeedClientRpc(killerName, victimName, weaponName);
            }
        }

        /// <summary>
        /// Exposes the currently active game mode (useful for UI displays).
        /// </summary>
        public GameModeBase ActiveGameMode => _activeGameMode;
    }
}
