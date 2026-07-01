using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// Team Deathmatch game mode. Features Blue (0) and Red (1) teams, auto-balancing
    /// on player connect, a 15-kill score limit, and a 7-minute time limit.
    /// </summary>
    public class TDMMode : GameModeBase
    {
        private const int KillLimit = 15;
        private const float TimeLimit = 420f; // 7 minutes

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

            playerTeam.Clear();
            BlueScore.Value = 0;
            RedScore.Value = 0;
            MatchTimeRemaining.Value = TimeLimit;
            MatchInProgress.Value = true;

            // Subscribe to late-joining connections
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                
                // Assign all currently connected players
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    AssignPlayerToTeam(client.ClientId);
                }
            }

            Debug.Log("[TDMMode] Match Started!");
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

            // Count current teams to auto-balance
            int blueCount = 0;
            int redCount = 0;
            foreach (var kvp in playerTeam)
            {
                if (kvp.Value == 0) blueCount++;
                else if (kvp.Value == 1) redCount++;
            }

            // Assign to the smaller team
            int assignedTeam = blueCount <= redCount ? 0 : 1;
            playerTeam[clientId] = assignedTeam;

            // Broadcast assignment to all clients
            SyncPlayerTeamClientRpc(clientId, assignedTeam);

            Debug.Log($"[TDMMode] Assigned Player {clientId} to {(assignedTeam == 0 ? "Blue" : "Red")} team (Blue: {blueCount}, Red: {redCount})");
        }

        public override void OnPlayerKilled(ulong killerClientId, ulong victimClientId)
        {
            if (!IsServer || !MatchInProgress.Value) return;

            // Retrieve killer's team
            if (!playerTeam.TryGetValue(killerClientId, out int killerTeam))
            {
                // Auto-assign if they aren't registered for some reason
                AssignPlayerToTeam(killerClientId);
                killerTeam = playerTeam[killerClientId];
            }

            // Add score to killer's team
            if (killerTeam == 0)
            {
                BlueScore.Value++;
                Debug.Log($"[TDMMode] Blue Team scores! Blue: {BlueScore.Value}, Red: {RedScore.Value}");
                if (BlueScore.Value >= KillLimit)
                {
                    OnMatchEnd();
                }
            }
            else if (killerTeam == 1)
            {
                RedScore.Value++;
                Debug.Log($"[TDMMode] Red Team scores! Blue: {BlueScore.Value}, Red: {RedScore.Value}");
                if (RedScore.Value >= KillLimit)
                {
                    OnMatchEnd();
                }
            }
        }

        private void Update()
        {
            if (!IsServer || !MatchInProgress.Value) return;

            MatchTimeRemaining.Value -= Time.deltaTime;
            if (MatchTimeRemaining.Value <= 0f)
            {
                MatchTimeRemaining.Value = 0f;
                OnMatchEnd();
            }
        }

        public override void OnMatchEnd()
        {
            if (!IsServer) return;

            MatchInProgress.Value = false;

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
            BroadcastMatchEndClientRpc(winnerInfo);
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
    }
}
