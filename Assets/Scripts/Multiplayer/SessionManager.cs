using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Unity.Services.Multiplayer;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace Shootopia
{
    /// <summary>
    /// Streamlined central manager for multiplayer sessions (UGS + NGO + Relay + LAN).
    /// Handles session lifecycle, connection approval, transport configuration, and event dispatch.
    /// </summary>
    public partial class SessionManager : MonoBehaviour
    {
        private static SessionManager _instance;
        public static SessionManager Instance
        {
            get
            {
                if (_instance == null) _instance = FindAnyObjectByType<SessionManager>();
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

        public bool IsHost => (IsLanSession && NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost) || (_currentSession != null && _currentSession.IsHost);
        public ISession CurrentSession => _currentSession;

        public bool IsLanSession { get; private set; } = false;
        public string CurrentRoomCode { get; private set; } = "";

        public static int ConfiguredMatchDuration { get; private set; } = 300;
        public static int ConfiguredScoreLimit { get; private set; } = 10;
        public static string ConfiguredGameMode { get; private set; } = "FFA";

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

        private void Start() => EnsureConnectionApprovalSetup();

        public void EnsureConnectionApprovalSetup()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
                NetworkManager.Singleton.ConnectionApprovalCallback = ConnectionApproval;
            }
        }

        private void ConfigureTransportForPlatform()
        {
            if (NetworkManager.Singleton == null) return;
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport == null) return;

#if UNITY_WEBGL
            if (!transport.UseWebSockets)
            {
                transport.UseWebSockets = true;
                Debug.Log("[SessionManager] WebGL: enabled WebSockets on UnityTransport for Relay.");
            }
#else
            if (transport.UseWebSockets)
            {
                transport.UseWebSockets = false;
                Debug.Log("[SessionManager] Desktop: using standard UDP on UnityTransport.");
            }
#endif
        }

        private void ConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            response.Approved = true;
            response.CreatePlayerObject = false;
            response.Pending = false;
        }

        public async Task EnsurePreviousSessionCleanedUpAsync()
        {
            LanDiscoveryManager.StopBroadcasting();
            IsLanSession = false;
            CurrentRoomCode = "";

            if (_currentSession != null)
            {
                try
                {
                    UnsubscribeFromSessionEvents(_currentSession);
                    if (_currentSession.IsHost)
                    {
                        var hostSession = _currentSession.AsHost();
                        if (hostSession != null) await hostSession.DeleteAsync();
                        else await _currentSession.LeaveAsync();
                    }
                    else
                    {
                        await _currentSession.LeaveAsync();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SessionManager] Cleaned previous session: {ex.Message}");
                }
                _currentSession = null;
            }

            if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsListening || NetworkManager.Singleton.ShutdownInProgress))
            {
                if (!NetworkManager.Singleton.ShutdownInProgress) NetworkManager.Singleton.Shutdown();
                while (NetworkManager.Singleton != null && NetworkManager.Singleton.ShutdownInProgress)
                {
                    await Task.Yield();
                }
            }

            if (NetworkedGameManager.Instance != null && NetworkedGameManager.Instance.gameObject.scene.name == "DontDestroyOnLoad")
            {
                Destroy(NetworkedGameManager.Instance.gameObject);
            }
        }

        public string GetJoinCode() => !string.IsNullOrEmpty(CurrentRoomCode) ? CurrentRoomCode : (_currentSession != null ? _currentSession.Code : string.Empty);
        public ISession GetCurrentSession() => _currentSession;

        private void OnApplicationQuit()
        {
            if (IsLanSession) LanDiscoveryManager.StopBroadcasting();
        }
    }
}
