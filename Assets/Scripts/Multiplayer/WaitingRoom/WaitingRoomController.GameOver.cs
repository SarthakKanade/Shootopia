using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace Shootopia
{
    public partial class WaitingRoomController
    {
        public static void ShowGameOver(string winnerInfo, PlayerScoreEntry[] scores)
        {
            if (Instance == null)
            {
                Instance = FindAnyObjectByType<WaitingRoomController>(FindObjectsInactive.Include);
            }
            if (Instance != null)
            {
                if (!Instance.gameObject.activeSelf)
                {
                    Instance.gameObject.SetActive(true);
                }
                Instance.DisplayGameOver(winnerInfo, scores);
            }
        }

        private void OnMatchEndedScoreboardReceived(string winnerInfo, PlayerScoreEntry[] scores)
        {
            DisplayGameOver(winnerInfo, scores);
        }

        public void DisplayGameOver(string winnerInfo, PlayerScoreEntry[] scores)
        {
            _isGameOverMode = true;
            _cachedGameOverWinnerInfo = winnerInfo;
            _cachedGameOverScores = scores;

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            SetLobbyVisible(true);

            if (warmupHudBadge != null) warmupHudBadge.SetActive(false);
            if (btnMinimizeToWarmup != null) btnMinimizeToWarmup.gameObject.SetActive(false);
            if (btnOpenLobbyFromWarmup != null) btnOpenLobbyFromWarmup.gameObject.SetActive(false);
            if (hostSettingsGroup != null) hostSettingsGroup.SetActive(false);

            RefreshGameOverUI();
        }

        public void RefreshGameOverUI()
        {
            var session = SessionManager.Instance != null ? SessionManager.Instance.GetCurrentSession() : null;
            bool isHost = (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer) || (session != null && session.IsHost);

            var activeMode = NetworkedGameManager.Instance != null ? NetworkedGameManager.Instance.ActiveGameMode : GameModeBase.Instance;
            if (activeMode == null)
            {
                activeMode = (GameModeBase)FindAnyObjectByType<FFAMode>() ?? (GameModeBase)FindAnyObjectByType<TDMMode>();
            }

            string mode = (activeMode is TDMMode) ? "TDM" : "FFA";
            if (session != null && session.Properties != null && session.Properties.TryGetValue("GameMode", out var modeProp))
            {
                if (modeProp.Value.ToUpper().Contains("TDM") || modeProp.Value.ToUpper().Contains("TEAM"))
                {
                    mode = "TDM";
                }
            }
            bool isTeamMode = (mode == "TDM" || activeMode is TDMMode);

            // Outcome Header
            string title = string.IsNullOrEmpty(_cachedGameOverWinnerInfo) ? "MATCH COMPLETED" : _cachedGameOverWinnerInfo.ToUpper();
            if (roomNameLabel != null)
            {
                roomNameLabel.text = title;
                roomNameLabel.color = ColorGold;
            }

            if (roomModeBadge != null)
            {
                roomModeBadge.text = isTeamMode ? "TEAM DEATHMATCH RESULTS" : "FREE-FOR-ALL RESULTS";
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

            if (roomPlayerCountLabel != null)
            {
                roomPlayerCountLabel.text = "MATCH FINISHED";
            }

            // Header Leave button
            if (btnLeave != null)
            {
                btnLeave.onClick.RemoveAllListeners();
                btnLeave.onClick.AddListener(LeaveLobby);
            }

            // Rematch button (replaces Start Match button)
            if (btnStartMatch != null)
            {
                btnStartMatch.gameObject.SetActive(true);
                var btnTxt = btnStartMatch.GetComponentInChildren<TextMeshProUGUI>();
                if (isHost)
                {
                    btnStartMatch.interactable = true;
                    if (btnTxt != null) btnTxt.text = "REMATCH";
                    btnStartMatch.onClick.RemoveAllListeners();
                    btnStartMatch.onClick.AddListener(OnRematchClicked);
                }
                else
                {
                    btnStartMatch.interactable = false;
                    if (btnTxt != null) btnTxt.text = "WAITING FOR REMATCH...";
                }
            }

            // Bottom Leave Lobby button (replaces Ready button)
            if (btnReady != null)
            {
                btnReady.gameObject.SetActive(true);
                btnReady.interactable = true;
                if (btnReadyLabel != null)
                {
                    btnReadyLabel.text = "LEAVE LOBBY";
                }
                btnReady.onClick.RemoveAllListeners();
                btnReady.onClick.AddListener(LeaveLobby);
            }

            if (rosterStatusSummary != null)
            {
                rosterStatusSummary.text = isHost
                    ? "<color=#45F595>MATCH FINISHED — HOST CAN START REMATCH OR LEAVE</color>"
                    : "<color=#FFD555>MATCH FINISHED — WAITING FOR HOST REMATCH</color>";
            }

            PopulateGameOverRoster(isTeamMode, _cachedGameOverScores, activeMode);
        }

        private void OnRematchClicked()
        {
            if (btnStartMatch != null) btnStartMatch.interactable = false;

            if (NetworkedGameManager.Instance != null)
            {
                NetworkedGameManager.Instance.RequestRematch();
            }
            else if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                string currentMap = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                NetworkManager.Singleton.SceneManager.LoadScene(currentMap, UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        }

        private async void LeaveLobby()
        {
            LoadingScreenManager.ShowOverlay("RETURNING TO MAIN MENU...");

            if (SessionManager.Instance != null)
            {
                try { await SessionManager.Instance.LeaveSession(); } catch { }
            }
            else if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.ShutdownInProgress)
            {
                try { NetworkManager.Singleton.Shutdown(); } catch { }
            }

            // Ensure we return to MainMenu even if leave threw an exception or session was null
            if (SceneManager.GetActiveScene().name != "MainMenu")
            {
                SceneManager.LoadScene("MainMenu");
            }
        }
    }
}
