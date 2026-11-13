using System.Collections.Generic;
using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// Central thread-safe registry mapping NGO Client IDs and UGS Player IDs
    /// to clean, verified display names. Keeps a historical cache so disconnected
    /// player names are never lost or replaced with raw IDs.
    /// </summary>
    public class PlayerNameRegistry : MonoBehaviour
    {
        public static PlayerNameRegistry Instance { get; private set; }

        private readonly Dictionary<ulong, string> _names = new Dictionary<ulong, string>();
        private readonly Dictionary<ulong, string> _lastKnownNames = new Dictionary<ulong, string>();
        private readonly Dictionary<string, ulong> _playerIdsToClientId = new Dictionary<string, ulong>();
        private readonly Dictionary<ulong, string> _clientIdsToPlayerId = new Dictionary<ulong, string>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public static void Register(ulong clientId, string displayName, string ugsPlayerId = null)
        {
            if (Instance == null)
            {
                var go = new GameObject("PlayerNameRegistry");
                Instance = go.AddComponent<PlayerNameRegistry>();
            }

            if (!string.IsNullOrWhiteSpace(displayName))
            {
                string clean = displayName.Trim();
                Instance._names[clientId] = clean;
                Instance._lastKnownNames[clientId] = clean;
                Debug.Log($"[PlayerNameRegistry] Registered: ClientId {clientId} -> '{clean}'");
            }

            if (!string.IsNullOrWhiteSpace(ugsPlayerId))
            {
                Instance._playerIdsToClientId[ugsPlayerId] = clientId;
                Instance._clientIdsToPlayerId[clientId] = ugsPlayerId;
            }
        }

        public static void Unregister(ulong clientId)
        {
            if (Instance != null && Instance._names.ContainsKey(clientId))
            {
                Instance._names.Remove(clientId);
                Debug.Log($"[PlayerNameRegistry] Unregistered active client: {clientId} (cached as '{GetLastKnownName(clientId)}')");
            }
        }

        public static string GetName(ulong clientId)
        {
            if (Instance != null && Instance._names.TryGetValue(clientId, out string name) && !string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            if (Unity.Netcode.NetworkManager.Singleton != null && 
                Unity.Netcode.NetworkManager.Singleton.LocalClientId == clientId && 
                PlayerProfileManager.Instance != null && 
                !string.IsNullOrWhiteSpace(PlayerProfileManager.Instance.DisplayName))
            {
                return PlayerProfileManager.Instance.DisplayName;
            }

            if (Instance != null && Instance._lastKnownNames.TryGetValue(clientId, out string cached) && !string.IsNullOrWhiteSpace(cached))
            {
                return cached;
            }

            return "Player";
        }

        public static string GetLastKnownName(ulong clientId)
        {
            if (Instance != null && Instance._lastKnownNames.TryGetValue(clientId, out string cached) && !string.IsNullOrWhiteSpace(cached))
            {
                return cached;
            }

            return GetName(clientId);
        }

        public static bool TryGetClientIdByPlayerId(string ugsPlayerId, out ulong clientId)
        {
            clientId = 0;
            if (string.IsNullOrEmpty(ugsPlayerId) || Instance == null) return false;
            return Instance._playerIdsToClientId.TryGetValue(ugsPlayerId, out clientId);
        }

        public static bool TryGetPlayerIdByClientId(ulong clientId, out string ugsPlayerId)
        {
            ugsPlayerId = null;
            if (Instance == null) return false;
            return Instance._clientIdsToPlayerId.TryGetValue(clientId, out ugsPlayerId);
        }

        public static bool TryGetNameByPlayerId(string ugsPlayerId, out string displayName)
        {
            displayName = null;
            if (TryGetClientIdByPlayerId(ugsPlayerId, out ulong clientId))
            {
                displayName = GetName(clientId);
                return !string.IsNullOrWhiteSpace(displayName);
            }
            return false;
        }
    }
}
