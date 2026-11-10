using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;

namespace Shootopia
{
    public partial class SessionManager
    {
        private static string GenerateRoomCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var random = new System.Random();
            char[] code = new char[6];
            for (int i = 0; i < 6; i++) code[i] = chars[random.Next(chars.Length)];
            return new string(code);
        }

        public async Task StartLanSession(string sessionName, int maxPlayers, string gameMode, string mapName = "Map1", int matchDurationSeconds = 300, int scoreLimit = 10)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            throw new InvalidOperationException("LAN matches are not supported in web browsers. Please create an Online Arena using a Room Code.");
#endif
            if (_isProcessingSession) return;
            _isProcessingSession = true;

            ConfiguredMatchDuration = matchDurationSeconds > 0 ? matchDurationSeconds : 300;
            ConfiguredScoreLimit = scoreLimit > 0 ? scoreLimit : 10;
            string normMode = (gameMode != null && gameMode.ToUpper().Contains("TDM")) ? "TDM" : "FFA";
            ConfiguredGameMode = normMode;
            PlayerPrefs.SetString("Shootopia_ConfiguredGameMode", normMode);
            PlayerPrefs.Save();

            try
            {
                await EnsurePreviousSessionCleanedUpAsync();
                EnsureConnectionApprovalSetup();

                string roomCode = GenerateRoomCode();
                CurrentRoomCode = roomCode;
                IsLanSession = true;

                PlayerPrefs.SetString("Shootopia_CurrentRoomCode", roomCode);
                PlayerPrefs.SetInt("Shootopia_IsLanSession", 1);
                PlayerPrefs.Save();

                if (NetworkManager.Singleton != null)
                {
                    var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                    if (transport != null)
                    {
                        transport.UseWebSockets = false;
                        transport.SetConnectionData("127.0.0.1", 7777, "0.0.0.0");
                    }

                    if (GetComponent<LagCompensationManager>() == null) gameObject.AddComponent<LagCompensationManager>();

                    string hostName = PlayerProfileManager.Instance != null ? PlayerProfileManager.Instance.DisplayName : "Host";
                    PlayerNameRegistry.Register(NetworkManager.ServerClientId, hostName);

                    if (!NetworkManager.Singleton.StartHost()) throw new Exception("NetworkManager failed to start LAN Host.");
                }

                LanDiscoveryManager.StartBroadcasting(roomCode, 7777, sessionName, mapName, gameMode, maxPlayers);
                OnSessionCreated?.Invoke();

                string targetMap = !string.IsNullOrEmpty(mapName) ? mapName : "Map1";
                if (NetworkManager.Singleton?.IsServer == true && NetworkManager.Singleton.SceneManager != null)
                    NetworkManager.Singleton.SceneManager.LoadScene(targetMap, LoadSceneMode.Single);
                else
                    LoadingScreenManager.LoadScene(targetMap, "DEPLOYING TO ARENA...");
            }
            catch (Exception ex)
            {
                string errMsg = $"StartLanSession failed: {ex.Message}";
                Debug.LogError($"[SessionManager] {errMsg}");
                OnSessionError?.Invoke(errMsg);
                throw;
            }
            finally
            {
                _isProcessingSession = false;
            }
        }

        public async Task JoinLanSessionByCode(string joinCode)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            throw new InvalidOperationException("LAN matches are not supported in web browsers. Please connect via Online Room Code.");
#endif
            if (_isProcessingSession) return;
            _isProcessingSession = true;

            string cleanCode = joinCode != null ? joinCode.Trim().ToUpper() : "";
            LoadingScreenManager.ShowOverlay($"SCANNING WI-FI FOR [{cleanCode}]...");

            try
            {
                await EnsurePreviousSessionCleanedUpAsync();
                LanSessionInfo lanInfo = await LanDiscoveryManager.LocateHostByCodeAsync(cleanCode, 5.0f);

                if (lanInfo == null) throw new Exception($"Could not locate room [{cleanCode}] on your local Wi-Fi.");

                EnsureConnectionApprovalSetup();

                if (NetworkManager.Singleton != null)
                {
                    var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                    if (transport != null)
                    {
                        transport.UseWebSockets = false;
                        transport.SetConnectionData(lanInfo.HostIP, lanInfo.HostPort);
                    }

                    string clientName = PlayerProfileManager.Instance != null ? PlayerProfileManager.Instance.DisplayName : "Agent";
                    NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.UTF8.GetBytes(clientName);

                    if (!NetworkManager.Singleton.StartClient()) throw new Exception("NetworkManager failed to connect to LAN Host.");
                }

                IsLanSession = true;
                CurrentRoomCode = cleanCode;
                if (!string.IsNullOrEmpty(lanInfo.GameMode))
                {
                    string normMode = lanInfo.GameMode.ToUpper().Contains("TDM") ? "TDM" : "FFA";
                    ConfiguredGameMode = normMode;
                    PlayerPrefs.SetString("Shootopia_ConfiguredGameMode", normMode);
                }

                PlayerPrefs.SetString("Shootopia_CurrentRoomCode", cleanCode); PlayerPrefs.SetInt("Shootopia_IsLanSession", 1); PlayerPrefs.Save();

                OnSessionJoined?.Invoke();
            }
            catch (Exception ex)
            {
                string errMsg = $"JoinLanSession failed: {ex.Message}";
                Debug.LogError($"[SessionManager] {errMsg}");
                LoadingScreenManager.HideOverlay();
                OnSessionError?.Invoke(errMsg);
                throw;
            }
            finally
            {
                _isProcessingSession = false;
            }
        }
    }
}

