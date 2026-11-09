using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Multiplayer;

namespace Shootopia
{
    public partial class MainMenuController
    {
// ── JOIN ROOM TAB ──

        private async void SubmitJoinCode()
        {
            if (inputJoinCode == null || string.IsNullOrWhiteSpace(inputJoinCode.text))
            {
                ShowError("Please enter a valid room code.");
                return;
            }

            string code = inputJoinCode.text.Trim().ToUpper();
            ShowBusy(true, $"CONNECTING TO CODE {code}...");

            try
            {
                // First check local Wi-Fi subnet for direct UDP LAN Host
                var lanInfo = await LanDiscoveryManager.FindRoomByCodeAsync(code, timeoutMs: 1500);
                if (lanInfo != null)
                {
                    ShowBusy(true, $"CONNECTING TO LOCAL WI-FI ARENA ({lanInfo.HostIP})...");
                    await SessionManager.Instance.JoinLanSessionByCode(code);
                    return;
                }

                // Fallback to UGS Relay / Internet
                EnsureLanRoomToggleInitialized();
                bool authReady = await MultiplayerAutoInitializer.EnsureInitializedAsync();
                if (!authReady || !MultiplayerAutoInitializer.IsAuthenticated)
                {
                    ShowError("Unable to connect to online game services. Please check your internet connection.");
                    return;
                }
                await SessionManager.Instance.JoinByCode(code);
            }
            catch (Exception ex)
            {
                ShowError($"Failed to join room: {ex.Message}");
            }
            finally
            {
                ShowBusy(false);
            }
        }

        private void SetBrowserFilter(string filter)
        {
            _browserModeFilter = filter;
            UpdateChipVisuals();
            PopulateRoomCards();
        }

        private async void RefreshRoomListAsync()
        {
            if (btnRefreshRooms != null) btnRefreshRooms.interactable = false;

            try
            {
                EnsureLanRoomToggleInitialized();
            await MultiplayerAutoInitializer.EnsureInitializedAsync();
                if (SessionManager.Instance != null)
                {
                    _cachedSessions = await SessionManager.Instance.GetPublicSessions();
                    PopulateRoomCards();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MainMenuController] Query sessions note: {ex.Message}");
            }
            finally
            {
                if (btnRefreshRooms != null) btnRefreshRooms.interactable = true;
            }
        }

        private void PopulateRoomCards()
        {
            if (roomListContent == null) return;

            foreach (Transform child in roomListContent)
            {
                Destroy(child.gameObject);
            }

            var filtered = _cachedSessions.FindAll(s =>
            {
                if (_browserModeFilter == "ALL") return true;
                string mode = (s.Properties != null && s.Properties.ContainsKey("GameMode")) ? s.Properties["GameMode"].Value : "FFA";
                return mode.Equals(_browserModeFilter, StringComparison.OrdinalIgnoreCase);
            });

            if (browseEmptyState != null)
            {
                browseEmptyState.SetActive(filtered.Count == 0);
            }

            foreach (var session in filtered)
            {
                CreateRoomCard(session);
            }
        }

        private void CreateRoomCard(ISessionInfo session)
        {
            if (roomCardPrefab == null || roomListContent == null) return;

            var card = Instantiate(roomCardPrefab, roomListContent);
            var texts = card.GetComponentsInChildren<TextMeshProUGUI>();
            var joinBtn = card.GetComponentInChildren<Button>();

            string roomName = session.Name;
            string mode = (session.Properties != null && session.Properties.ContainsKey("GameMode")) ? session.Properties["GameMode"].Value : "FFA";
            string map = (session.Properties != null && session.Properties.ContainsKey("MapName")) ? session.Properties["MapName"].Value : "Map1";
            string mapDisplay = MapRegistry.GetDisplayName(map);
            string state = (session.Properties != null && session.Properties.ContainsKey("State")) ? session.Properties["State"].Value : "Lobby";
            bool isInProgress = state.Equals("InProgress", StringComparison.OrdinalIgnoreCase);
            int maxPlayers = session.MaxPlayers;

            string durationStr = "";
            if (session.Properties != null && session.Properties.TryGetValue("MatchDuration", out var durProp) && int.TryParse(durProp.Value, out int durSec) && durSec > 0)
            {
                durationStr = $" • {durSec / 60}m";
            }
            string scoreStr = "";
            if (session.Properties != null && session.Properties.TryGetValue("ScoreLimit", out var scoreProp) && int.TryParse(scoreProp.Value, out int scoreGoal) && scoreGoal > 0)
            {
                scoreStr = $" • {scoreGoal}pts";
            }

            if (texts.Length > 0)
            {
                texts[0].text = roomName.StartsWith("LAN", StringComparison.OrdinalIgnoreCase)
                    ? $"<color=#00E676>[LAN]</color> {roomName}"
                    : roomName;
            }
            if (texts.Length > 1) texts[1].text = $"{mode} • {mapDisplay}{durationStr}{scoreStr} • MAX {maxPlayers}P{(isInProgress ? " • [IN MATCH]" : "")}";

            if (joinBtn != null)
            {
                joinBtn.onClick.AddListener(async () =>
                {
                    ShowBusy(true, $"JOINING {roomName}...");
                    try
                    {
                        await SessionManager.Instance.JoinById(session.Id);
                    }
                    catch (Exception ex)
                    {
                        ShowError($"Failed to join: {ex.Message}");
                    }
                    finally
                    {
                        ShowBusy(false);
                    }
                });
            }
        }
    }
}
