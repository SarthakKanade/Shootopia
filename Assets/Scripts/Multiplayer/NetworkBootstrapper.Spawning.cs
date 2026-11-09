using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace Shootopia
{
    public partial class NetworkBootstrapper
    {
public void SpawnAllConnectedPlayers(bool cleanRespawn = false)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                try
                {
                    SpawnPlayerForClient(client.ClientId, cleanRespawn);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[NetworkBootstrapper] Error in SpawnPlayerForClient({client.ClientId}): {ex.Message}");
                }
            }
        }

        public void SpawnPlayerForClient(ulong clientId, bool cleanRespawn = false)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

            string currentSceneName = SceneManager.GetActiveScene().name;
            if (!IsValidSpawnScene(currentSceneName)) return;

            // Resolve spawn position with safe platform fallback (offset per clientId so fallback never overlaps players)
            Vector3 spawnPos = new Vector3((clientId % 4) * 3f, 0.5f, ((clientId / 4) % 4) * 3f);
            Quaternion spawnRot = Quaternion.identity;

            if (SpawnPointManager.Instance != null)
            {
                SpawnPointType spawnType = SpawnPointType.FFA;
                if (GameModeBase.Instance is TDMMode tdm)
                {
                    int team = tdm.GetPlayerTeam(clientId);
                    if (team == 0) spawnType = SpawnPointType.TeamBlue;
                    else if (team == 1) spawnType = SpawnPointType.TeamRed;
                }

                Transform sp = SpawnPointManager.Instance.GetSpawnPoint(spawnType);
                if (sp != null)
                {
                    spawnPos = sp.position;
                    spawnRot = sp.rotation;
                }
            }
            else if (currentSceneName == "WaitingRoom")
            {
                spawnPos = new Vector3(15f, 0.5f, 15f);
            }

            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                if (client.PlayerObject != null)
                {
                    bool sceneMismatch = client.PlayerObject.gameObject.scene != SceneManager.GetActiveScene();
                    bool notSpawned = !client.PlayerObject.IsSpawned;

                    if (cleanRespawn || sceneMismatch || notSpawned)
                    {
                        Debug.Log($"[NetworkBootstrapper] Cleaning up existing player object for client {clientId} (cleanRespawn={cleanRespawn}, sceneMismatch={sceneMismatch}, notSpawned={notSpawned}).");
                        try
                        {
                            if (client.PlayerObject.IsSpawned)
                            {
                                client.PlayerObject.Despawn(true);
                            }
                            else
                            {
                                Destroy(client.PlayerObject.gameObject);
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogWarning($"[NetworkBootstrapper] Exception while despawning old player object for client {clientId}: {ex.Message}");
                        }
                    }
                    else
                    {
                        Debug.Log($"[NetworkBootstrapper] Client {clientId} has spawned player. Repositioning to {spawnPos} in '{currentSceneName}'.");
                        var health = client.PlayerObject.GetComponent<PlayerHealth>();
                        if (health != null)
                        {
                            try
                            {
                                health.ResetStateForMatch(spawnPos, spawnRot);
                            }
                            catch (System.Exception ex)
                            {
                                Debug.LogWarning($"[NetworkBootstrapper] Error in ResetStateForMatch for client {clientId}: {ex.Message}");
                            }
                        }
                        else
                        {
                            var cc = client.PlayerObject.GetComponent<CharacterController>();
                            if (cc != null) cc.enabled = false;
                            client.PlayerObject.transform.position = spawnPos;
                            client.PlayerObject.transform.rotation = spawnRot;
                            if (cc != null) cc.enabled = true;
                        }
                        return;
                    }
                }
            }

            var playerPrefab = NetworkManager.Singleton.NetworkConfig.PlayerPrefab;
            if (playerPrefab == null)
            {
                Debug.LogError("[NetworkBootstrapper] PlayerPrefab is not assigned in NetworkManager NetworkConfig!");
                return;
            }

            var playerInstance = Instantiate(playerPrefab, spawnPos, spawnRot);
            var netObj = playerInstance.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.SpawnAsPlayerObject(clientId, true);
                Debug.Log($"[NetworkBootstrapper] Successfully spawned fresh player for client {clientId} at {spawnPos} in scene '{currentSceneName}'.");
            }
            else
            {
                Debug.LogError("[NetworkBootstrapper] Instantiated player prefab is missing a NetworkObject component!");
                return;
            }

            AssignPlayerDisplayName(clientId, playerInstance);
        }

        private void AssignPlayerDisplayName(ulong clientId, GameObject playerObject)
        {
            string displayName = ResolveDisplayName(clientId);
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = PlayerNameRegistry.GetName(clientId);
            }
            if (string.IsNullOrEmpty(displayName) || displayName == $"Player {clientId}")
            {
                if (NetworkManager.Singleton != null && clientId == NetworkManager.Singleton.LocalClientId && PlayerProfileManager.Instance != null)
                {
                    displayName = PlayerProfileManager.Instance.DisplayName;
                }
                else
                {
                    displayName = $"Agent_{clientId}";
                }
            }

            var nameDisplay = playerObject.GetComponent<PlayerNameDisplay>();
            if (nameDisplay != null)
            {
                nameDisplay.DisplayName.Value = displayName;
            }

            PlayerNameRegistry.Register(clientId, displayName);
            Debug.Log($"[NetworkBootstrapper] Assigned display name '{displayName}' to client {clientId}.");
        }

        private string ResolveDisplayName(ulong clientId)
        {
            if (SessionManager.Instance == null) return null;
            var session = SessionManager.Instance.GetCurrentSession();
            if (session == null || session.Players == null) return null;

            int clientIdx = 0;
            for (int i = 0; i < NetworkManager.Singleton.ConnectedClientsList.Count; i++)
            {
                if (NetworkManager.Singleton.ConnectedClientsList[i].ClientId == clientId)
                {
                    clientIdx = i;
                    break;
                }
            }

            int pIdx = 0;
            foreach (var player in session.Players)
            {
                if (player == null) continue;
                if (pIdx == clientIdx)
                {
                    if (player.Properties != null && player.Properties.TryGetValue("DisplayName", out var prop) && !string.IsNullOrEmpty(prop.Value))
                    {
                        return prop.Value;
                    }
                    return $"Agent_{clientId}";
                }
                pIdx++;
            }

            return null;
        }
    }
}
