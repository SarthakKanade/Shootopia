using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;

namespace Shootopia
{
    public partial class WaitingRoomController
    {
private void RegisterCallbacks()
        {
            if (btnCopyCode != null) btnCopyCode.onClick.AddListener(CopyRoomCode);
            if (btnReady != null) btnReady.onClick.AddListener(ToggleReady);
            if (btnStartMatch != null) btnStartMatch.onClick.AddListener(StartMatchAsHost);
            if (btnLeave != null) btnLeave.onClick.AddListener(LeaveLobby);
            if (btnMinimizeToWarmup != null) btnMinimizeToWarmup.onClick.AddListener(() => SetLobbyVisible(false));
            if (btnOpenLobbyFromWarmup != null) btnOpenLobbyFromWarmup.onClick.AddListener(() => SetLobbyVisible(true));
            if (togglePublicRoom != null) togglePublicRoom.onValueChanged.AddListener(OnVisibilityToggled);
            if (btnSelectAlpha != null) btnSelectAlpha.onClick.AddListener(() => SwitchTeam(0));
            if (btnSelectOmega != null) btnSelectOmega.onClick.AddListener(() => SwitchTeam(1));

            if (roomMapLabel != null)
            {
                var btnMap = roomMapLabel.GetComponent<Button>();
                if (btnMap != null) btnMap.onClick.AddListener(CycleMapAsHost);
            }
        }

        private async void CycleMapAsHost()
        {
            if (SessionManager.Instance == null || !SessionManager.Instance.IsHost) return;
            string currentMap = SessionManager.Instance.GetSessionMapName();
            string nextMap = (currentMap == "Map1") ? "GasStationMap" : "Map1";
            await SessionManager.Instance.UpdateSessionMapAsync(nextMap);
            RefreshLobbyUI();
        }

        private void UnregisterCallbacks()
        {
            if (btnCopyCode != null) btnCopyCode.onClick.RemoveAllListeners();
            if (btnReady != null) btnReady.onClick.RemoveAllListeners();
            if (btnStartMatch != null) btnStartMatch.onClick.RemoveAllListeners();
            if (btnLeave != null) btnLeave.onClick.RemoveAllListeners();
            if (btnMinimizeToWarmup != null) btnMinimizeToWarmup.onClick.RemoveAllListeners();
            if (btnOpenLobbyFromWarmup != null) btnOpenLobbyFromWarmup.onClick.RemoveAllListeners();
            if (togglePublicRoom != null) togglePublicRoom.onValueChanged.RemoveAllListeners();
            if (btnSelectAlpha != null) btnSelectAlpha.onClick.RemoveAllListeners();
            if (btnSelectOmega != null) btnSelectOmega.onClick.RemoveAllListeners();
            if (roomMapLabel != null)
            {
                var btnMap = roomMapLabel.GetComponent<Button>();
                if (btnMap != null) btnMap.onClick.RemoveAllListeners();
            }
        }

private bool AreAllPlayersReady()
        {
            if (SessionManager.Instance == null) return true;
            var readyStates = SessionManager.Instance.GetPlayerReadyStates();
            if (readyStates == null || readyStates.Count <= 1) return true;

            var session = SessionManager.Instance.GetCurrentSession();
            string hostId = (session != null && !string.IsNullOrEmpty(session.Host)) ? session.Host : (NetworkManager.Singleton != null ? NetworkManager.ServerClientId.ToString() : "0");
            foreach (var p in readyStates)
            {
                bool isHost = (p.playerId == hostId);
                if (!isHost && !p.isReady)
                {
                    return false;
                }
            }
            return true;
        }

