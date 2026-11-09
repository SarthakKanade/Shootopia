using UnityEngine;

namespace Shootopia
{
    public partial class MainMenuController
    {
private void RegisterCallbacks()
        {
            if (btnEditProfile != null) btnEditProfile.onClick.AddListener(() => OpenModal(modalEditProfile, editProfileGroup));
            if (btnToggleSound != null) btnToggleSound.onClick.AddListener(ToggleSound);
            if (btnTopSettings != null) btnTopSettings.onClick.AddListener(() => OpenModal(modalSettings, settingsGroup));
            if (btnTopRegion != null) btnTopRegion.onClick.AddListener(() => OpenModal(modalRegionSelect, regionSelectGroup));

            if (btnPlayMatch != null) btnPlayMatch.onClick.AddListener(() => OpenModal(modalQuickFilter, quickFilterGroup));
            if (btnCustomRooms != null) btnCustomRooms.onClick.AddListener(OpenCustomRooms);
            if (btnCustomize != null) btnCustomize.onClick.AddListener(() => OpenModal(modalCustomize, customizeGroup));
            if (btnSettings != null) btnSettings.onClick.AddListener(() => OpenModal(modalSettings, settingsGroup));
            if (btnQuit != null) btnQuit.onClick.AddListener(QuitGame);

            // Quick Filter Modal
            if (btnCloseQuickFilter != null) btnCloseQuickFilter.onClick.AddListener(CloseTopModal);
            if (chipQuickAll != null) chipQuickAll.onClick.AddListener(() => SetQuickMode("ALL"));
            if (chipQuickFFA != null) chipQuickFFA.onClick.AddListener(() => SetQuickMode("FFA"));
            if (chipQuickTDM != null) chipQuickTDM.onClick.AddListener(() => SetQuickMode("TDM"));
            if (chipQuickMapAll != null) chipQuickMapAll.onClick.AddListener(() => SetQuickMap("ALL"));
            if (chipQuickMapSectorAlpha != null) chipQuickMapSectorAlpha.onClick.AddListener(() => SetQuickMap("Map1"));
            if (chipQuickMapGasStation != null) chipQuickMapGasStation.onClick.AddListener(() => SetQuickMap("GasStationMap"));
            if (btnConfirmQuickPlay != null) btnConfirmQuickPlay.onClick.AddListener(StartQuickMatch);

            // Custom Rooms Modal
            if (btnCloseCustomRooms != null) btnCloseCustomRooms.onClick.AddListener(CloseTopModal);
            if (tabBtnCreate != null) tabBtnCreate.onClick.AddListener(() => SwitchCustomRoomsTab(true));
            if (tabBtnJoin != null) tabBtnJoin.onClick.AddListener(() => SwitchCustomRoomsTab(false));

            // Create Tab
            if (chipModeFFA != null) chipModeFFA.onClick.AddListener(() => SetCreateMode("FFA"));
            if (chipModeTDM != null) chipModeTDM.onClick.AddListener(() => SetCreateMode("TDM"));
            if (chipMapSectorAlpha != null) chipMapSectorAlpha.onClick.AddListener(() => SetCreateMap("Map1"));
            if (chipMapGasStation != null) chipMapGasStation.onClick.AddListener(() => SetCreateMap("GasStationMap"));

            if (sliderMaxPlayers != null) sliderMaxPlayers.onValueChanged.AddListener(OnMaxPlayersChanged);
            if (btnDecreasePlayers != null) btnDecreasePlayers.onClick.AddListener(() => AdjustMaxPlayers(-1));
            if (btnIncreasePlayers != null) btnIncreasePlayers.onClick.AddListener(() => AdjustMaxPlayers(1));
            if (inputMaxPlayers != null) inputMaxPlayers.onEndEdit.AddListener(OnMaxPlayersInputEndEdit);

            if (sliderMatchDuration != null) sliderMatchDuration.onValueChanged.AddListener(OnDurationChanged);
            if (btnDecreaseDuration != null) btnDecreaseDuration.onClick.AddListener(() => AdjustDuration(-1));
            if (btnIncreaseDuration != null) btnIncreaseDuration.onClick.AddListener(() => AdjustDuration(1));
            if (inputMatchDuration != null) inputMatchDuration.onEndEdit.AddListener(OnDurationInputEndEdit);

            if (sliderScoreLimit != null) sliderScoreLimit.onValueChanged.AddListener(OnScoreLimitChanged);
            if (btnDecreaseScore != null) btnDecreaseScore.onClick.AddListener(() => AdjustScoreLimit(-1));
            if (btnIncreaseScore != null) btnIncreaseScore.onClick.AddListener(() => AdjustScoreLimit(1));
            if (inputScoreLimit != null) inputScoreLimit.onEndEdit.AddListener(OnScoreLimitInputEndEdit);

            if (btnConfirmCreate != null) btnConfirmCreate.onClick.AddListener(ConfirmCreateRoom);

            // Join Tab
            if (btnSubmitJoinCode != null) btnSubmitJoinCode.onClick.AddListener(SubmitJoinCode);
            if (chipFilterAll != null) chipFilterAll.onClick.AddListener(() => SetBrowserFilter("ALL"));
            if (chipFilterFFA != null) chipFilterFFA.onClick.AddListener(() => SetBrowserFilter("FFA"));
            if (chipFilterTDM != null) chipFilterTDM.onClick.AddListener(() => SetBrowserFilter("TDM"));
            if (btnRefreshRooms != null) btnRefreshRooms.onClick.AddListener(RefreshRoomListAsync);

            // Customize Modal
            if (btnCloseCustomize != null) btnCloseCustomize.onClick.AddListener(CloseTopModal);
            if (btnSkinAgent != null) btnSkinAgent.onClick.AddListener(() => SelectSkin(0));
            if (btnSkinRobot != null) btnSkinRobot.onClick.AddListener(() => SelectSkin(1));

            // Settings Modal
            if (btnCloseSettings != null) btnCloseSettings.onClick.AddListener(CloseTopModal);
            if (sliderMasterVol != null) sliderMasterVol.onValueChanged.AddListener(OnMasterVolChanged);
            if (sliderMusicVol != null) sliderMusicVol.onValueChanged.AddListener(OnMusicVolChanged);
            if (sliderSFXVol != null) sliderSFXVol.onValueChanged.AddListener(OnSFXVolChanged);
            if (sliderMouseSens != null) sliderMouseSens.onValueChanged.AddListener(OnMouseSensChanged);
            if (sliderFOV != null) sliderFOV.onValueChanged.AddListener(OnFOVChanged);
            if (toggleInvertY != null) toggleInvertY.onValueChanged.AddListener(OnInvertYChanged);
            if (toggleFullscreen != null) toggleFullscreen.onValueChanged.AddListener(OnFullscreenChanged);

            if (switchInvertY != null) switchInvertY.OnValueChanged += OnInvertYChanged;
            if (switchFullscreen != null) switchFullscreen.OnValueChanged += OnFullscreenChanged;
            if (btnMinusSens != null) btnMinusSens.onClick.AddListener(() => AdjustSensitivity(-0.2f));
            if (btnPlusSens != null) btnPlusSens.onClick.AddListener(() => AdjustSensitivity(+0.2f));
            if (btnActionReset != null) btnActionReset.onClick.AddListener(ResetSettingsToDefaults);
            if (btnActionControls != null) btnActionControls.onClick.AddListener(OpenControlsModal);
            if (btnActionApply != null) btnActionApply.onClick.AddListener(CloseTopModal);

            // Controls Reference Modal
            if (btnCloseControls != null) btnCloseControls.onClick.AddListener(CloseTopModal);

            // Edit Profile Modal
            if (btnCloseProfile != null) btnCloseProfile.onClick.AddListener(CloseTopModal);
            if (btnSaveProfile != null) btnSaveProfile.onClick.AddListener(SaveProfile);

            // Server Region Modal
            if (btnCloseRegionSelect != null) btnCloseRegionSelect.onClick.AddListener(CloseTopModal);
            if (chipRegionAuto != null) chipRegionAuto.onClick.AddListener(() => SelectRegion("auto"));
            if (chipRegionIndia != null) chipRegionIndia.onClick.AddListener(() => SelectRegion("in"));
            if (chipRegionAsia != null) chipRegionAsia.onClick.AddListener(() => SelectRegion("asia"));
            if (chipRegionEurope != null) chipRegionEurope.onClick.AddListener(() => SelectRegion("eu"));
            if (chipRegionUSEast != null) chipRegionUSEast.onClick.AddListener(() => SelectRegion("us"));

            // Busy & Error
            if (btnCancelBusy != null) btnCancelBusy.onClick.AddListener(CancelBusy);
            if (btnDismissError != null) btnDismissError.onClick.AddListener(CloseTopModal);
        }

        private void UnregisterCallbacks()
        {
            // Reset modal stack
            _modalStack.Clear();
        }
    }
}
