using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Multiplayer;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace Shootopia
{
    public partial class SessionManager
    {
        public async Task CreateSession(string sessionName, bool isPublic, int maxPlayers, string gameMode, string mapName = "Map1", int matchDurationSeconds = 300, int scoreLimit = 10)
        {
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
                bool authReady = await MultiplayerAutoInitializer.EnsureInitializedAsync();
                if (!authReady || !MultiplayerAutoInitializer.IsAuthenticated)
                    throw new Exception("Unable to connect to online game services. Please check your internet connection.");

                EnsureConnectionApprovalSetup();
                ConfigureTransportForPlatform();

                var options = new SessionOptions
                {
                    Name = sessionName,
                    MaxPlayers = maxPlayers,
                    IsPrivate = !isPublic,
                    Type = "default-session"
                };

                options.SessionProperties = new Dictionary<string, SessionProperty>
                {
                    { "GameMode", new SessionProperty(gameMode, VisibilityPropertyOptions.Public, PropertyIndex.String1) },
                    { "MapName", new SessionProperty(mapName, VisibilityPropertyOptions.Public, PropertyIndex.String2) },
                    { "State", new SessionProperty("Lobby", VisibilityPropertyOptions.Public, PropertyIndex.String3) },
                    { "MatchDuration", new SessionProperty(ConfiguredMatchDuration.ToString(), VisibilityPropertyOptions.Public, PropertyIndex.String4) },
                    { "ScoreLimit", new SessionProperty(ConfiguredScoreLimit.ToString(), VisibilityPropertyOptions.Public, PropertyIndex.String5) }
                };

                ApplyRelayRegionOptions(options);

                _currentSession = await MultiplayerService.Instance.CreateSessionAsync(options);
                SubscribeToSessionEvents(_currentSession);

                string myName = PlayerProfileManager.Instance != null ? PlayerProfileManager.Instance.DisplayName : "Agent";
                if (_currentSession.CurrentPlayer != null)
                {
                    _currentSession.CurrentPlayer.SetProperty("DisplayName", new PlayerProperty(myName, VisibilityPropertyOptions.Public));
                    _currentSession.CurrentPlayer.SetProperty("IsReady", new PlayerProperty("true", VisibilityPropertyOptions.Public));
                    await _currentSession.SaveCurrentPlayerDataAsync();
                }

                OnSessionCreated?.Invoke();

                string targetMap = !string.IsNullOrEmpty(mapName) ? mapName : GetSessionMapName();
                if (NetworkManager.Singleton?.IsServer == true && NetworkManager.Singleton.SceneManager != null)
                {
                    NetworkManager.Singleton.SceneManager.LoadScene(targetMap, LoadSceneMode.Single);
                }
                else
                {
                    LoadingScreenManager.LoadScene(targetMap, "WARPING TO ARENA...");
                }
            }
            catch (Exception ex)
            {
                string errMsg = $"CreateSession failed: {ex.Message}";
                Debug.LogError($"[SessionManager] {errMsg}");
                OnSessionError?.Invoke(errMsg);
                throw;
            }
            finally
            {
                _isProcessingSession = false;
            }
        }

        private void ApplyRelayRegionOptions(SessionOptions options)
        {
            string preferredRegion = PlayerPrefs.GetString("Shootopia_PreferredRegion", "in");
            if (string.IsNullOrEmpty(preferredRegion) || preferredRegion.Equals("auto", StringComparison.OrdinalIgnoreCase))
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                // In WebGL, browsers cannot ping Relay datacenters; default to asia-south1 (Mumbai) to prevent routing to US-East
                options.WithRelayNetwork("asia-south1");
                return;
#else
                options.WithRelayNetwork();
                return;
#endif
            }

            string targetRegion = preferredRegion.ToLower() switch
            {
                "in" => "asia-south1",
                "asia" => "asia-southeast1",
                "eu" => "europe-central2",
                "us" => "us-east1",
                _ => "asia-south1"
            };

            options.WithRelayNetwork(targetRegion);
        }

        public async Task JoinByCode(string joinCode)
        {
            if (_isProcessingSession) return;
            _isProcessingSession = true;

            try
            {
                await EnsurePreviousSessionCleanedUpAsync();
                await MultiplayerAutoInitializer.EnsureInitializedAsync();
                EnsureConnectionApprovalSetup();
                ConfigureTransportForPlatform();

                _currentSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(joinCode);
                SubscribeToSessionEvents(_currentSession);

                string myName = PlayerProfileManager.Instance != null ? PlayerProfileManager.Instance.DisplayName : "Agent";
                if (_currentSession.CurrentPlayer != null)
                {
                    _currentSession.CurrentPlayer.SetProperty("DisplayName", new PlayerProperty(myName, VisibilityPropertyOptions.Public));
                    _currentSession.CurrentPlayer.SetProperty("IsReady", new PlayerProperty("false", VisibilityPropertyOptions.Public));
                    await _currentSession.SaveCurrentPlayerDataAsync();
                }

                OnSessionJoined?.Invoke();
            }
            catch (Exception ex)
            {
                string errMsg = $"JoinByCode failed: {ex.Message}";
                Debug.LogError($"[SessionManager] {errMsg}");
                OnSessionError?.Invoke(errMsg);
                throw;
            }
            finally
            {
                _isProcessingSession = false;
            }
        }

        public async Task JoinById(string sessionId)
        {
            if (_isProcessingSession) return;
            _isProcessingSession = true;

            try
            {
                await EnsurePreviousSessionCleanedUpAsync();
                await MultiplayerAutoInitializer.EnsureInitializedAsync();
                EnsureConnectionApprovalSetup();
                ConfigureTransportForPlatform();

                _currentSession = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId);
                SubscribeToSessionEvents(_currentSession);

                string myName = PlayerProfileManager.Instance != null ? PlayerProfileManager.Instance.DisplayName : "Agent";
                if (_currentSession.CurrentPlayer != null)
                {
                    _currentSession.CurrentPlayer.SetProperty("DisplayName", new PlayerProperty(myName, VisibilityPropertyOptions.Public));
                    _currentSession.CurrentPlayer.SetProperty("IsReady", new PlayerProperty("false", VisibilityPropertyOptions.Public));
                    await _currentSession.SaveCurrentPlayerDataAsync();
                }

                OnSessionJoined?.Invoke();
            }
            catch (Exception ex)
            {
                string errMsg = $"JoinById failed: {ex.Message}";
                Debug.LogError($"[SessionManager] {errMsg}");
                OnSessionError?.Invoke(errMsg);
                throw;
            }
            finally
            {
                _isProcessingSession = false;
            }
        }

        public async Task<List<ISessionInfo>> GetPublicSessions()
        {
            try
            {
                await MultiplayerAutoInitializer.EnsureInitializedAsync();
                var options = new QuerySessionsOptions();
                var results = await MultiplayerService.Instance.QuerySessionsAsync(options);
                return results?.Sessions != null ? new List<ISessionInfo>(results.Sessions) : new List<ISessionInfo>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SessionManager] GetPublicSessions failed: {ex.Message}");
                return new List<ISessionInfo>();
            }
        }
    }
}