        private bool CanStartMatch(string mode, int playerCount)
        {
            // Allow the host to start only when there is at least 1 player and all joined players are ready
            if (playerCount < 1) return false;
            return AreAllPlayersReady();
        }

private void CopyRoomCode()
        {
            string code = SessionManager.Instance != null ? SessionManager.Instance.GetJoinCode() : "";
            if (!string.IsNullOrEmpty(code))
            {
                GUIUtility.systemCopyBuffer = code;
                if (_copyFeedbackCoroutine != null) StopCoroutine(_copyFeedbackCoroutine);
                _copyFeedbackCoroutine = StartCoroutine(CopyFeedbackRoutine());
            }
        }

        private IEnumerator CopyFeedbackRoutine()
        {
            if (btnCopyCodeLabel != null) btnCopyCodeLabel.text = "COPIED!";
            yield return new WaitForSecondsRealtime(1.8f);
            if (btnCopyCodeLabel != null) btnCopyCodeLabel.text = "COPY";
        }

        private void OnSessionLeftLocally()
        {
            SceneManager.LoadScene("MainMenu");
        }

        private async void OnVisibilityToggled(bool isPublic)
        {
            if (SessionManager.Instance != null)
            {
                try
                {
                    await SessionManager.Instance.SetSessionVisibility(isPublic);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[WaitingRoomController] Visibility toggle note: {ex.Message}");
                }
            }
        }

        private void StartMatchAsHost()
        {
            if (!AreAllPlayersReady())
            {
                Debug.LogWarning("[WaitingRoomController] Cannot start match: Not all players are ready!");
                return;
            }

            if (startMatchClip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX2D(startMatchClip, 1.0f);
            }

            // Immediately close lobby locally on host
            OnMatchStarted();

            // Trigger match start on NetworkedGameManager
            if (NetworkedGameManager.Instance != null)
            {
                NetworkedGameManager.Instance.StartMatchFromLobby();
            }
        }
        public void SetLobbyVisible(bool visible)
        {
            if (_isGameOverMode)
            {
                visible = true;
            }
            else
            {
                var activeMode = NetworkedGameManager.Instance != null ? NetworkedGameManager.Instance.ActiveGameMode : GameModeBase.Instance;
                bool isNetworkMatchStarted = false;
                try
                {
                    isNetworkMatchStarted = (activeMode != null && activeMode.IsSpawned && (activeMode.MatchActive.Value || activeMode.IsInProgress)) ||
                                             (NetworkedGameManager.Instance != null && NetworkedGameManager.Instance.IsSpawned && NetworkedGameManager.Instance.MatchStarted.Value && activeMode != null && activeMode.PreMatchCountdown.Value >= 0);
                }
                catch { }

                if (_matchStarted && isNetworkMatchStarted)
                {
                    visible = false;
                }
            }

            IsLobbyMenuOpen = visible;

            if (visible)
            {
                CursorManager.PushUnlock("WaitingRoom");
                if (dimBackdrop != null) dimBackdrop.SetActive(true);
                if (lobbyModalDiv != null) lobbyModalDiv.SetActive(true);
                if (lobbyCanvasGroup != null)
                {
                    lobbyCanvasGroup.alpha = 1f;
                    lobbyCanvasGroup.blocksRaycasts = true;
                    lobbyCanvasGroup.interactable = true;
                }
                if (warmupHudBadge != null) warmupHudBadge.SetActive(false);
                RefreshLobbyUI();
            }
            else
            {
                CursorManager.PopUnlock("WaitingRoom");
                if (dimBackdrop != null) dimBackdrop.SetActive(false);
                if (lobbyModalDiv != null) lobbyModalDiv.SetActive(false);
                if (lobbyCanvasGroup != null)
                {
                    lobbyCanvasGroup.alpha = 0f;
                    lobbyCanvasGroup.blocksRaycasts = false;
                    lobbyCanvasGroup.interactable = false;
                }
                if (warmupHudBadge != null) warmupHudBadge.SetActive(!_matchStarted && !_isGameOverMode);
            }
        }

        public void ToggleLobbyVisibility()
        {
            SetLobbyVisible(!IsLobbyMenuOpen);
        }
    }
}
