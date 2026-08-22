using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// Free-For-All game mode. Tracks individual player kills with a target score of 10
    /// and a time limit of 5 minutes.
    /// </summary>
    public class FFAMode : GameModeBase, IScoreProvider
    {
        private const int KillLimit = 10;
        private const float TimeLimit = 300f; // 5 minutes

        public override void OnMatchStart()
        {
            if (!IsServer) return;

            PlayerKills.Clear();
            PlayerDeaths.Clear();
            MatchTimeRemaining.Value = TimeLimit;
            MatchInProgress.Value = true;
            Debug.Log("[FFAMode] Match Started!");
            
            // Ensure all players are positioned at valid FFA spawn points
            if (NetworkBootstrapper.Instance != null)
            {
                NetworkBootstrapper.Instance.SpawnAllConnectedPlayers();
            }

            // Broadcast initial time
            BroadcastTimeRemainingClientRpc(MatchTimeRemaining.Value);
        }

        public override void OnPlayerKilled(ulong killerClientId, ulong victimClientId)
        {
            if (!IsServer || !MatchInProgress.Value) return;

            // Track base scoring (increments kills/deaths in base dictionaries)
            base.OnPlayerKilled(killerClientId, victimClientId);

            int killerKills = PlayerKills.TryGetValue(killerClientId, out var k) ? k : 0;

            // Replicate score change to all clients (for visual feed)
            SyncPlayerKillsClientRpc(killerClientId, killerKills);

            Debug.Log($"[FFAMode] Player {killerClientId} killed Player {victimClientId}. Total Kills: {killerKills}");

            // Win condition check
            if (killerKills >= KillLimit)
            {
                OnMatchEnd();
            }
        }

        public override void OnMatchEnd()
        {
            if (!IsServer) return;

            base.OnMatchEnd();
            
            // Find winner with highest kills
            ulong winnerId = 0;
            int maxKills = -1;
            foreach (var kvp in PlayerKills)
            {
                if (kvp.Value > maxKills)
                {
                    maxKills = kvp.Value;
                    winnerId = kvp.Key;
                }
            }

            string winnerInfo = maxKills > 0 ? $"Player {winnerId} with {maxKills} kills" : "No one (Draw)";
            Debug.Log($"[FFAMode] Match Ended! Winner: {winnerInfo}");

            var scores = CompileScoreboard();
            BroadcastMatchEndClientRpc(winnerInfo, scores);
        }

        [ClientRpc]
        private void SyncPlayerKillsClientRpc(ulong clientId, int kills)
        {
            // Sync locally for client scoring reads if needed
            PlayerKills[clientId] = kills;
            Debug.Log($"[FFA Client] Sync: Player {clientId} has {kills} kills");
        }

        public override int GetPlayerKills(ulong clientId)
        {
            return PlayerKills.TryGetValue(clientId, out int kills) ? kills : 0;
        }

        public int GetPlayerRank(ulong clientId)
        {
            var sortedPlayers = new List<ulong>();
            if (NetworkManager.Singleton != null)
            {
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    sortedPlayers.Add(client.ClientId);
                }
            }
            else
            {
                foreach (var id in PlayerKills.Keys)
                {
                    sortedPlayers.Add(id);
                }
            }

            // Sort players by kills (descending)
            sortedPlayers.Sort((a, b) => {
                int killsA = GetPlayerKills(a);
                int killsB = GetPlayerKills(b);
                return killsB.CompareTo(killsA);
            });

            // Find rank (handling ties)
            int rank = 1;
            int lastKills = -1;
            for (int i = 0; i < sortedPlayers.Count; i++)
            {
                ulong id = sortedPlayers[i];
                int kills = GetPlayerKills(id);
                if (i > 0 && kills < lastKills)
                {
                    rank = i + 1;
                }
                if (id == clientId)
                {
                    return rank;
                }
                lastKills = kills;
            }

            return 1;
        }

        public ScoreDisplayData GetScoreDisplayData()
        {
            var entries = new List<ScoreDisplayEntry>();
            
            ulong localClientId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0;
            int localKills = GetPlayerKills(localClientId);
            
            entries.Add(new ScoreDisplayEntry { Label = "You", Value = $"{localKills} Kills" });

            ulong leaderId = localClientId;
            int maxKills = localKills;
            
            foreach (var kvp in PlayerKills)
            {
                if (kvp.Value > maxKills)
                {
                    maxKills = kvp.Value;
                    leaderId = kvp.Key;
                }
            }

            if (leaderId != localClientId)
            {
                string leaderName = PlayerNameRegistry.GetName(leaderId);
                entries.Add(new ScoreDisplayEntry { Label = $"Leader ({leaderName})", Value = $"{maxKills} Kills" });
            }
            else
            {
                int runnerUpKills = -1;
                ulong runnerUpId = ulong.MaxValue;
                foreach (var kvp in PlayerKills)
                {
                    if (kvp.Key != localClientId && kvp.Value > runnerUpKills)
                    {
                        runnerUpKills = kvp.Value;
                        runnerUpId = kvp.Key;
                    }
                }
                if (runnerUpId != ulong.MaxValue && runnerUpKills >= 0)
                {
                    string runnerName = PlayerNameRegistry.GetName(runnerUpId);
                    entries.Add(new ScoreDisplayEntry { Label = $"2nd ({runnerName})", Value = $"{runnerUpKills} Kills" });
                }
            }

            return new ScoreDisplayData { Entries = entries };
        }
    }
}
