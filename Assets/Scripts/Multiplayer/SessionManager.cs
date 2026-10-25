using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Unity.Services.Multiplayer;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;

namespace Shootopia
{
    /// <summary>
    /// Streamlined central manager for multiplayer sessions (UGS + NGO + Relay).
    /// Handles session creation, joining, listing, ready states, connection approval, and scene routing.
    /// </summary>
    public class SessionManager : MonoBehaviour
    {
        private static SessionManager _instance;
        public static SessionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<SessionManager>();
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Events")]
        public UnityEvent OnSessionCreated;
        public UnityEvent OnSessionJoined;
        public UnityEvent OnSessionLeft;
        public UnityEvent<string> OnSessionError;
        public UnityEvent OnPlayerReadyChanged;
        public UnityEvent OnSessionUpdated;

        public bool IsHost => _currentSession != null && _currentSession.IsHost;
        public ISession CurrentSession => _currentSession;

        public static int ConfiguredMatchDuration { get; private set; } = 300;
        public static int ConfiguredScoreLimit { get; private set; } = 10;

        private ISession _currentSession;
        private bool _isProcessingSession = false;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }
            _instance = this;

            EnsureConnectionApprovalSetup();
        }

        private void Start()
        {
            EnsureConnectionApprovalSetup();
        }

        public void EnsureConnectionApprovalSetup()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
                NetworkManager.Singleton.ConnectionApprovalCallback = ConnectionApproval;
            }
        }

        /// <summary>
        /// Ensures UnityTransport uses WebSockets on WebGL (required by Unity Relay on that platform).
        /// UDP is kept for all other platforms (Mac, Windows, etc.).
        /// Must be called before any session is created or joined.
        /// </summary>
        private void ConfigureTransportForPlatform()
        {
            if (NetworkManager.Singleton == null) return;
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport == null) return;

#if UNITY_WEBGL
            if (!transport.UseWebSockets)
            {
                transport.UseWebSockets = true;
                Debug.Log("[SessionManager] WebGL build target: enabled WebSockets on UnityTransport for Relay.");
            }
#else
            if (transport.UseWebSockets)
            {
                transport.UseWebSockets = false;
                Debug.Log("[SessionManager] Non-WebGL build target: keeping UDP on UnityTransport for Relay.");
            }
