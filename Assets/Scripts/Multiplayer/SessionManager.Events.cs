using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Multiplayer;
using Unity.Netcode;

namespace Shootopia
{
    public partial class SessionManager
    {
        public async Task LeaveSession()
        {
            if (_currentSession != null)
            {
                try
                {
                    UnsubscribeFromSessionEvents(_currentSession);
                    if (_currentSession.IsHost)
                    {
                        var host = _currentSession.AsHost();
                        if (host != null) await host.DeleteAsync();
                        else await _currentSession.LeaveAsync();
                    }
                    else await _currentSession.LeaveAsync();
                }
                catch (Exception ex) { Debug.LogWarning($"[SessionManager] LeaveSession note: {ex.Message}"); }
                _currentSession = null;
            }

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.ShutdownInProgress)
            {
                try
                {
                    NetworkManager.Singleton.Shutdown();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SessionManager] NetworkManager shutdown note: {ex.Message}");
                }
            }

            while (NetworkManager.Singleton != null && NetworkManager.Singleton.ShutdownInProgress)
            {
                await Task.Yield();
            }

            LanDiscoveryManager.StopBroadcasting();
            IsLanSession = false;
            CurrentRoomCode = "";
            OnSessionLeft?.Invoke();
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
            OnSessionUpdated?.Invoke();
        }
    }
}
