using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using Unity.Services.Multiplayer;

namespace Shootopia
{
    public partial class WaitingRoomController
    {
public void RefreshLobbyUI()
        {
            if (_matchStarted) return;

            var session = SessionManager.Instance != null ? SessionManager.Instance.GetCurrentSession() : null;
            bool isHost = (session != null && session.IsHost) ||
                          (SessionManager.Instance != null && SessionManager.Instance.IsHost) ||
                          (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer));

            // Room Name
            if (roomNameLabel != null)
            {
                roomNameLabel.text = session != null ? session.Name.ToUpper() : "CUSTOM ARENA";
            }

            // Mode & Map
            string mode = SessionManager.Instance != null ? SessionManager.Instance.GetSessionGameMode() : "FFA";
            if (session != null && session.Properties != null && session.Properties.TryGetValue("GameMode", out var modeProp))
            {
                if (!string.IsNullOrEmpty(modeProp.Value)) mode = modeProp.Value.ToUpper();
            }

            bool isTeamMode = (mode == "TDM" || mode.Contains("TEAM"));
            int durSec = SessionManager.Instance != null ? SessionManager.Instance.GetSessionMatchDuration() : 300;
            int scoreGoal = SessionManager.Instance != null ? SessionManager.Instance.GetSessionScoreLimit() : 10;
            int durMin = Mathf.Max(1, durSec / 60);

            if (roomModeBadge != null)
            {
                string modeLabel = isTeamMode ? "TEAM DEATHMATCH" : "FREE-FOR-ALL";
                roomModeBadge.text = $"{modeLabel} • {durMin}M • {scoreGoal} {(isTeamMode ? "PTS" : "KILLS")}";
                roomModeBadge.color = isTeamMode ? ColorGold : ColorSapphire;
            }

            string map = SessionManager.Instance != null ? SessionManager.Instance.GetSessionMapName() : "Map1";
            if (roomMapLabel != null)
            {
                roomMapLabel.text = $"MAP: {MapRegistry.GetDisplayName(map)}";
            }
            if (roomMapThumbnail != null)
            {
                roomMapThumbnail.sprite = (map == "GasStationMap") ? mapGasStationSprite : mapSectorAlphaSprite;
                roomMapThumbnail.gameObject.SetActive(roomMapThumbnail.sprite != null);
            }

            // Room Code
            string code = SessionManager.Instance != null ? SessionManager.Instance.GetJoinCode() : "";
            if (string.IsNullOrEmpty(code) && PlayerPrefs.HasKey("Shootopia_CurrentRoomCode"))
            {
                code = PlayerPrefs.GetString("Shootopia_CurrentRoomCode");
            }
            if (string.IsNullOrEmpty(code)) code = "------";
            if (roomCodeDisplay != null)
            {
                roomCodeDisplay.text = code;
            }

            // Player Count
            int count = 1;
            int max = 8;
            if (session != null && session.Players != null)
            {
                count = session.Players.Count;
                max = session.MaxPlayers;
            }
            else if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                count = NetworkManager.Singleton.ConnectedClients.Count;
                max = 8;
            }

            if (roomPlayerCountLabel != null)
            {
                roomPlayerCountLabel.text = $"{count}/{max} PLAYERS";
            }

            // Host Controls
            if (hostSettingsGroup != null)
            {
                hostSettingsGroup.SetActive(isHost);
            }

            if (btnStartMatch != null)
            {
                btnStartMatch.gameObject.SetActive(isHost);
                btnStartMatch.interactable = CanStartMatch(mode, count);
            }

            if (btnReady != null)
            {
                btnReady.gameObject.SetActive(!isHost);
                if (btnReadyLabel != null)
                {
                    btnReadyLabel.text = _isLocalReady ? "UNREADY" : "READY";
                }
            }

            // Toggle Panels based on Game Mode
            if (panelFFA != null) panelFFA.SetActive(!isTeamMode);
            if (panelTeams != null) panelTeams.SetActive(isTeamMode);

            // Update Team Switch buttons interactivity
            if (isTeamMode)
            {
                if (btnSelectAlpha != null) btnSelectAlpha.interactable = !_isLocalReady;
                if (btnSelectOmega != null) btnSelectOmega.interactable = !_isLocalReady;

                // Highlight active team button
                if (imgSelectAlphaBg != null)
                {
                    imgSelectAlphaBg.color = (_localTeamIndex == 0) ? new Color(0.2f, 0.75f, 1f, 1f) : new Color(0.2f, 0.3f, 0.5f, 0.6f);
                }
                if (imgSelectOmegaBg != null)
                {
                    imgSelectOmegaBg.color = (_localTeamIndex == 1) ? new Color(1f, 0.3f, 0.45f, 1f) : new Color(0.5f, 0.2f, 0.3f, 0.6f);
                }
            }

            PopulateRoster(session, isHost, isTeamMode);
        }
    }
}
