using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Unity.Services.Multiplayer;
using Unity.Netcode;

namespace Shootopia
{
    /// <summary>
    /// Manages multiplayer session creation, joining, browsing, and leaving using the Unity MPS SDK.
    /// Acts as a central persistent singleton to expose session state to the UI and network layers.
    /// </summary>
    public class SessionManager : MonoBehaviour
    {
        public static SessionManager Instance { get; private set; }

        [Header("Events")]
        public UnityEvent OnSessionCreated;
        public UnityEvent OnSessionJoined;
        public UnityEvent OnSessionLeft;
        public UnityEvent<string> OnSessionError;
        public UnityEvent OnPlayerReadyChanged;

        private ISession _currentSession;

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

        private void Start()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
                NetworkManager.Singleton.ConnectionApprovalCallback = ConnectionApproval;
            }
        }

        private void ConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            response.Approved = true;
            response.CreatePlayerObject = false; // Disable NGO automatic spawning globally
            response.Pending = false;
        }

        /// <summary>
        /// Creates a new multiplayer session on the Unity backend.
        /// </summary>
        public async Task CreateSession(string sessionName, bool isPublic, int maxPlayers, string gameMode)
        {
            try
            {
                Debug.Log($"[SessionManager] Creating session '{sessionName}' (Public: {isPublic}, MaxPlayers: {maxPlayers}, Mode: {gameMode})...");
                
                var options = new SessionOptions
                {
                    Name = sessionName,
                    MaxPlayers = maxPlayers,
                    IsPrivate = !isPublic,
                    Type = "default-session"
                };

                // Add the custom GameMode property for session browsing filters
                options.SessionProperties = new Dictionary<string, SessionProperty>
                {
                    { "GameMode", new SessionProperty(gameMode, VisibilityPropertyOptions.Public, PropertyIndex.String1) }
                };

                // Configure connection protocol using Relay networking
                options.WithRelayNetwork();

                // Create the session through UGS
                _currentSession = await MultiplayerService.Instance.CreateSessionAsync(options);
                SubscribeToSessionEvents(_currentSession);
                
                Debug.Log($"[SessionManager] Session created successfully. Join Code: {_currentSession.Code}, Session ID: {_currentSession.Id}");
                OnSessionCreated?.Invoke();
            }
            catch (Exception ex)
            {
                string errMsg = $"CreateSession failed: {ex.Message}";
                Debug.LogError($"[SessionManager] {errMsg}");
                OnSessionError?.Invoke(errMsg);
                throw;
            }
        }

        /// <summary>
        /// Joins an existing multiplayer session using its alphanumeric join code.
        /// </summary>
        public async Task JoinByCode(string joinCode)
        {
            try
            {
                Debug.Log($"[SessionManager] Joining session by code '{joinCode}'...");
                
                var options = new JoinSessionOptions
                {
                    Type = "default-session"
                };

                _currentSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(joinCode, options);
                SubscribeToSessionEvents(_currentSession);
                
                Debug.Log($"[SessionManager] Successfully joined session. Session ID: {_currentSession.Id}");
                OnSessionJoined?.Invoke();
            }
            catch (Exception ex)
            {
                string errMsg = $"JoinByCode failed: {ex.Message}";
                Debug.LogError($"[SessionManager] {errMsg}");
                OnSessionError?.Invoke(errMsg);
                throw;
            }
        }

        /// <summary>
        /// Joins an existing multiplayer session using its session ID.
        /// </summary>
        public async Task JoinById(string sessionId)
        {
            try
            {
                Debug.Log($"[SessionManager] Joining session by ID '{sessionId}'...");
                
                var options = new JoinSessionOptions
                {
                    Type = "default-session"
                };

                _currentSession = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId, options);
                SubscribeToSessionEvents(_currentSession);
                
                Debug.Log($"[SessionManager] Successfully joined session. Session ID: {_currentSession.Id}");
                OnSessionJoined?.Invoke();
            }
            catch (Exception ex)
            {
                string errMsg = $"JoinById failed: {ex.Message}";
                Debug.LogError($"[SessionManager] {errMsg}");
                OnSessionError?.Invoke(errMsg);
                throw;
            }
        }

        /// <summary>
        /// Queries available public sessions.
        /// </summary>
        public async Task<List<ISessionInfo>> GetPublicSessions()
        {
            try
            {
                Debug.Log("[SessionManager] Querying public sessions...");
                
                var queryOptions = new QuerySessionsOptions
                {
                    // By default queries active public sessions matching type
                };

                var queryResult = await MultiplayerService.Instance.QuerySessionsAsync(queryOptions);
                Debug.Log($"[SessionManager] Query complete. Found {queryResult.Sessions.Count} public sessions.");
                
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
        /// Leaves the currently connected session and clears local references.
        /// </summary>
        public async Task LeaveSession()
        {
            try
            {
                if (_currentSession != null)
                {
                    Debug.Log($"[SessionManager] Leaving current session (ID: {_currentSession.Id})...");
                    UnsubscribeFromSessionEvents(_currentSession);
                    await _currentSession.LeaveAsync();
                    _currentSession = null;
                    Debug.Log("[SessionManager] Successfully left the session.");
                }
                else
                {
                    Debug.LogWarning("[SessionManager] LeaveSession called but no active session was stored.");
                }
                OnSessionLeft?.Invoke();
            }
            catch (Exception ex)
            {
                string errMsg = $"LeaveSession failed: {ex.Message}";
                Debug.LogError($"[SessionManager] {errMsg}");
                OnSessionError?.Invoke(errMsg);
                throw;
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
                    Debug.Log($"[SessionManager] Setting session visibility to Public: {isPublic}...");
                    hostSession.IsPrivate = !isPublic;
                    await hostSession.SavePropertiesAsync();
                    Debug.Log("[SessionManager] Visibility updated successfully.");
                }
                else
                {
                    Debug.LogWarning("[SessionManager] SetSessionVisibility called but local client is not the host.");
                }
            }
            catch (Exception ex)
            {
                string errMsg = $"SetSessionVisibility failed: {ex.Message}";
                Debug.LogError($"[SessionManager] {errMsg}");
                OnSessionError?.Invoke(errMsg);
                throw;
            }
        }

        /// <summary>
        /// Updates the local player's ready state in UGS session metadata.
        /// </summary>
        public async Task SetReadyStatus(bool isReady)
        {
            try
            {
                if (_currentSession == null) return;
                var localPlayer = _currentSession.CurrentPlayer;
                if (localPlayer != null)
                {
                    Debug.Log($"[SessionManager] Setting local player ready state: {isReady}...");
                    localPlayer.SetProperty("IsReady", new PlayerProperty(isReady.ToString().ToLower(), VisibilityPropertyOptions.Public));
                    await _currentSession.SaveCurrentPlayerDataAsync();
                    Debug.Log("[SessionManager] Local player ready status saved.");
                    OnPlayerReadyChanged?.Invoke();
                }
            }
            catch (Exception ex)
            {
                string errMsg = $"SetReadyStatus failed: {ex.Message}";
                Debug.LogError($"[SessionManager] {errMsg}");
                OnSessionError?.Invoke(errMsg);
                throw;
            }
        }

        /// <summary>
        /// Gets the ready states and display names of all players in the current session.
        /// </summary>
        public List<PlayerReadyState> GetPlayerReadyStates()
        {
            var states = new List<PlayerReadyState>();
            if (_currentSession == null) return states;

            foreach (var player in _currentSession.Players)
            {
                bool isReady = false;
                if (player.Properties != null && player.Properties.TryGetValue("IsReady", out var prop))
                {
                    bool.TryParse(prop.Value, out isReady);
                }

                // If player is host, default to ready = true
                if (player.Id == _currentSession.Host)
                {
                    isReady = true;
                }

                states.Add(new PlayerReadyState(player.Id, isReady, player.GetPlayerName()));
            }
            return states;
        }

        private void SubscribeToSessionEvents(ISession session)
        {
            if (session == null) return;
            session.PlayerJoined += OnPlayerJoinedOrLeft;
            session.PlayerLeaving += OnPlayerJoinedOrLeft;
            session.PlayerPropertiesChanged += OnPlayerPropertiesUpdated;
            session.Changed += OnSessionPropertiesUpdated;
        }

        private void UnsubscribeFromSessionEvents(ISession session)
        {
            if (session == null) return;
            session.PlayerJoined -= OnPlayerJoinedOrLeft;
            session.PlayerLeaving -= OnPlayerJoinedOrLeft;
            session.PlayerPropertiesChanged -= OnPlayerPropertiesUpdated;
            session.Changed -= OnSessionPropertiesUpdated;
        }

        private void OnPlayerJoinedOrLeft(string playerId)
        {
            Debug.Log($"[SessionManager] Player joined/left: {playerId}");
            OnPlayerReadyChanged?.Invoke();
        }

        private void OnPlayerPropertiesUpdated()
        {
            Debug.Log("[SessionManager] Player properties updated.");
            OnPlayerReadyChanged?.Invoke();
        }

        private void OnSessionPropertiesUpdated()
        {
            Debug.Log("[SessionManager] Session properties updated.");
            OnPlayerReadyChanged?.Invoke();
        }

        /// <summary>
        /// Returns the alphanumeric join code of the active session.
        /// </summary>
        public string GetJoinCode()
        {
            if (_currentSession != null)
            {
                return _currentSession.Code;
            }
            return string.Empty;
        }

        /// <summary>
        /// Gets the reference to the underlying ISession.
        /// </summary>
        public ISession GetCurrentSession()
        {
            return _currentSession;
        }

        /// <summary>
        /// Updates the session properties to indicate that the match has started.
        /// </summary>
        public async Task StartMatch()
        {
            try
            {
                if (_currentSession == null) return;
                var hostSession = _currentSession.AsHost();
                if (hostSession != null)
                {
                    Debug.Log("[SessionManager] Starting match, setting MatchStarted property...");
                    hostSession.SetProperty("MatchStarted", new SessionProperty("true", VisibilityPropertyOptions.Public));
                    await hostSession.SavePropertiesAsync();
                    Debug.Log("[SessionManager] Match started property saved.");
                }
                else
                {
                    Debug.LogWarning("[SessionManager] StartMatch called but local client is not the host.");
                }
            }
            catch (Exception ex)
            {
                string errMsg = $"StartMatch failed: {ex.Message}";
                Debug.LogError($"[SessionManager] {errMsg}");
                OnSessionError?.Invoke(errMsg);
                throw;
            }
        }
    }
}

/*
VERIFICATION:
- Two Editor instances (ParrelSync or two builds).
- Instance A: CreateSession — console logs a join code.
- Instance B: JoinByCode with that code — console logs joined.
- GetPublicSessions returns the created session in the list.
*/
