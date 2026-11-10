using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Multiplayer;

namespace Shootopia
{
    public partial class MainMenuController
    {
        private void OpenCustomRooms()
        {
            OpenModal(modalCustomRooms, customRoomsGroup);
            SwitchCustomRoomsTab(true); // Default to Create tab or Join tab
        }

        public void SwitchCustomRoomsTab(bool isCreateTab)
        {
            if (tabContentCreate != null) tabContentCreate.SetActive(isCreateTab);
            if (tabContentJoin != null) tabContentJoin.SetActive(!isCreateTab);

            if (tabBtnCreate != null)
            {
                var img = tabBtnCreate.GetComponent<Image>();
                if (img != null) img.color = isCreateTab ? new Color(1f, 0.9f, 0.4f, 1f) : new Color(0.6f, 0.6f, 0.7f, 0.8f);
            }
            if (tabBtnJoin != null)
            {
                var img = tabBtnJoin.GetComponent<Image>();
                if (img != null) img.color = !isCreateTab ? new Color(1f, 0.9f, 0.4f, 1f) : new Color(0.6f, 0.6f, 0.7f, 0.8f);
            }

            if (isCreateTab)
            {
                EnsureLanRoomToggleInitialized();
            }
            else
            {
                RefreshRoomListAsync();
            }
        }

        // ── CREATE ROOM TAB ──

        private void SetCreateMode(string mode)
        {
            _selectedCreateMode = mode;
            if (!_hasCustomizedScore)
            {
                _createScoreLimit = (mode == "TDM") ? defaultTdmScoreLimit : defaultFfaScoreLimit;
                if (sliderScoreLimit != null) sliderScoreLimit.value = _createScoreLimit;
            }
            if (!_hasCustomizedDuration)
            {
                _createDurationMinutes = (mode == "TDM") ? defaultTdmDurationMinutes : defaultFfaDurationMinutes;
                if (sliderMatchDuration != null) sliderMatchDuration.value = _createDurationMinutes;
            }
            UpdateScoreLimitDisplay();
            UpdateDurationDisplay();
            UpdateChipVisuals();
        }

        private void SetCreateMap(string mapName)
        {
            _selectedCreateMap = mapName;
            UpdateChipVisuals();
        }

        private async void ConfirmCreateRoom()
        {
            string hostName = PlayerProfileManager.Instance != null ? PlayerProfileManager.Instance.DisplayName : "Agent";
            string roomName = inputCreateRoomName != null && !string.IsNullOrWhiteSpace(inputCreateRoomName.text)
                ? inputCreateRoomName.text.Trim()
                : $"{hostName}'s Arena";

            int maxPlayers = _createMaxPlayers;
            int matchDurationSeconds = _createDurationMinutes * 60;
            int scoreLimit = _createScoreLimit;
            bool isPublic = togglePublicRoom == null || togglePublicRoom.isOn;
            bool isLan = toggleLanRoom != null && toggleLanRoom.gameObject.activeInHierarchy && toggleLanRoom.isOn;

            CloseTopModal();
            ShowBusy(true, isLan ? "STARTING LOCAL WI-FI ARENA..." : "INITIALIZING ARENA...");

            try
            {
                if (isLan)
                {
                    await SessionManager.Instance.StartLanSession(roomName, maxPlayers, _selectedCreateMode, _selectedCreateMap, matchDurationSeconds, scoreLimit);
                    return;
                }

                EnsureLanRoomToggleInitialized();
                bool authReady = await MultiplayerAutoInitializer.EnsureInitializedAsync();
                if (!authReady || !MultiplayerAutoInitializer.IsAuthenticated)
                {
                    ShowError("Unable to connect to online game services. Please check your internet connection.");
                    return;
                }
                await SessionManager.Instance.CreateSession(roomName, isPublic, maxPlayers, _selectedCreateMode, _selectedCreateMap, matchDurationSeconds, scoreLimit);
            }
            catch (Exception ex)
            {
                ShowError($"Failed to create room: {ex.Message}");
            }
            finally
            {
                ShowBusy(false);
            }
        }

        private void InitRoomCreationDefaults()
        {
            if (createRoomConfig == null && tabContentCreate != null)
            {
                createRoomConfig = tabContentCreate.GetComponent<CustomRoomCreateConfig>();
            }

            if (createRoomConfig != null)
            {
                defaultFfaDurationMinutes = createRoomConfig.defaultFfaDuration;
                defaultFfaScoreLimit = createRoomConfig.defaultFfaScore;
                defaultTdmDurationMinutes = createRoomConfig.defaultTdmDuration;
                defaultTdmScoreLimit = createRoomConfig.defaultTdmScore;
                minDurationMinutes = createRoomConfig.minDuration;
                maxDurationMinutes = createRoomConfig.maxDuration;
                durationStepMinutes = createRoomConfig.durationStep;
                minScoreLimit = createRoomConfig.minScore;
                maxScoreLimit = createRoomConfig.maxScore;
                scoreLimitStep = createRoomConfig.scoreStep;
                defaultMaxPlayers = createRoomConfig.defaultPlayers;
                minPlayers = createRoomConfig.minPlayers;
                maxPlayersLimit = createRoomConfig.maxPlayers;
                playerStep = createRoomConfig.playerStep;
                if (!string.IsNullOrEmpty(createRoomConfig.defaultMode)) _selectedCreateMode = createRoomConfig.defaultMode;
                if (!string.IsNullOrEmpty(createRoomConfig.defaultMap)) _selectedCreateMap = createRoomConfig.defaultMap;
            }

            _createMaxPlayers = defaultMaxPlayers;
            _createDurationMinutes = (_selectedCreateMode == "TDM") ? defaultTdmDurationMinutes : defaultFfaDurationMinutes;
            _createScoreLimit = (_selectedCreateMode == "TDM") ? defaultTdmScoreLimit : defaultFfaScoreLimit;

            if (sliderMaxPlayers != null)
            {
                sliderMaxPlayers.minValue = minPlayers;
                sliderMaxPlayers.maxValue = maxPlayersLimit;
                sliderMaxPlayers.wholeNumbers = true;
                sliderMaxPlayers.value = _createMaxPlayers;
            }
            UpdateMaxPlayersDisplay();

            if (sliderMatchDuration != null)
            {
                sliderMatchDuration.minValue = minDurationMinutes;
                sliderMatchDuration.maxValue = maxDurationMinutes;
                sliderMatchDuration.wholeNumbers = true;
                sliderMatchDuration.value = _createDurationMinutes;
            }
            UpdateDurationDisplay();

            if (sliderScoreLimit != null)
            {
                sliderScoreLimit.minValue = minScoreLimit;
                sliderScoreLimit.maxValue = maxScoreLimit;
                sliderScoreLimit.wholeNumbers = true;
                sliderScoreLimit.value = _createScoreLimit;
            }
            UpdateScoreLimitDisplay();
        }
    }
}
