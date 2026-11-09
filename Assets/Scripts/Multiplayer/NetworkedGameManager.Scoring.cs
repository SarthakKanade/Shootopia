using UnityEngine;

namespace Shootopia
{
    public partial class NetworkedGameManager
    {
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
            var mode = _activeGameMode ?? GameModeBase.Instance;
            if (!IsServer || mode == null) return;

            mode.OnPlayerKilled(killerClientId, victimClientId);

            string killerName = ResolvePlayerDisplayName(killerClientId);
            string victimName = ResolvePlayerDisplayName(victimClientId);

            if (killerClientId == victimClientId)
            {
                if (weaponName == "Lava" || weaponName == "Environment") killerName = weaponName;
                else victimName = "Themselves";
            }

            mode.BroadcastKillFeedClientRpc(killerName, victimName, weaponName, killerClientId, victimClientId);
        }

        private string ResolvePlayerDisplayName(ulong clientId)
        {
            return PlayerNameRegistry.GetName(clientId);
        }

        private void OnMatchEndedScoreboard(string winnerInfo, PlayerScoreEntry[] scores)
        {
            if (IsServer) MatchStarted.Value = false;
        }
    }
}

