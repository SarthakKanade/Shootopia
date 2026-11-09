using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

namespace Shootopia
{
    public partial class HUDController
    {
        private void UpdateTimerAndScore()
        {
            var activeMode = NetworkedGameManager.Instance != null ? NetworkedGameManager.Instance.ActiveGameMode : GameModeBase.Instance;
            if (activeMode == null)
            {
                var ffaFallback = FindAnyObjectByType<FFAMode>();
                var tdmFallback = FindAnyObjectByType<TDMMode>();
                if (ffaFallback != null && ffaFallback.enabled) activeMode = ffaFallback;
                else if (tdmFallback != null && tdmFallback.enabled) activeMode = tdmFallback;
                else activeMode = (GameModeBase)ffaFallback ?? (GameModeBase)tdmFallback;
            }

            // 1. Handle Pre-Match Countdown (3... 2... 1... FIGHT!)
            if (activeMode != null)
            {
                int countdown = activeMode.PreMatchCountdown.Value;
                if (countdown != _lastDisplayedCountdown)
                {
                    _lastDisplayedCountdown = countdown;
                    HandleCountdownTick(countdown);
                }
            }

            bool isMatchOfficiallyStarted = _matchStartedDirectly || 
                                            (WaitingRoomController.Instance != null && WaitingRoomController.Instance.IsMatchStarted);
            try
            {
                isMatchOfficiallyStarted = isMatchOfficiallyStarted || 
                                           (NetworkedGameManager.Instance != null && NetworkedGameManager.Instance.MatchStarted.Value) ||
                                           (activeMode != null && (activeMode.MatchActive.Value || activeMode.IsInProgress));
            }
            catch { }

            // 2. Handle Match Time Display
            if (activeMode != null && (activeMode.IsInProgress || activeMode.MatchActive.Value))
            {
                float timeRemaining = activeMode.TimeRemaining;
                int totalSec = Mathf.FloorToInt(timeRemaining);
                if (totalSec != _lastRemainingSeconds && timerText != null)
                {
                    _lastRemainingSeconds = totalSec;
                    int minutes = Mathf.FloorToInt(timeRemaining / 60f);
                    int seconds = Mathf.FloorToInt(timeRemaining % 60f);
                    timerText.text = $"{minutes:00}:{seconds:00}";
                }

                if (timerText != null)
                {
                    // Pulse red when under 30 seconds
                    if (timeRemaining <= 30f && timeRemaining > 0f)
                    {
                        float pulse = (Mathf.Sin(Time.time * 8f) + 1f) * 0.5f;
                        timerText.color = Color.Lerp(Color.white, new Color(1f, 0.25f, 0.25f), pulse);
                    }
                    else if (timerText.color != Color.white)
                    {
                        timerText.color = Color.white;
                    }
                }
            }
            else if (activeMode != null && activeMode.PreMatchCountdown.Value > 0)
            {
                if (timerText != null)
                {
                    timerText.text = $"STARTING IN {activeMode.PreMatchCountdown.Value}...";
                    timerText.color = new Color(0.0f, 0.94f, 1.0f);
                }
            }
            else
            {
                if (timerText != null)
                {
                    if (isMatchOfficiallyStarted)
                    {
                        float timeRemaining = activeMode != null ? activeMode.TimeRemaining : 300f;
                        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
                        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
                        timerText.text = $"{minutes:00}:{seconds:00}";
                        timerText.color = Color.white;
                    }
                    else
                    {
                        bool isLobbyOpen = WaitingRoomController.IsLobbyMenuOpen;
                        timerText.text = isLobbyOpen ? "WARMUP" : "05:00";
                        timerText.color = Color.white;
                    }
                }
            }

            // 3. Handle Score & Placement Tracking
            UpdateScoreAndPlacement(activeMode);
        }

