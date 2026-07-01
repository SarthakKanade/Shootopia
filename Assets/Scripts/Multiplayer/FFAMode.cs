using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// Free-For-All game mode. Tracks individual player kills with a target score of 10
    /// and a time limit of 5 minutes.
    /// </summary>
    public class FFAMode : GameModeBase
    {
        private const int KillLimit = 10;
        private const float TimeLimit = 300f; // 5 minutes

        // Server-side & Client-side kill tracking
        private readonly Dictionary<ulong, int> _playerKills = new Dictionary<ulong, int>();

        public override void OnMatchStart()
        {
            if (!IsServer) return;

            _playerKills.Clear();
            MatchTimeRemaining.Value = TimeLimit;
            MatchInProgress.Value = true;
            Debug.Log("[FFAMode] Match Started!");
        }

        public override void OnPlayerKilled(ulong killerClientId, ulong victimClientId)
        {
            if (!IsServer || !MatchInProgress.Value) return;

            // Track/increment kills for the killer
            if (!_playerKills.ContainsKey(killerClientId))
            {
                _playerKills[killerClientId] = 0;
            }

            _playerKills[killerClientId]++;
            int killerKills = _playerKills[killerClientId];

            // Replicate score change to all clients
            SyncPlayerKillsClientRpc(killerClientId, killerKills);

            Debug.Log($"[FFAMode] Player {killerClientId} killed Player {victimClientId}. Total Kills: {killerKills}");

            // Win condition check
            if (killerKills >= KillLimit)
            {
                OnMatchEnd();
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
            
            // Find winner with highest kills
            ulong winnerId = 0;
            int maxKills = -1;
            foreach (var kvp in _playerKills)
            {
                if (kvp.Value > maxKills)
                {
                    maxKills = kvp.Value;
                    winnerId = kvp.Key;
                }
            }

            string winnerInfo = maxKills > 0 ? $"Player {winnerId} with {maxKills} kills" : "No one (Draw)";
            Debug.Log($"[FFAMode] Match Ended! Winner: {winnerInfo}");

            BroadcastMatchEndClientRpc(winnerInfo);
        }

        [ClientRpc]
        private void SyncPlayerKillsClientRpc(ulong clientId, int kills)
        {
            _playerKills[clientId] = kills;
            Debug.Log($"[FFA Client] Sync: Player {clientId} has {kills} kills");
        }

        public int GetPlayerKills(ulong clientId)
        {
            return _playerKills.TryGetValue(clientId, out int kills) ? kills : 0;
        }
    }
}
