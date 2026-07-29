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

#if UNITY_EDITOR
                var playerPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab");
                if (playerPrefab != null)
                {
                    var networkPrefab = new NetworkPrefab { Prefab = playerPrefab };
                    config.Prefabs.Add(networkPrefab);
                    config.PlayerPrefab = playerPrefab;
                    Debug.Log("[AutoNetworkSandboxHost] Registered PlayerPrefab for Sandbox host testing.");
                }
#endif

                netManager.NetworkConfig = config;
                Debug.Log("[AutoNetworkSandboxHost] Created NetworkManager for Sandbox testing.");
            }
        }
    }
}
