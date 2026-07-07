using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Shootopia
{
    [System.Serializable]
    public struct PlayerScoreEntry : INetworkSerializable
    {
        public ulong ClientId;
        public int Kills;
        public int Deaths;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref Kills);
            serializer.SerializeValue(ref Deaths);
        }
    }

    /// <summary>
    /// Abstract base class for all networked game modes in Shootopia.
    /// Manages high-level match state (timer, active state, RPC broadcasts) on the server.
    /// </summary>
    public abstract class GameModeBase : NetworkBehaviour
    {
        public static GameModeBase Instance { get; protected set; }
        public static event System.Action<string, PlayerScoreEntry[]> OnMatchEndedWithScoreboard;

        [Header("Match Lifecycle Variables")]
        public readonly NetworkVariable<bool> MatchActive = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public readonly NetworkVariable<int> PreMatchCountdown = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        [Header("Match State")]
        protected readonly NetworkVariable<bool> MatchInProgress = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        protected float MatchTimeRemainingValue;

        /// <summary>
        /// Expose match variables to external systems (e.g., UI displays).
        /// </summary>
        public float TimeRemaining => MatchTimeRemainingValue;
        public bool IsInProgress => MatchInProgress.Value;

        protected readonly Dictionary<ulong, int> PlayerKills = new Dictionary<ulong, int>();
        protected readonly Dictionary<ulong, int> PlayerDeaths = new Dictionary<ulong, int>();

        protected virtual void Awake()
        {
            Instance = this;
        }

        protected virtual void OnEnable()
        {
            Instance = this;
        }

        public abstract void OnMatchStart();
        
        public virtual void OnPlayerKilled(ulong killerClientId, ulong victimClientId)
        {
            if (!IsServer) return;

            if (!PlayerKills.ContainsKey(killerClientId)) PlayerKills[killerClientId] = 0;
            PlayerKills[killerClientId]++;

            if (!PlayerDeaths.ContainsKey(victimClientId)) PlayerDeaths[victimClientId] = 0;
            PlayerDeaths[victimClientId]++;
        }

        public virtual void OnMatchEnd()
        {
            if (!IsServer) return;
            MatchInProgress.Value = false;
            MatchActive.Value = false;
        }

        private float _timeSinceLastTimeBroadcast = 0f;

        protected virtual void Update()
        {
            if (!IsServer || !MatchInProgress.Value || !MatchActive.Value) return;

            MatchTimeRemainingValue -= Time.deltaTime;
            if (MatchTimeRemainingValue <= 0f)
            {
                MatchTimeRemainingValue = 0f;
                OnMatchEnd();
            }

            _timeSinceLastTimeBroadcast += Time.deltaTime;
            if (_timeSinceLastTimeBroadcast >= 1.0f)
            {
                _timeSinceLastTimeBroadcast = 0f;
                BroadcastTimeRemainingClientRpc(MatchTimeRemainingValue);
            }
        }

        [ClientRpc]
        public void BroadcastTimeRemainingClientRpc(float timeRemaining)
        {
            MatchTimeRemainingValue = timeRemaining;
        }

        [ClientRpc]
        public void BroadcastKillFeedClientRpc(string killerName, string victimName, string weaponName)
        {
            Debug.Log($"[KillFeed] {killerName} killed {victimName} with {weaponName}");
        }

        [ClientRpc]
        public void BroadcastMatchEndClientRpc(string winnerInfo, PlayerScoreEntry[] scores)
        {
            Debug.Log($"[MatchEnd] Winner: {winnerInfo}");
            OnMatchEndedWithScoreboard?.Invoke(winnerInfo, scores);
        }

        protected PlayerScoreEntry[] CompileScoreboard()
        {
            var entries = new List<PlayerScoreEntry>();
            if (NetworkManager.Singleton != null)
            {
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    ulong id = client.ClientId;
                    int kills = PlayerKills.TryGetValue(id, out var k) ? k : 0;
                    int deaths = PlayerDeaths.TryGetValue(id, out var d) ? d : 0;
                    entries.Add(new PlayerScoreEntry { ClientId = id, Kills = kills, Deaths = deaths });
                }
            }
            return entries.ToArray();
        }
    }
}
