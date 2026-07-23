using System.Collections.Generic;
using UnityEngine;

namespace Shootopia
{
    public class PlayerNameRegistry : MonoBehaviour
    {
        public static PlayerNameRegistry Instance { get; private set; }

        private readonly Dictionary<ulong, string> _names = new Dictionary<ulong, string>();

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

        public static void Register(ulong clientId, string displayName)
        {
            if (Instance == null)
            {
                var go = new GameObject("PlayerNameRegistry");
                Instance = go.AddComponent<PlayerNameRegistry>();
            }

            Instance._names[clientId] = displayName;
            Debug.Log("[PlayerNameRegistry] Registered: " + clientId + " -> " + displayName);
        }

        public static void Unregister(ulong clientId)
        {
            if (Instance != null && Instance._names.ContainsKey(clientId))
            {
                Instance._names.Remove(clientId);
                Debug.Log("[PlayerNameRegistry] Unregistered: " + clientId);
            }
        }

        public static string GetName(ulong clientId)
        {
            if (Instance != null && Instance._names.TryGetValue(clientId, out string name))
            {
                return name;
            }
            return "Player " + clientId;
        }
    }
}
