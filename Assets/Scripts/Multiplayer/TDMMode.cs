using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// Team Deathmatch game mode. Features Blue (0) and Red (1) teams, auto-balancing
    /// on player connect, a 15-kill score limit, and a 7-minute time limit.
    /// </summary>
    public class TDMMode : GameModeBase, IScoreProvider
    {
        public const int DefaultKillLimit = 15;
        public const float DefaultTimeLimit = 420f; // 7 minutes

        // Team representation: 0 = Blue, 1 = Red
        private readonly Dictionary<ulong, int> playerTeam = new Dictionary<ulong, int>();

        [Header("TDM Scores")]
        protected readonly NetworkVariable<int> BlueScore = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        protected readonly NetworkVariable<int> RedScore = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public int ScoreBlue => BlueScore.Value;
        public int ScoreRed => RedScore.Value;

        public override void OnMatchStart()
        {
            if (!IsServer) return;

            MatchActive.Value = true;
            PlayerKills.Clear();
            PlayerDeaths.Clear();
            BlueScore.Value = 0;
            RedScore.Value = 0;

            float duration = InitialMatchDuration.Value > 0f ? InitialMatchDuration.Value : DefaultTimeLimit;
            InitialMatchDuration.Value = duration;
            MatchTimeRemaining.Value = duration;

            if (TargetScoreLimit.Value <= 0)
            {
                TargetScoreLimit.Value = DefaultKillLimit;
            }

            MatchInProgress.Value = true;
            Debug.Log($"[TDMMode] Match Started! Duration: {duration}s, KillLimit: {TargetScoreLimit.Value}");

            // Subscribe to late-joining connections
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                
                // Assign any unassigned connected players
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    AssignPlayerToTeam(client.ClientId);
                }
            }

            // Reposition all currently connected players to their team spawn points
            if (SpawnPointManager.Instance != null && NetworkManager.Singleton != null)
            {
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    if (client.PlayerObject != null)
                    {
                        int team = GetPlayerTeam(client.ClientId);
                        SpawnPointType spawnType = (team == 0) ? SpawnPointType.TeamBlue : SpawnPointType.TeamRed;
                        Transform sp = SpawnPointManager.Instance.GetSpawnPoint(spawnType);
                        if (sp != null)
                        {
                            var health = client.PlayerObject.GetComponent<PlayerHealth>();
                            if (health != null)
                            {
                                try
                                {
                                    health.ResetStateForMatch(sp.position, sp.rotation);
                                }
                                catch (System.Exception ex)
                                {
                                    Debug.LogWarning($"[TDMMode] Error resetting state for client {client.ClientId}: {ex.Message}");
                                }
                            }
                        }
                    }
                }
            }

            Debug.Log("[TDMMode] Match Started!");
            
            // Broadcast initial time
            BroadcastTimeRemainingClientRpc(MatchTimeRemaining.Value);
        }

        /// <summary>
        /// Pre-assigns connected players to their selected teams before initial spawning in PreMatchSequence.
        /// </summary>
        public void InitializeTeamsBeforeSpawn()
        {
            if (!IsServer) return;

            if (NetworkManager.Singleton != null)
            {
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    AssignPlayerToTeam(client.ClientId);
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            if (!IsServer || !MatchInProgress.Value) return;
            AssignPlayerToTeam(clientId);

            // Replicate all player team assignments to all clients so late joiners have full team state
            foreach (var kvp in playerTeam)
            {
                SyncPlayerTeamClientRpc(kvp.Key, kvp.Value);
            }
        }

        private void AssignPlayerToTeam(ulong clientId)
        {
            if (!IsServer) return;

            // Skip if already assigned
            if (playerTeam.ContainsKey(clientId))
            {
                // Re-sync to the joining client just in case
                SyncPlayerTeamClientRpc(clientId, playerTeam[clientId]);
                return;
            }

            int assignedTeam = -1;

            // 1. Honor player's chosen team from Waiting Room lobby
            if (SessionManager.Instance != null)
            {
                var readyStates = SessionManager.Instance.GetPlayerReadyStates();
                if (readyStates != null)
                {
                    string targetName = PlayerNameRegistry.GetName(clientId);
                    var match = readyStates.Find(r => 
                        r.playerId == clientId.ToString() || 
                        (!string.IsNullOrEmpty(targetName) && r.displayName == targetName));

                    if (!string.IsNullOrEmpty(match.playerId))
                    {
                        assignedTeam = Mathf.Clamp(match.teamIndex, 0, 1);
                    }
                }
            }

            // 2. Fallback: Count current teams to auto-balance
            if (assignedTeam < 0)
            {
                int blueCount = 0;
                int redCount = 0;
                foreach (var kvp in playerTeam)
                {
                    if (kvp.Value == 0) blueCount++;
                    else if (kvp.Value == 1) redCount++;
                }

                assignedTeam = blueCount <= redCount ? 0 : 1;
            }

            playerTeam[clientId] = assignedTeam;

            // Broadcast assignment to all clients
            SyncPlayerTeamClientRpc(clientId, assignedTeam);

            Debug.Log($"[TDMMode] Assigned Player {clientId} to {(assignedTeam == 0 ? "Blue" : "Red")} team.");
        }

        public override void OnPlayerKilled(ulong killerClientId, ulong victimClientId)
        {
            if (!IsServer || !MatchInProgress.Value) return;

            // Track base scoring (increments kills or penalizes -1 for self death)
            base.OnPlayerKilled(killerClientId, victimClientId);

            bool isSelfDeath = (killerClientId == victimClientId);
            if (isSelfDeath)
            {
                if (!playerTeam.TryGetValue(victimClientId, out int victimTeam))
                {
                    AssignPlayerToTeam(victimClientId);
                    victimTeam = playerTeam[victimClientId];
                }

                if (victimTeam == 0)
                {
                    BlueScore.Value = Mathf.Max(0, BlueScore.Value - 1);
                    Debug.Log($"[TDMMode] Self-death deduction (-1) for Blue Team! Blue: {BlueScore.Value}, Red: {RedScore.Value}");
                }
                else if (victimTeam == 1)
                {
                    RedScore.Value = Mathf.Max(0, RedScore.Value - 1);
                    Debug.Log($"[TDMMode] Self-death deduction (-1) for Red Team! Blue: {BlueScore.Value}, Red: {RedScore.Value}");
                }
                return;
            }

            // Retrieve killer's team
            if (!playerTeam.TryGetValue(killerClientId, out int killerTeam))
            {
                // Auto-assign if they aren't registered for some reason
                AssignPlayerToTeam(killerClientId);
                killerTeam = playerTeam[killerClientId];
            }

            // Add score to killer's team
            int effectiveLimit = TargetScoreLimit.Value > 0 ? TargetScoreLimit.Value : DefaultKillLimit;
            if (killerTeam == 0)
            {
                BlueScore.Value++;
                Debug.Log($"[TDMMode] Blue Team scores! Blue: {BlueScore.Value}, Red: {RedScore.Value}");
                if (BlueScore.Value >= effectiveLimit)
                {
                    OnMatchEnd();
                }
            }
            else if (killerTeam == 1)
            {
                RedScore.Value++;
                Debug.Log($"[TDMMode] Red Team scores! Blue: {BlueScore.Value}, Red: {RedScore.Value}");
                if (RedScore.Value >= effectiveLimit)
                {
                    OnMatchEnd();
                }
            }
        }

        public override void OnMatchEnd()
        {
            if (!IsServer) return;

            base.OnMatchEnd();

            string winnerInfo;
            if (BlueScore.Value > RedScore.Value)
            {
                winnerInfo = $"Blue Team Wins ({BlueScore.Value} - {RedScore.Value})";
            }
            else if (RedScore.Value > BlueScore.Value)
            {
                winnerInfo = $"Red Team Wins ({RedScore.Value} - {BlueScore.Value})";
            }
            else
            {
                winnerInfo = $"Draw ({BlueScore.Value} - {RedScore.Value})";
            }

            Debug.Log($"[TDMMode] Match Ended! Winner: {winnerInfo}");

            var scores = CompileScoreboard();
            BroadcastMatchEndClientRpc(winnerInfo, scores);
        }

        [ClientRpc]
        private void SyncPlayerTeamClientRpc(ulong clientId, int teamId)
        {
            playerTeam[clientId] = teamId;
            Debug.Log($"[TDM Client] Player {clientId} is on team: {(teamId == 0 ? "Blue" : "Red")}");
        }

        public int GetPlayerTeam(ulong clientId)
        {
            return playerTeam.TryGetValue(clientId, out int teamId) ? teamId : -1;
        }

        public ScoreDisplayData GetScoreDisplayData()
        {
            var entries = new List<ScoreDisplayEntry>
            {
                new ScoreDisplayEntry { Label = "Blue", Value = ScoreBlue.ToString() },
                new ScoreDisplayEntry { Label = "Red", Value = ScoreRed.ToString() }
            };

            return new ScoreDisplayData { Entries = entries };
        }
    }
}