        private void UpdateScoreAndPlacement(GameModeBase activeMode)
        {
            if (activeMode == null)
            {
                var ffaFallback = FindAnyObjectByType<FFAMode>();
                var tdmFallback = FindAnyObjectByType<TDMMode>();
                if (ffaFallback != null && ffaFallback.enabled) activeMode = ffaFallback;
                else if (tdmFallback != null && tdmFallback.enabled) activeMode = tdmFallback;
                else activeMode = (GameModeBase)ffaFallback ?? (GameModeBase)tdmFallback;
            }

            bool isMatchOfficiallyStarted = _matchStartedDirectly || 
                                            (WaitingRoomController.Instance != null && WaitingRoomController.Instance.IsMatchStarted);
            try
            {
                isMatchOfficiallyStarted = isMatchOfficiallyStarted || 
                                           (NetworkedGameManager.Instance != null && NetworkedGameManager.Instance.MatchStarted.Value) ||
                                           (activeMode != null && (activeMode.MatchActive.Value || activeMode.IsInProgress));
            }
            catch { }

            if (activeMode == null)
            {
                if (matchLimitText != null) matchLimitText.text = "";
                if (gameModeBadgeText != null) gameModeBadgeText.text = isMatchOfficiallyStarted ? "MATCH ACTIVE" : "WARMUP";
                if (tdmScoreRoot != null) tdmScoreRoot.SetActive(false);
                if (ffaScoreRoot != null) ffaScoreRoot.SetActive(false);
                if (placementText != null) placementText.text = "";
                if (scoreText != null) scoreText.text = "";
                return;
            }

            ulong localId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0;

            if (activeMode is TDMMode tdm)
            {
                if (tdmScoreRoot != null && !tdmScoreRoot.activeSelf) tdmScoreRoot.SetActive(true);
                if (ffaScoreRoot != null && ffaScoreRoot.activeSelf) ffaScoreRoot.SetActive(false);

                int blue = tdm.ScoreBlue;
                int red = tdm.ScoreRed;
                int limit = (tdm != null && tdm.TargetScoreLimit.Value > 0) ? tdm.TargetScoreLimit.Value : 15;
                int localTeam = tdm.GetPlayerTeam(localId);

                if (blue != _lastBlueScore || red != _lastRedScore)
                {
                    _lastBlueScore = blue;
                    _lastRedScore = red;

                    if (teamBlueScoreText != null) teamBlueScoreText.text = blue.ToString();
                    if (teamRedScoreText != null) teamRedScoreText.text = red.ToString();

                    if (teamBlueScoreBar != null) teamBlueScoreBar.fillAmount = Mathf.Clamp01((float)blue / limit);
                    if (teamRedScoreBar != null) teamRedScoreBar.fillAmount = Mathf.Clamp01((float)red / limit);

                    if (teamBlueNameText != null)
                    {
                        teamBlueNameText.text = (localTeam == 0) ? "ALLIES <size=11><color=#00F0FF>[YOU]</color></size>" : "BLUE";
                    }
                    if (teamRedNameText != null)
                    {
                        teamRedNameText.text = (localTeam == 1) ? "AXIS <size=11><color=#FF4444>[YOU]</color></size>" : "RED";
                    }

                    if (matchLimitText != null) matchLimitText.text = $"FIRST TO {limit}";
                    if (gameModeBadgeText != null) gameModeBadgeText.text = "TEAM DEATHMATCH";

                    if (placementText != null) placementText.text = "TDM";
                    if (scoreText != null) scoreText.text = $"<color=#00F0FF>{blue}</color> <size=13>—</size> <color=#FF4444>{red}</color>";
                }
            }
            else if (activeMode is FFAMode ffa)
            {
                if (tdmScoreRoot != null && tdmScoreRoot.activeSelf) tdmScoreRoot.SetActive(false);
                if (ffaScoreRoot != null && !ffaScoreRoot.activeSelf) ffaScoreRoot.SetActive(true);

                int kills = ffa.GetPlayerKills(localId);
                int rank = ffa.GetPlayerRank(localId);
                int limit = (ffa != null && ffa.TargetScoreLimit.Value > 0) ? ffa.TargetScoreLimit.Value : 10;

                if (kills != _lastFFAKills || rank != _lastFFARank)
                {
                    _lastFFAKills = kills;
                    _lastFFARank = rank;
                    int totalPlayers = NetworkManager.Singleton != null ? NetworkManager.Singleton.ConnectedClientsList.Count : 1;

                    if (placementText != null)
                    {
                        placementText.text = $"#{rank} <size=12><color=#8A99B5>/ {totalPlayers}</color></size>";
                    }

                    if (scoreText != null)
                    {
                        scoreText.text = $"<color=#00F0FF>{kills}</color> <size=12><color=#8A99B5>/ {limit} KILLS</color></size>";
                    }

                    if (matchLimitText != null) matchLimitText.text = $"FIRST TO {limit}";
                    if (gameModeBadgeText != null) gameModeBadgeText.text = "FREE FOR ALL";
                }
            }
            else
            {
                if (tdmScoreRoot != null) tdmScoreRoot.SetActive(false);
                if (ffaScoreRoot != null) ffaScoreRoot.SetActive(false);
                if (gameModeBadgeText != null) gameModeBadgeText.text = isMatchOfficiallyStarted ? "MATCH ACTIVE" : "WARMUP";
                if (matchLimitText != null) matchLimitText.text = "";
            }
        }

    }
}
