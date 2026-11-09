using UnityEngine;
using Unity.Netcode;

namespace Shootopia
{
    public partial class TDMMode
    {
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

        [Rpc(SendTo.ClientsAndHost)]
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

