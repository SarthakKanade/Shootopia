using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace Shootopia
{
    /// <summary>
    /// Auto-starts Netcode as Host when running Sandbox/Testing scenes directly in the Unity Editor.
    /// Configures NetworkManager with the PlayerPrefab so input, ownership, and weapons function properly.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class AutoNetworkSandboxHost : MonoBehaviour
    {
        [SerializeField] private GameObject playerPrefab;

        private void Awake()
        {
            EnsureNetworkManager();
        }

        private void Start()
        {
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
            {
                Debug.Log("[AutoNetworkSandboxHost] Starting Netcode Host for Sandbox testing...");
                bool success = NetworkManager.Singleton.StartHost();
                if (success)
                {
                    Debug.Log("[AutoNetworkSandboxHost] Host started successfully in Sandbox scene!");
                }
                else
                {
                    Debug.LogError("[AutoNetworkSandboxHost] Failed to start Host in Sandbox scene!");
                }
            }
        }

        private void EnsureNetworkManager()
        {
            if (NetworkManager.Singleton == null)
            {
                var netManagerObj = new GameObject("NetworkManager");
                var netManager = netManagerObj.AddComponent<NetworkManager>();
                netManagerObj.AddComponent<UnityTransport>();

                // Configure NetworkManager transport
                var config = new NetworkConfig
                {
                    NetworkTransport = netManagerObj.GetComponent<UnityTransport>(),
                    EnableSceneManagement = true
                };

                var prefab = playerPrefab != null ? playerPrefab : Resources.Load<GameObject>("Player");
                if (prefab != null)
                {
                    var networkPrefab = new NetworkPrefab { Prefab = prefab };
                    config.Prefabs.Add(networkPrefab);
                    config.PlayerPrefab = prefab;
                    Debug.Log("[AutoNetworkSandboxHost] Registered PlayerPrefab for Sandbox host testing.");
                }

                netManager.NetworkConfig = config;
                Debug.Log("[AutoNetworkSandboxHost] Created NetworkManager for Sandbox testing.");
            }
        }
    }
}
