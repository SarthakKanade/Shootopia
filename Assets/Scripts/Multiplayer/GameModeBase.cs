using Unity.Netcode;
using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// Abstract base class for all networked game modes in Shootopia.
    /// Manages high-level match state (timer, active state, RPC broadcasts) on the server.
    /// </summary>
    public abstract class GameModeBase : NetworkBehaviour
    {
        [Header("Match State")]
        protected readonly NetworkVariable<float> MatchTimeRemaining = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        protected readonly NetworkVariable<bool> MatchInProgress = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        /// <summary>
        /// Expose match variables to external systems (e.g., UI displays).
        /// </summary>
        public float TimeRemaining => MatchTimeRemaining.Value;
        public bool IsInProgress => MatchInProgress.Value;

        public abstract void OnMatchStart();
        public abstract void OnPlayerKilled(ulong killerClientId, ulong victimClientId);
        public abstract void OnMatchEnd();

        [ClientRpc]
        public void BroadcastKillFeedClientRpc(string killerName, string victimName, string weaponName)
        {
            Debug.Log($"[KillFeed] {killerName} killed {victimName} with {weaponName}");
        }

        [ClientRpc]
        public void BroadcastMatchEndClientRpc(string winnerInfo)
        {
            Debug.Log($"[MatchEnd] Winner: {winnerInfo}");
        }
    }
}
