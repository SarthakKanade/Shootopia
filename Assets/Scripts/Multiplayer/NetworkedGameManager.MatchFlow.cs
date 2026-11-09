using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shootopia
{
    public partial class NetworkedGameManager
    {
        public void StartMatchFromLobby()
        {
            if (IsServer) StartMatchFromLobbyServer();
            else StartMatchFromLobbyServerRpc();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void StartMatchFromLobbyServerRpc(RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != NetworkManager.ServerClientId) return;
            StartMatchFromLobbyServer();
        }

        private void StartMatchFromLobbyServer()
        {
            if (!IsServer) return;

            if (SessionManager.Instance != null)
            {
                var readyStates = SessionManager.Instance.GetPlayerReadyStates();
                if (readyStates != null && readyStates.Count > 1)
                {
                    var session = SessionManager.Instance.GetCurrentSession();
                    string hostId = session != null ? session.Host : "";
                    foreach (var p in readyStates)
                    {
                        if (p.playerId != hostId && !p.isReady) return;
                    }
                }
            }

            MatchStarted.Value = true;
            CloseWaitingRoomRpc();
            BroadcastMatchStartedDirect();

            _ = SessionManager.Instance?.UpdateSessionStateAsync("InProgress");
            StartCoroutine(PreMatchSequence());
        }

        private void BroadcastMatchStartedDirect()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.CustomMessagingManager != null)
            {
                using (var writer = new FastBufferWriter(sizeof(bool), Unity.Collections.Allocator.Temp))
                {
                    writer.WriteValueSafe(true);
                    NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll("Shootopia_MatchStarted", writer);
                }
            }
        }

        [Rpc(SendTo.Everyone)]
        private void CloseWaitingRoomRpc() => CloseLobbyUI();

        public static void CloseLobbyUI()
        {
            if (HUDController.Instance != null) HUDController.Instance.ClearKillFeed();

            if (WaitingRoomController.Instance != null)
            {
                WaitingRoomController.Instance.OnMatchStarted();
            }
            else
            {
                var wrc = Object.FindAnyObjectByType<WaitingRoomController>();
                if (wrc != null) wrc.OnMatchStarted();
                else if (AudioManager.Instance != null) AudioManager.Instance.PlayMapMusic();
            }
        }

        private IEnumerator PreMatchSequence()
        {
            string targetMode = ActiveModeName.Value.ToString();
            if (string.IsNullOrEmpty(targetMode) && SessionManager.Instance != null)
                targetMode = SessionManager.Instance.GetSessionGameMode();

            ApplyActiveGameMode(targetMode);
            if (_activeGameMode == null) yield break;

            MatchStarted.Value = true;
            CloseWaitingRoomRpc();

            _activeGameMode.MatchActive.Value = false;
            _activeGameMode.PreMatchCountdown.Value = -1;

            float maxWait = 10f;
            float waitTimer = 0f;
            int expected = SessionManager.Instance?.GetCurrentSession()?.Players?.Count ?? 
                           (NetworkManager.Singleton != null ? NetworkManager.Singleton.ConnectedClientsList.Count : 1);

            while (waitTimer < maxWait)
            {
                int spawned = 0;
                if (NetworkManager.Singleton != null)
                {
                    foreach (var c in NetworkManager.Singleton.ConnectedClientsList)
                    {
                        if (c.PlayerObject != null && c.PlayerObject.IsSpawned) spawned++;
                    }
                }
                if (spawned >= expected && spawned > 0) break;
                yield return new WaitForSeconds(0.5f);
                waitTimer += 0.5f;
            }

            if (_activeGameMode is TDMMode tdm) tdm.InitializeTeamsBeforeSpawn();

            if (NetworkBootstrapper.Instance != null) NetworkBootstrapper.Instance.SpawnAllConnectedPlayers();
            if (LootBoxSpawnManager.Instance != null) LootBoxSpawnManager.Instance.CheckAndAdjustChestsForPlayerCount();

            int countdown = 3;
            while (countdown > 0)
            {
                if (_activeGameMode != null) _activeGameMode.PreMatchCountdown.Value = countdown;
                yield return new WaitForSeconds(1.0f);
                countdown--;
            }

            if (_activeGameMode != null)
            {
                _activeGameMode.PreMatchCountdown.Value = 0;
                _activeGameMode.MatchActive.Value = true;
            }

            CloseWaitingRoomRpc();
            CleanupWarmupEntities();

            if (_activeGameMode != null) _activeGameMode.OnMatchStart();
            GrantMatchStartInvincibility();
        }

        private void GrantMatchStartInvincibility()
        {
            if (!IsServer) return;
            foreach (var p in PlayerHealth.ActivePlayers)
            {
                if (p != null && p.IsSpawned) p.StartInvincibilityServer(p.SpawnInvincibilityDuration);
            }
        }

        private void CleanupWarmupEntities()
        {
            foreach (var t in Object.FindObjectsByType<TrapBase>(FindObjectsSortMode.None)) if (t != null) Object.Destroy(t.gameObject);
            foreach (var s in Object.FindObjectsByType<SmokeCloud>(FindObjectsSortMode.None)) if (s != null) Object.Destroy(s.gameObject);
            foreach (var v in Object.FindObjectsByType<VoidZone>(FindObjectsSortMode.None)) if (v != null) Object.Destroy(v.gameObject);
            foreach (var p in Object.FindObjectsByType<KinematicProjectileController>(FindObjectsSortMode.None)) if (p != null) Object.Destroy(p.gameObject);
            foreach (var b in Object.FindObjectsByType<BaseProjectile>(FindObjectsSortMode.None)) if (b != null) Object.Destroy(b.gameObject);

            if (LootBoxSpawnManager.Instance != null) LootBoxSpawnManager.Instance.ResetAllChestsForMatch();
            if (NetworkBootstrapper.Instance != null) NetworkBootstrapper.Instance.SpawnAllConnectedPlayers();
        }

        public void RequestRematch()
        {
            if (IsServer) RestartMatchServer();
            else RequestRematchServerRpc();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestRematchServerRpc() => RestartMatchServer();

        private void RestartMatchServer()
        {
            if (!IsServer) return;
            string currentMap = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.LoadScene(currentMap, UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(currentMap);
            }
        }
    }
}

