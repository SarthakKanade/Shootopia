using System;
using UnityEngine;

namespace Shootopia
{
    public partial class MainMenuController
    {
private void SetQuickMode(string mode)
        {
            _quickMatchMode = mode;
            UpdateChipVisuals();
            UpdateQuickMatchDesc();
        }

        private void SetQuickMap(string map)
        {
            _quickMatchMap = map;
            UpdateChipVisuals();
            UpdateQuickMatchDesc();
        }

        private void UpdateQuickMatchDesc()
        {
            if (quickModeDescText != null)
            {
                string modeStr = _quickMatchMode == "FFA" ? "Free-For-All" : (_quickMatchMode == "TDM" ? "Team Deathmatch" : "Any Mode");
                string mapStr = _quickMatchMap == "Map1" ? "Sector Alpha" : (_quickMatchMap == "GasStationMap" ? "Gas Station" : "Any Map");
                quickModeDescText.text = $"Mode: <color=#00E5FF>{modeStr}</color>  |  Map: <color=#FFD54F>{mapStr}</color>\nFast-matchmaking into public session with selected preferences.";
            }
        }

        private async void StartQuickMatch()
        {
            CloseTopModal();
            string modeName = _quickMatchMode == "ALL" ? "ANY" : _quickMatchMode;
            string mapName = _quickMatchMap == "ALL" ? "ANY MAP" : (_quickMatchMap == "Map1" ? "SECTOR ALPHA" : "GAS STATION");
            ShowBusy(true, $"SEARCHING FOR {modeName} ON {mapName}...");

            try
            {
                EnsureLanRoomToggleInitialized();
            await MultiplayerAutoInitializer.EnsureInitializedAsync();

                if (SessionManager.Instance != null)
                {
                    var sessions = await SessionManager.Instance.GetPublicSessions();
                    var match = sessions.Find(s =>
                    {
                        // Exclude matches that have already started
                        if (s.Properties != null && s.Properties.TryGetValue("State", out var stateProp))
                        {
                            if (stateProp.Value.Equals("InProgress", StringComparison.OrdinalIgnoreCase)) return false;
                        }

                        if (_quickMatchMode != "ALL")
                        {
                            string mode = (s.Properties != null && s.Properties.ContainsKey("GameMode")) ? s.Properties["GameMode"].Value : "FFA";
                            if (!mode.Equals(_quickMatchMode, StringComparison.OrdinalIgnoreCase)) return false;
                        }

                        if (_quickMatchMap != "ALL")
                        {
                            string map = (s.Properties != null && s.Properties.ContainsKey("MapName")) ? s.Properties["MapName"].Value : "Map1";
                            if (!map.Equals(_quickMatchMap, StringComparison.OrdinalIgnoreCase)) return false;
                        }

                        return true;
                    });

                    if (match != null)
                    {
                        await SessionManager.Instance.JoinById(match.Id);
                    }
                    else
                    {
                        string modeToUse = _quickMatchMode == "ALL" ? "FFA" : _quickMatchMode;
                        string mapToUse = _quickMatchMap == "ALL" ? "Map1" : _quickMatchMap;
                        string hostName = PlayerProfileManager.Instance != null ? PlayerProfileManager.Instance.DisplayName : "Agent";
                        await SessionManager.Instance.CreateSession($"{hostName}'s Match", true, 8, modeToUse, mapToUse);
                    }
                }
                else
                {
                    ShowError("SessionManager instance not found!");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Matchmaking Failed: {ex.Message}");
            }
            finally
            {
                ShowBusy(false);
            }
        }
    }
}