#endif
        }

        private void ConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            Debug.Log($"[SessionManager] ConnectionApproval for client {request.ClientNetworkId}. Approving connection.");
            response.Approved = true;
            response.CreatePlayerObject = false; // Player objects are spawned explicitly on scene load
            response.Pending = false;
        }

        public async Task EnsurePreviousSessionCleanedUpAsync()
        {
            if (_currentSession != null)
            {
                try
                {
                    UnsubscribeFromSessionEvents(_currentSession);
                    if (_currentSession.IsHost)
                    {
                        var hostSession = _currentSession.AsHost();
                        if (hostSession != null)
                        {
                            await hostSession.DeleteAsync();
                            Debug.Log("[SessionManager] Host cleanly deleted session on exit.");
                        }
                        else
                        {
                            await _currentSession.LeaveAsync();
                        }
                    }
                    else
                    {
                        await _currentSession.LeaveAsync();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SessionManager] Note leaving/deleting session: {ex.Message}");
                }
                _currentSession = null;
            }

            if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsListening || NetworkManager.Singleton.ShutdownInProgress))
            {
                if (!NetworkManager.Singleton.ShutdownInProgress)
                {
                    NetworkManager.Singleton.Shutdown();
                }
                while (NetworkManager.Singleton != null && NetworkManager.Singleton.ShutdownInProgress)
                {
                    await Task.Yield();
                }
            }
        }

        /// <summary>
        /// Creates a new multiplayer session on Unity Gaming Services and starts NGO Host.
        /// </summary>
        public async Task CreateSession(string sessionName, bool isPublic, int maxPlayers, string gameMode, string mapName = "Map1", int matchDurationSeconds = 300, int scoreLimit = 10)
        {
            if (_isProcessingSession)
            {
                Debug.LogWarning("[SessionManager] Operation in progress. Request ignored.");
                return;
            }

            _isProcessingSession = true;
            ConfiguredMatchDuration = matchDurationSeconds > 0 ? matchDurationSeconds : 300;
            ConfiguredScoreLimit = scoreLimit > 0 ? scoreLimit : 10;

            try
            {
                await EnsurePreviousSessionCleanedUpAsync();
                await MultiplayerAutoInitializer.EnsureInitializedAsync();
                EnsureConnectionApprovalSetup();
                ConfigureTransportForPlatform();

                Debug.Log($"[SessionManager] Creating session '{sessionName}' (Public: {isPublic}, MaxPlayers: {maxPlayers}, Mode: {gameMode}, Map: {mapName}, Duration: {ConfiguredMatchDuration}s, ScoreLimit: {ConfiguredScoreLimit})...");

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

                // WithRelayNetwork automatically configures UTP transport and initiates NGO Host
                options.WithRelayNetwork();

                _currentSession = await MultiplayerService.Instance.CreateSessionAsync(options);
                SubscribeToSessionEvents(_currentSession);

                string myName = PlayerProfileManager.Instance != null ? PlayerProfileManager.Instance.DisplayName : "Agent";
                if (_currentSession.CurrentPlayer != null)
                {
                    _currentSession.CurrentPlayer.SetProperty("DisplayName", new PlayerProperty(myName, VisibilityPropertyOptions.Public));
                    _currentSession.CurrentPlayer.SetProperty("IsReady", new PlayerProperty("true", VisibilityPropertyOptions.Public));
                    await _currentSession.SaveCurrentPlayerDataAsync();
                }

                Debug.Log($"[SessionManager] Session created successfully. Join Code: {_currentSession.Code}, Session ID: {_currentSession.Id}");
                OnSessionCreated?.Invoke();

                // Warp directly to selected map scene (Warmup Lobby)
                string targetMap = !string.IsNullOrEmpty(mapName) ? mapName : GetSessionMapName();
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && NetworkManager.Singleton.SceneManager != null)
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

        /// <summary>
        /// Joins an existing multiplayer session using its alphanumeric join code.
        /// </summary>
        public async Task JoinByCode(string joinCode)
        {
            if (_isProcessingSession)
            {
                Debug.LogWarning("[SessionManager] Operation in progress. Request ignored.");
                return;
            }

            _isProcessingSession = true;

            try
            {
                await EnsurePreviousSessionCleanedUpAsync();
                await MultiplayerAutoInitializer.EnsureInitializedAsync();
                ConfigureTransportForPlatform();

                LoadingScreenManager.ShowOverlay("WARPING TO LOBBY...");
                Debug.Log($"[SessionManager] Joining session by code '{joinCode}'...");

                var options = new JoinSessionOptions
                {
                    Type = "default-session"
                };

                // WithRelayNetwork automatically configures UTP transport and initiates NGO Client
                _currentSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(joinCode.Trim().ToUpper(), options);
                SubscribeToSessionEvents(_currentSession);

                if (_currentSession.CurrentPlayer != null)
                {
                    string myName = PlayerProfileManager.Instance != null ? PlayerProfileManager.Instance.DisplayName : "Agent";
                    _currentSession.CurrentPlayer.SetProperty("DisplayName", new PlayerProperty(myName, VisibilityPropertyOptions.Public));
                    _currentSession.CurrentPlayer.SetProperty("IsReady", new PlayerProperty("false", VisibilityPropertyOptions.Public));
                    await _currentSession.SaveCurrentPlayerDataAsync();
                }

                Debug.Log($"[SessionManager] Successfully joined session. Session ID: {_currentSession.Id}");
                OnSessionJoined?.Invoke();
            }
            catch (Exception ex)
            {
                string errMsg = $"JoinByCode failed: {ex.Message}";
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

        /// <summary>
        /// Joins an existing multiplayer session using its session ID.
        /// </summary>
        public async Task JoinById(string sessionId)
        {
            if (_isProcessingSession)
            {
                Debug.LogWarning("[SessionManager] Operation in progress. Request ignored.");
                return;
            }

            _isProcessingSession = true;

            try
            {
                await EnsurePreviousSessionCleanedUpAsync();
                await MultiplayerAutoInitializer.EnsureInitializedAsync();
                ConfigureTransportForPlatform();

                LoadingScreenManager.ShowOverlay("WARPING TO LOBBY...");
                Debug.Log($"[SessionManager] Joining session by ID '{sessionId}'...");

                var options = new JoinSessionOptions
                {
                    Type = "default-session"
                };

                _currentSession = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId, options);
                SubscribeToSessionEvents(_currentSession);

                if (_currentSession.CurrentPlayer != null)
                {
                    string myName = PlayerProfileManager.Instance != null ? PlayerProfileManager.Instance.DisplayName : "Agent";
                    _currentSession.CurrentPlayer.SetProperty("DisplayName", new PlayerProperty(myName, VisibilityPropertyOptions.Public));
                    _currentSession.CurrentPlayer.SetProperty("IsReady", new PlayerProperty("false", VisibilityPropertyOptions.Public));
                    await _currentSession.SaveCurrentPlayerDataAsync();
                }

                Debug.Log($"[SessionManager] Successfully joined session. Session ID: {_currentSession.Id}");
                OnSessionJoined?.Invoke();
            }
            catch (Exception ex)
            {
                string errMsg = $"JoinById failed: {ex.Message}";
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

        /// <summary>
        /// Queries available public sessions.
        /// </summary>
        public async Task<List<ISessionInfo>> GetPublicSessions()
        {
            try
            {
                await MultiplayerAutoInitializer.EnsureInitializedAsync();
                var queryResult = await MultiplayerService.Instance.QuerySessionsAsync(new QuerySessionsOptions());
                return new List<ISessionInfo>(queryResult.Sessions);
            }
            catch (Exception ex)
            {
                string errMsg = $"GetPublicSessions failed: {ex.Message}";
                Debug.LogError($"[SessionManager] {errMsg}");
                OnSessionError?.Invoke(errMsg);
                throw;
            }
        }

        /// <summary>
        /// Leaves the currently connected session, shuts down NGO, and returns to MainMenu.
        /// </summary>
        public async Task LeaveSession()
        {
            try
            {
                await EnsurePreviousSessionCleanedUpAsync();

                Debug.Log("[SessionManager] Successfully left session.");
                OnSessionLeft?.Invoke();

                if (SceneManager.GetActiveScene().name != "MainMenu")
                {
                    LoadingScreenManager.LoadScene("MainMenu", "RETURNING TO BASE...");
                }
            }
            catch (Exception ex)
            {
                string errMsg = $"LeaveSession failed: {ex.Message}";
                Debug.LogError($"[SessionManager] {errMsg}");
                OnSessionError?.Invoke(errMsg);
            }
        }

        /// <summary>
        /// Updates the current session's public/private flag via UGS.
        /// </summary>
        public async Task SetSessionVisibility(bool isPublic)
        {
            try
            {
                if (_currentSession == null) return;
                var hostSession = _currentSession.AsHost();
                if (hostSession != null)
                {
                    hostSession.IsPrivate = !isPublic;
                    await hostSession.SavePropertiesAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SessionManager] SetSessionVisibility failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates local player's ready state in UGS session metadata.
        /// </summary>
        public async Task SetReadyStatus(bool isReady)
        {
            try
            {
                if (_currentSession == null || _currentSession.CurrentPlayer == null) return;
                _currentSession.CurrentPlayer.SetProperty("IsReady", new PlayerProperty(isReady.ToString().ToLower(), VisibilityPropertyOptions.Public));
                await _currentSession.SaveCurrentPlayerDataAsync();
                OnPlayerReadyChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SessionManager] SetReadyStatus failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates local player's team selection (0 = Alpha / Sapphire, 1 = Omega / Ruby) in UGS session metadata.
        /// </summary>
        public async Task SetPlayerTeam(int teamIndex)
        {
            try
            {
                if (_currentSession == null || _currentSession.CurrentPlayer == null) return;
                _currentSession.CurrentPlayer.SetProperty("TeamIndex", new PlayerProperty(teamIndex.ToString(), VisibilityPropertyOptions.Public));
                await _currentSession.SaveCurrentPlayerDataAsync();
                OnPlayerReadyChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SessionManager] SetPlayerTeam failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the ready states, team assignments, and display names of all players in the current session.
        /// </summary>
        public List<PlayerReadyState> GetPlayerReadyStates()
        {
            var states = new List<PlayerReadyState>();
            if (_currentSession == null || _currentSession.Players == null) return states;

            int fallbackIndex = 0;
            foreach (var player in _currentSession.Players)
            {
                if (player == null) continue;
                bool isReady = false;
                if (player.Properties != null && player.Properties.TryGetValue("IsReady", out var prop))
                {
                    bool.TryParse(prop.Value, out isReady);
                }
                if (player.Id == _currentSession.Host)
                {
                    isReady = true;
                }

                int teamIndex = fallbackIndex % 2;
                if (player.Properties != null && player.Properties.TryGetValue("TeamIndex", out var teamProp))
                {
                    if (int.TryParse(teamProp.Value, out int parsedTeam))
                    {
                        teamIndex = Mathf.Clamp(parsedTeam, 0, 1);
                    }
                }

                string resolvedName = null;
                if (player.Properties != null && player.Properties.TryGetValue("DisplayName", out var nameProp))
                {
                    resolvedName = nameProp.Value;
                }
                if (string.IsNullOrEmpty(resolvedName))
                {
                    resolvedName = $"Agent_{player.Id.Substring(0, Mathf.Min(4, player.Id.Length))}";
                }

                states.Add(new PlayerReadyState(player.Id, isReady, resolvedName, teamIndex));
                fallbackIndex++;
            }
            return states;
        }

        private void SubscribeToSessionEvents(ISession session)
        {
            if (session == null) return;
            session.PlayerJoined += HandlePlayerEvent;
            session.PlayerLeaving += HandlePlayerEvent;
            session.PlayerPropertiesChanged += HandleSessionChanged;
            session.SessionPropertiesChanged += HandleSessionChanged;
            session.Changed += HandleSessionChanged;
        }

        private void UnsubscribeFromSessionEvents(ISession session)
        {
            if (session == null) return;
            session.PlayerJoined -= HandlePlayerEvent;
            session.PlayerLeaving -= HandlePlayerEvent;
            session.PlayerPropertiesChanged -= HandleSessionChanged;
            session.SessionPropertiesChanged -= HandleSessionChanged;
            session.Changed -= HandleSessionChanged;
        }

        private void HandlePlayerEvent(string playerId)
        {
            OnPlayerReadyChanged?.Invoke();
            OnSessionUpdated?.Invoke();
        }

        private void HandleSessionChanged()
        {
            OnPlayerReadyChanged?.Invoke();
            OnSessionUpdated?.Invoke();
        }

        public string GetJoinCode() => _currentSession != null ? _currentSession.Code : string.Empty;
        public ISession GetCurrentSession() => _currentSession;

        public string GetSessionMapName()
        {
            if (_currentSession != null && _currentSession.Properties != null && _currentSession.Properties.TryGetValue("MapName", out var prop))
            {
                if (!string.IsNullOrEmpty(prop.Value)) return prop.Value;
            }
            return MapRegistry.DEFAULT_MAP;
        }

        public int GetSessionMatchDuration()
        {
            if (_currentSession != null && _currentSession.Properties != null && _currentSession.Properties.TryGetValue("MatchDuration", out var prop))
            {
                if (int.TryParse(prop.Value, out int val) && val > 0) return val;
            }
            return ConfiguredMatchDuration > 0 ? ConfiguredMatchDuration : 300;
        }

        public int GetSessionScoreLimit()
        {
            if (_currentSession != null && _currentSession.Properties != null && _currentSession.Properties.TryGetValue("ScoreLimit", out var prop))
            {
                if (int.TryParse(prop.Value, out int val) && val > 0) return val;
            }
            return ConfiguredScoreLimit > 0 ? ConfiguredScoreLimit : 10;
        }

        public async Task UpdateSessionMatchRulesAsync(int durationSeconds, int scoreLimit)
        {
            if (_currentSession == null || !_currentSession.IsHost) return;
            try
            {
                var hostSession = _currentSession.AsHost();
                if (hostSession != null)
                {
                    ConfiguredMatchDuration = durationSeconds > 0 ? durationSeconds : 300;
                    ConfiguredScoreLimit = scoreLimit > 0 ? scoreLimit : 10;

                    hostSession.SetProperty("MatchDuration", new SessionProperty(ConfiguredMatchDuration.ToString(), VisibilityPropertyOptions.Public, PropertyIndex.String4));
                    hostSession.SetProperty("ScoreLimit", new SessionProperty(ConfiguredScoreLimit.ToString(), VisibilityPropertyOptions.Public, PropertyIndex.String5));
                    await hostSession.SavePropertiesAsync();
                    Debug.Log($"[SessionManager] Host updated match rules: Duration={ConfiguredMatchDuration}s, ScoreLimit={ConfiguredScoreLimit}");
                    OnSessionUpdated?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SessionManager] Failed to update match rules: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the session's chosen map (Host only).
        /// </summary>
        public async Task UpdateSessionMapAsync(string newMapName)
        {
            if (_currentSession == null || !_currentSession.IsHost) return;
            try
            {
                var hostSession = _currentSession.AsHost();
                if (hostSession != null)
                {
                    hostSession.SetProperty("MapName", new SessionProperty(newMapName, VisibilityPropertyOptions.Public, PropertyIndex.String2));
                    await hostSession.SavePropertiesAsync();
                    Debug.Log($"[SessionManager] Host updated session map to: {newMapName}");
                    OnSessionUpdated?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SessionManager] Failed to update map: {ex.Message}");
            }
        }

        /// <summary>
        /// Host launches the match: loads Map1 networked scene for all connected clients.
        /// </summary>
        public void StartMatch()
        {
            if (_currentSession == null || !_currentSession.IsHost)
            {
                Debug.LogWarning("[SessionManager] Only the host can start the match!");
                return;
            }

            string mapName = GetSessionMapName();
            Debug.Log($"[SessionManager] Host starting match on map: {mapName}...");

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.SceneManager.LoadScene(mapName, LoadSceneMode.Single);
            }
            else
            {
                LoadingScreenManager.LoadScene(mapName, "DEPLOYING TO ARENA...");
            }
        }

        /// <summary>
        /// Updates the session state (e.g. "Lobby", "InProgress") in UGS metadata (Host only).
        /// </summary>
        public async Task UpdateSessionStateAsync(string newState)
        {
            if (_currentSession == null || !_currentSession.IsHost) return;
            try
            {
                var hostSession = _currentSession.AsHost();
                if (hostSession != null)
                {
                    hostSession.SetProperty("State", new SessionProperty(newState, VisibilityPropertyOptions.Public, PropertyIndex.String3));
                    await hostSession.SavePropertiesAsync();
                    Debug.Log($"[SessionManager] Host updated session State to: {newState}");
                    OnSessionUpdated?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SessionManager] Failed to update session State: {ex.Message}");
            }
        }

        private void OnApplicationQuit()
        {
            if (_currentSession != null)
            {
                try
                {
                    UnsubscribeFromSessionEvents(_currentSession);
                    if (_currentSession.IsHost)
                    {
                        _ = _currentSession.AsHost()?.DeleteAsync();
                    }
                    else
                    {
                        _ = _currentSession.LeaveAsync();
                    }
                }
                catch { }
                _currentSession = null;
            }
        }
    }
}
