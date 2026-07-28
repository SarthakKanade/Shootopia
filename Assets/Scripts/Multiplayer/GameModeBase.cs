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
        public static event System.Action<string, string, string, ulong, ulong> OnKillFeedReceived;
        public static event System.Action<string> OnSystemMessageReceived;

        [Header("Match Lifecycle Variables")]
        public readonly NetworkVariable<bool> MatchActive = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public readonly NetworkVariable<int> PreMatchCountdown = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        [Header("Match State")]
        protected readonly NetworkVariable<bool> MatchInProgress = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public readonly NetworkVariable<float> MatchTimeRemaining = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        /// <summary>
        /// Expose match variables to external systems (e.g., UI displays).
        /// </summary>
        public float TimeRemaining => MatchTimeRemaining.Value;
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

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnBaseClientConnected;
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnBaseClientConnected;
            }
        }

        private void OnBaseClientConnected(ulong clientId)
        {
            if (!IsServer) return;
            // Sync all existing player score stats to joining clients
            foreach (var kvp in PlayerKills)
            {
                ulong id = kvp.Key;
                int kills = kvp.Value;
                int deaths = PlayerDeaths.TryGetValue(id, out var d) ? d : 0;
                SyncPlayerStatsClientRpc(id, kills, id, deaths);
            }
        }

        public abstract void OnMatchStart();
        
        public virtual void OnPlayerKilled(ulong killerClientId, ulong victimClientId)
        {
            if (!IsServer) return;

            if (!PlayerKills.ContainsKey(killerClientId)) PlayerKills[killerClientId] = 0;
            PlayerKills[killerClientId]++;

            if (!PlayerDeaths.ContainsKey(victimClientId)) PlayerDeaths[victimClientId] = 0;
            PlayerDeaths[victimClientId]++;

            SyncPlayerStatsClientRpc(killerClientId, PlayerKills[killerClientId], victimClientId, PlayerDeaths[victimClientId]);
        }

        [ClientRpc]
        protected void SyncPlayerStatsClientRpc(ulong killerId, int killerKills, ulong victimId, int victimDeaths)
        {
            PlayerKills[killerId] = killerKills;
            PlayerDeaths[victimId] = victimDeaths;
        }

        public virtual int GetPlayerKills(ulong clientId)
        {
            return PlayerKills.TryGetValue(clientId, out var k) ? k : 0;
        }

        public int GetPlayerDeaths(ulong clientId)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) && client.PlayerObject != null)
            {
                var health = client.PlayerObject.GetComponent<PlayerHealth>();
                if (health != null) return health.DeathCount.Value;
            }
            return PlayerDeaths.TryGetValue(clientId, out var d) ? d : 0;
        }

        public virtual void OnMatchEnd()
        {
            if (!IsServer) return;
            MatchInProgress.Value = false;
            MatchActive.Value = false;
        }

        protected virtual void Update()
        {
            if (!MatchInProgress.Value || !MatchActive.Value) return;

            if (IsServer)
            {
                MatchTimeRemaining.Value = Mathf.Max(0f, MatchTimeRemaining.Value - Time.deltaTime);
                if (MatchTimeRemaining.Value <= 0f)
                {
                    OnMatchEnd();
                }
            }
        }

        [ClientRpc]
        public void BroadcastTimeRemainingClientRpc(float timeRemaining)
        {
            if (IsServer)
            {
                MatchTimeRemaining.Value = timeRemaining;
            }
        }

        [ClientRpc]
        public void BroadcastKillFeedClientRpc(string killerName, string victimName, string weaponName, ulong killerClientId, ulong victimClientId)
        {
            Debug.Log($"[KillFeed] {killerName} killed {victimName} with {weaponName}");
            OnKillFeedReceived?.Invoke(killerName, victimName, weaponName, killerClientId, victimClientId);
        }

        [ClientRpc]
        public void BroadcastSystemMessageClientRpc(string message)
        {
            Debug.Log($"[SystemMessage] {message}");
            OnSystemMessageReceived?.Invoke(message);
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
                    int kills = GetPlayerKills(id);
                    int deaths = GetPlayerDeaths(id);
                    entries.Add(new PlayerScoreEntry { ClientId = id, Kills = kills, Deaths = deaths });
                }
            }
            return entries.ToArray();
        }
    }
}
