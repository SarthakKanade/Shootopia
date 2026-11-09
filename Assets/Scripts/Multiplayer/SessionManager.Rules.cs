using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Multiplayer;
using Unity.Netcode;

namespace Shootopia
{
    public partial class SessionManager
    {
        public async Task SetSessionVisibility(bool isPublic)
        {
            if (_currentSession == null || !_currentSession.IsHost) return;
            try
            {
                var hostSession = _currentSession.AsHost();
                if (hostSession != null)
                {
                    hostSession.IsPrivate = !isPublic;
                    await hostSession.SavePropertiesAsync();
                    OnSessionUpdated?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SessionManager] SetSessionVisibility failed: {ex.Message}");
            }
        }

        public async Task SetReadyStatus(bool isReady)
        {
            if (_currentSession?.CurrentPlayer == null) return;
            try
            {
                _currentSession.CurrentPlayer.SetProperty("IsReady", new PlayerProperty(isReady ? "true" : "false", VisibilityPropertyOptions.Public));
                await _currentSession.SaveCurrentPlayerDataAsync();
                OnPlayerReadyChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SessionManager] SetReadyStatus failed: {ex.Message}");
            }
        }

        public async Task SetPlayerTeam(int teamIndex)
        {
            if (_currentSession?.CurrentPlayer == null) return;
            try
            {
                _currentSession.CurrentPlayer.SetProperty("TeamIndex", new PlayerProperty(teamIndex.ToString(), VisibilityPropertyOptions.Public));
                await _currentSession.SaveCurrentPlayerDataAsync();
                OnPlayerReadyChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SessionManager] SetPlayerTeam failed: {ex.Message}");
            }
        }

        public List<PlayerReadyState> GetPlayerReadyStates()
        {
            var list = new List<PlayerReadyState>();
            if (IsLanSession)
            {
                if (NetworkManager.Singleton != null)
                {
                    foreach (var c in NetworkManager.Singleton.ConnectedClientsList)
                    {
                        ulong id = c.ClientId;
                        string dName = PlayerNameRegistry.GetName(id);
                        int team = (GameModeBase.Instance is TDMMode tdm) ? tdm.GetPlayerTeam(id) : 0;
                        list.Add(new PlayerReadyState(id.ToString(), true, dName, team));
                    }
                }
                return list;
            }

            if (_currentSession?.Players == null) return list;

            foreach (var p in _currentSession.Players)
            {
                string name = p.Properties?.ContainsKey("DisplayName") == true ? p.Properties["DisplayName"].Value : p.Id;
                bool ready = p.Properties?.ContainsKey("IsReady") == true && p.Properties["IsReady"].Value == "true";
                int team = 0;
                if (p.Properties?.ContainsKey("TeamIndex") == true && int.TryParse(p.Properties["TeamIndex"].Value, out int parsed))
                    team = parsed;

                list.Add(new PlayerReadyState(p.Id, ready, name, team));
            }
            return list;
        }

        public string GetSessionMapName()
        {
            if (_currentSession?.Properties != null && _currentSession.Properties.TryGetValue("MapName", out var prop))
                return prop.Value;
            return "Map1";
        }

        public int GetSessionMatchDuration()
        {
            if (_currentSession?.Properties != null && _currentSession.Properties.TryGetValue("MatchDuration", out var prop) && int.TryParse(prop.Value, out int d))
                return d;
            return ConfiguredMatchDuration;
        }

        public int GetSessionScoreLimit()
        {
            if (_currentSession?.Properties != null && _currentSession.Properties.TryGetValue("ScoreLimit", out var prop) && int.TryParse(prop.Value, out int s))
                return s;
            return ConfiguredScoreLimit;
        }

        public string GetSessionGameMode()
        {
            if (_currentSession?.Properties != null && _currentSession.Properties.TryGetValue("GameMode", out var prop))
                return prop.Value;
            return ConfiguredGameMode;
        }

        public async Task UpdateSessionMatchRulesAsync(int durationSeconds, int scoreLimit)
        {
            ConfiguredMatchDuration = durationSeconds;
            ConfiguredScoreLimit = scoreLimit;

            if (_currentSession != null && _currentSession.IsHost)
            {
                try
                {
                    var host = _currentSession.AsHost();
                    if (host != null)
                    {
                        host.SetProperty("MatchDuration", new SessionProperty(durationSeconds.ToString(), VisibilityPropertyOptions.Public, PropertyIndex.String4));
                        host.SetProperty("ScoreLimit", new SessionProperty(scoreLimit.ToString(), VisibilityPropertyOptions.Public, PropertyIndex.String5));
                        await host.SavePropertiesAsync();
                        OnSessionUpdated?.Invoke();
                    }
                }
                catch (Exception ex) { Debug.LogWarning($"[SessionManager] UpdateMatchRules: {ex.Message}"); }
            }
        }

        public async Task UpdateSessionMapAsync(string newMapName)
        {
            if (_currentSession != null && _currentSession.IsHost)
            {
                try
                {
                    var host = _currentSession.AsHost();
                    if (host != null)
                    {
                        host.SetProperty("MapName", new SessionProperty(newMapName, VisibilityPropertyOptions.Public, PropertyIndex.String2));
                        await host.SavePropertiesAsync();
                        OnSessionUpdated?.Invoke();
                    }
                }
                catch (Exception ex) { Debug.LogWarning($"[SessionManager] UpdateSessionMap: {ex.Message}"); }
            }
        }

        public async Task UpdateSessionStateAsync(string newState)
        {
            if (_currentSession != null && _currentSession.IsHost)
            {
                try
                {
                    var host = _currentSession.AsHost();
                    if (host != null)
                    {
                        host.SetProperty("State", new SessionProperty(newState, VisibilityPropertyOptions.Public, PropertyIndex.String3));
                        await host.SavePropertiesAsync();
                        OnSessionUpdated?.Invoke();
                    }
                }
                catch (Exception ex) { Debug.LogWarning($"[SessionManager] UpdateSessionState: {ex.Message}"); }
            }
        }

        public void StartMatch()
        {
            if (NetworkedGameManager.Instance != null)
            {
                NetworkedGameManager.Instance.StartMatchFromLobby();
            }
        }
    }
}
