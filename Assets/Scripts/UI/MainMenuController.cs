using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Multiplayer;

namespace Shootopia
{
    /// <summary>
    /// Master UGUI UI Controller for the Shootopia Main Menu.
    /// Handles Quick Match filter & play, Custom Rooms (Create/Join/Code), Settings,
    /// 3D Character Customization, and responsive audio/network feedback.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Audio Feedback")]
        [SerializeField] private AudioClip buttonClickClip;
        [SerializeField] private AudioClip errorClip;
        [SerializeField] private AudioClip menuMusicClip;

        [Header("Top Header & Profile")]
        [SerializeField] private TextMeshProUGUI playerNameLabel;
        [SerializeField] private Button btnEditProfile;
        [SerializeField] private Button btnToggleSound;
        [SerializeField] private Image soundIconImage;
        [SerializeField] private Sprite soundOnSprite;
        [SerializeField] private Sprite soundOffSprite;
        [SerializeField] private Button btnTopSettings;

        [Header("Main Action Hub")]
        [SerializeField] private Button btnPlayMatch;
        [SerializeField] private Button btnCustomRooms;
        [SerializeField] private Button btnCustomize;
        [SerializeField] private Button btnSettings;
        [SerializeField] private Button btnQuit;

        [Header("Modal: Quick Match Filter")]
        [SerializeField] private GameObject modalQuickFilter;
        [SerializeField] private CanvasGroup quickFilterGroup;
        [SerializeField] private Button btnCloseQuickFilter;
        [SerializeField] private Button chipQuickAll, chipQuickFFA, chipQuickTDM;
        [SerializeField] private Button chipQuickMapAll, chipQuickMapSectorAlpha, chipQuickMapGasStation;
        [SerializeField] private TextMeshProUGUI quickModeDescText;
        [SerializeField] private Button btnConfirmQuickPlay;

        [Header("Modal: Custom Rooms Hub")]
        [SerializeField] private GameObject modalCustomRooms;
        [SerializeField] private CanvasGroup customRoomsGroup;
        [SerializeField] private Button btnCloseCustomRooms;
        [SerializeField] private Button tabBtnCreate, tabBtnJoin;
        [SerializeField] private GameObject tabContentCreate, tabContentJoin;

        [Header("Custom Rooms: Create Tab")]
        [SerializeField] private CustomRoomCreateConfig createRoomConfig;
        [SerializeField] private TMP_InputField inputCreateRoomName;
        [SerializeField] private Button chipModeFFA, chipModeTDM;
        [SerializeField] private Button chipMapSectorAlpha, chipMapGasStation;
        [SerializeField] private Slider sliderMaxPlayers;
        [SerializeField] private Button btnDecreasePlayers;
        [SerializeField] private Button btnIncreasePlayers;
        [SerializeField] private TextMeshProUGUI labelMaxPlayers;
        [SerializeField] private TextMeshProUGUI labelMaxPlayersUnit;
        [SerializeField] private TMP_InputField inputMaxPlayers;
        [SerializeField] private Toggle togglePublicRoom;
        [SerializeField] private Button btnConfirmCreate;

        [Header("Custom Rooms: Match Rules")]
        [SerializeField] private Slider sliderMatchDuration;
        [SerializeField] private Button btnDecreaseDuration;
        [SerializeField] private Button btnIncreaseDuration;
        [SerializeField] private TextMeshProUGUI labelMatchDuration;
        [SerializeField] private TextMeshProUGUI labelDurationUnit;
        [SerializeField] private TMP_InputField inputMatchDuration;

        [SerializeField] private Slider sliderScoreLimit;
        [SerializeField] private Button btnDecreaseScore;
        [SerializeField] private Button btnIncreaseScore;
        [SerializeField] private TextMeshProUGUI labelScoreLimit;
        [SerializeField] private TextMeshProUGUI labelScoreLimitUnit;
        [SerializeField] private TMP_InputField inputScoreLimit;

        [Header("Custom Rooms: Rules Configuration (Inspector Editable)")]
        [Tooltip("Default match duration in minutes for Free-For-All mode.")]
        [SerializeField] private int defaultFfaDurationMinutes = 5;

        [Tooltip("Default score/kill limit for Free-For-All mode.")]
        [SerializeField] private int defaultFfaScoreLimit = 10;

        [Tooltip("Default match duration in minutes for Team Deathmatch mode.")]
        [SerializeField] private int defaultTdmDurationMinutes = 7;

        [Tooltip("Default score/kill limit for Team Deathmatch mode.")]
        [SerializeField] private int defaultTdmScoreLimit = 15;

        [Tooltip("Minimum match duration in minutes.")]
        [SerializeField] private int minDurationMinutes = 1;

        [Tooltip("Maximum match duration in minutes.")]
        [SerializeField] private int maxDurationMinutes = 30;

        [Tooltip("Duration adjustment step in minutes for +/- buttons.")]
        [SerializeField] private int durationStepMinutes = 1;

        [Tooltip("Minimum score/kill limit.")]
        [SerializeField] private int minScoreLimit = 5;

        [Tooltip("Maximum score/kill limit.")]
        [SerializeField] private int maxScoreLimit = 100;

        [Tooltip("Score adjustment step for +/- buttons.")]
        [SerializeField] private int scoreLimitStep = 5;

        [Tooltip("Default max players for custom rooms.")]
        [SerializeField] private int defaultMaxPlayers = 8;

        [Tooltip("Minimum player limit for custom rooms.")]
        [SerializeField] private int minPlayers = 2;

        [Tooltip("Maximum player limit for custom rooms.")]
        [SerializeField] private int maxPlayersLimit = 12;

        [Tooltip("Player adjustment step for +/- buttons.")]
        [SerializeField] private int playerStep = 1;

        [Header("Custom Rooms: Join Tab (Browser & Code)")]
        [SerializeField] private TMP_InputField inputJoinCode;
        [SerializeField] private Button btnSubmitJoinCode;
        [SerializeField] private Button chipFilterAll, chipFilterFFA, chipFilterTDM;
        [SerializeField] private Button btnRefreshRooms;
        [SerializeField] private Transform roomListContent;
        [SerializeField] private GameObject roomCardPrefab;
        [SerializeField] private GameObject browseEmptyState;

        [Header("Modal: Customize")]
        [SerializeField] private GameObject modalCustomize;
        [SerializeField] private CanvasGroup customizeGroup;
        [SerializeField] private Button btnCloseCustomize;
        [SerializeField] private Button btnSkinAgent, btnSkinRobot;

        [Header("Modal: Settings")]
        [SerializeField] private GameObject modalSettings;
        [SerializeField] private CanvasGroup settingsGroup;
        [SerializeField] private Button btnCloseSettings;
        [SerializeField] private Slider sliderMasterVol, sliderMusicVol, sliderSFXVol, sliderMouseSens;
        [SerializeField] private TextMeshProUGUI labelMasterVol, labelMusicVol, labelSFXVol, labelMouseSens;
        [SerializeField] private Slider sliderFOV;
        [SerializeField] private TextMeshProUGUI labelFOV;
        [SerializeField] private Toggle toggleInvertY, toggleFullscreen;
        [SerializeField] private SpaceGUISwitch switchInvertY, switchFullscreen;
        [SerializeField] private Button btnMinusSens, btnPlusSens;
        [SerializeField] private Button btnActionReset, btnActionControls, btnActionApply;

        [Header("Modal: Controls Reference Sheet")]
        [SerializeField] private GameObject modalControls;
        [SerializeField] private CanvasGroup controlsGroup;
        [SerializeField] private Button btnCloseControls;

        [Header("Modal: Edit Profile")]
        [SerializeField] private GameObject modalEditProfile;
        [SerializeField] private CanvasGroup editProfileGroup;
        [SerializeField] private Button btnCloseProfile;
        [SerializeField] private TMP_InputField inputDisplayName;
        [SerializeField] private Button btnSaveProfile;

        [Header("Busy / Matchmaking Overlay")]
        [SerializeField] private GameObject busyOverlay;
        [SerializeField] private CanvasGroup busyGroup;
        [SerializeField] private TextMeshProUGUI busyLabel;
        [SerializeField] private Button btnCancelBusy;
        [SerializeField] private Transform spinnerIcon;

        [Header("Error Toast / Alert Modal")]
        [SerializeField] private GameObject errorModal;
        [SerializeField] private CanvasGroup errorGroup;
        [SerializeField] private TextMeshProUGUI errorLabel;
        [SerializeField] private Button btnDismissError;

        // ── State Variables ──
        private string _selectedCreateMode = "FFA";
        private string _selectedCreateMap = "Map1";
        private string _quickMatchMode = "ALL";
        private string _quickMatchMap = "ALL";
        private string _browserModeFilter = "ALL";
        private int _selectedSkin = 0;
        private int _createMaxPlayers = 8;
        private int _createDurationMinutes = 5;
        private int _createScoreLimit = 10;
        private bool _hasCustomizedDuration = false;
        private bool _hasCustomizedScore = false;
        private bool _isMuted = false;
        private bool _isBusy = false;
        private readonly Stack<GameObject> _modalStack = new Stack<GameObject>();
        private List<ISessionInfo> _cachedSessions = new List<ISessionInfo>();

        private void OnEnable()
        {
            CursorManager.PushUnlock("MainMenu");
            RefreshProfileDisplay();

            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.OnSessionError.AddListener(OnSessionError);
            }

            RegisterCallbacks();
        }

        private async void Start()
        {
            if (menuMusicClip == null && AudioManager.Instance != null)
            {
                menuMusicClip = AudioManager.Instance.MenuMusicClip;
            }
            if (menuMusicClip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMusic(menuMusicClip, 0.8f);
            }

            InitSettingsValues();
            UpdateChipVisuals();

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

            await MultiplayerAutoInitializer.EnsureInitializedAsync();
        }

        private void OnDisable()
        {
            CursorManager.PopUnlock("MainMenu");

            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.OnSessionError.RemoveListener(OnSessionError);
            }

            UnregisterCallbacks();
        }

        private void Update()
        {
            if (_isBusy && spinnerIcon != null)
            {
                spinnerIcon.Rotate(0f, 0f, -180f * Time.unscaledDeltaTime);
            }

            if (Keyboard.current != null)
            {
                if (Keyboard.current.cKey.wasPressedThisFrame || Keyboard.current.oKey.wasPressedThisFrame)
                {
                    if (modalCustomize != null)
                    {
                        if (modalCustomize.activeSelf) CloseTopModal();
                        else OpenModal(modalCustomize, customizeGroup);
                    }
                }
                else if (Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    if (_modalStack.Count > 0)
                    {
                        CloseTopModal();
                    }
                }
            }
        }

        // ──────────────────────── CALLBACK REGISTRATION ────────────────────────

        private void RegisterCallbacks()
        {
            if (btnEditProfile != null) btnEditProfile.onClick.AddListener(() => OpenModal(modalEditProfile, editProfileGroup));
            if (btnToggleSound != null) btnToggleSound.onClick.AddListener(ToggleSound);
            if (btnTopSettings != null) btnTopSettings.onClick.AddListener(() => OpenModal(modalSettings, settingsGroup));

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

            // Busy & Error
            if (btnCancelBusy != null) btnCancelBusy.onClick.AddListener(CancelBusy);
            if (btnDismissError != null) btnDismissError.onClick.AddListener(CloseTopModal);
        }

        private void UnregisterCallbacks()
        {
            // Reset modal stack
            _modalStack.Clear();
        }

        // ──────────────────────── PROFILE & HEADER ────────────────────────

        public void RefreshProfileDisplay()
        {
            string pName = PlayerProfileManager.Instance != null ? PlayerProfileManager.Instance.DisplayName : "Agent";
            if (playerNameLabel != null) playerNameLabel.text = pName;
            if (inputDisplayName != null) inputDisplayName.text = pName;
        }

        private void SaveProfile()
        {
            if (inputDisplayName != null && !string.IsNullOrWhiteSpace(inputDisplayName.text))
            {
                if (PlayerProfileManager.Instance != null)
                {
                    PlayerProfileManager.Instance.SetDisplayName(inputDisplayName.text.Trim());
                }
                RefreshProfileDisplay();
            }
            CloseTopModal();
        }

        private void ToggleSound()
        {
            _isMuted = !_isMuted;
            AudioListener.pause = _isMuted;
            if (soundIconImage != null)
            {
                soundIconImage.sprite = _isMuted ? soundOffSprite : soundOnSprite;
            }
        }

        // ──────────────────────── QUICK MATCHMAKING ────────────────────────

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

        // ──────────────────────── CUSTOM ROOMS HUB ────────────────────────

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

            if (!isCreateTab)
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

        private void AdjustMaxPlayers(int delta)
        {
            _createMaxPlayers = Mathf.Clamp(_createMaxPlayers + (delta * playerStep), minPlayers, maxPlayersLimit);
            if (sliderMaxPlayers != null && Mathf.RoundToInt(sliderMaxPlayers.value) != _createMaxPlayers)
            {
                sliderMaxPlayers.value = _createMaxPlayers;
            }
            UpdateMaxPlayersDisplay();
        }

        private void OnMaxPlayersChanged(float val)
        {
            _createMaxPlayers = Mathf.Clamp(Mathf.RoundToInt(val), minPlayers, maxPlayersLimit);
            UpdateMaxPlayersDisplay();
        }

        private void OnMaxPlayersInputEndEdit(string text)
        {
            if (int.TryParse(text, out int val) && val > 0)
            {
                _createMaxPlayers = Mathf.Clamp(val, minPlayers, maxPlayersLimit);
                if (sliderMaxPlayers != null) sliderMaxPlayers.value = _createMaxPlayers;
            }
            UpdateMaxPlayersDisplay();
        }

        private void UpdateMaxPlayersDisplay()
        {
            string numStr = _createMaxPlayers.ToString();
            if (inputMaxPlayers != null && inputMaxPlayers.text != numStr)
            {
                inputMaxPlayers.text = numStr;
            }
            else if (labelMaxPlayers != null)
            {
                labelMaxPlayers.text = numStr;
            }
            if (labelMaxPlayersUnit != null) labelMaxPlayersUnit.text = "PLAYERS";
        }

        private void AdjustDuration(int delta)
        {
            _hasCustomizedDuration = true;
            _createDurationMinutes = Mathf.Clamp(_createDurationMinutes + (delta * durationStepMinutes), minDurationMinutes, maxDurationMinutes);
            if (sliderMatchDuration != null && Mathf.RoundToInt(sliderMatchDuration.value) != _createDurationMinutes)
            {
                sliderMatchDuration.value = _createDurationMinutes;
            }
            UpdateDurationDisplay();
        }

        private void OnDurationChanged(float val)
        {
            _hasCustomizedDuration = true;
            _createDurationMinutes = Mathf.Clamp(Mathf.RoundToInt(val), minDurationMinutes, maxDurationMinutes);
            UpdateDurationDisplay();
        }

        private void OnDurationInputEndEdit(string text)
        {
            if (int.TryParse(text, out int val) && val > 0)
            {
                _hasCustomizedDuration = true;
                _createDurationMinutes = Mathf.Clamp(val, minDurationMinutes, maxDurationMinutes);
                if (sliderMatchDuration != null) sliderMatchDuration.value = _createDurationMinutes;
            }
            UpdateDurationDisplay();
        }

        private void UpdateDurationDisplay()
        {
            string numStr = _createDurationMinutes.ToString();
            if (inputMatchDuration != null && inputMatchDuration.text != numStr)
            {
                inputMatchDuration.text = numStr;
            }
            else if (labelMatchDuration != null)
            {
                labelMatchDuration.text = numStr;
            }
            if (labelDurationUnit != null) labelDurationUnit.text = "MINS";
        }

        private void AdjustScoreLimit(int delta)
        {
            _hasCustomizedScore = true;
            _createScoreLimit = Mathf.Clamp(_createScoreLimit + (delta * scoreLimitStep), minScoreLimit, maxScoreLimit);
            if (sliderScoreLimit != null && Mathf.RoundToInt(sliderScoreLimit.value) != _createScoreLimit)
            {
                sliderScoreLimit.value = _createScoreLimit;
            }
            UpdateScoreLimitDisplay();
        }

        private void OnScoreLimitChanged(float val)
        {
            _hasCustomizedScore = true;
            _createScoreLimit = Mathf.Clamp(Mathf.RoundToInt(val), minScoreLimit, maxScoreLimit);
            UpdateScoreLimitDisplay();
        }

        private void OnScoreLimitInputEndEdit(string text)
        {
            if (int.TryParse(text, out int val) && val > 0)
            {
                _hasCustomizedScore = true;
                _createScoreLimit = Mathf.Clamp(val, minScoreLimit, maxScoreLimit);
                if (sliderScoreLimit != null) sliderScoreLimit.value = _createScoreLimit;
            }
            UpdateScoreLimitDisplay();
        }

        private void UpdateScoreLimitDisplay()
        {
            string numStr = _createScoreLimit.ToString();
            if (inputScoreLimit != null && inputScoreLimit.text != numStr)
            {
                inputScoreLimit.text = numStr;
            }
            else if (labelScoreLimit != null)
            {
                labelScoreLimit.text = numStr;
            }
            string unit = (_selectedCreateMode == "TDM") ? "PTS" : "KILLS";
            if (labelScoreLimitUnit != null) labelScoreLimitUnit.text = unit;
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

            CloseTopModal();
            ShowBusy(true, "INITIALIZING ARENA...");

            try
            {
                await MultiplayerAutoInitializer.EnsureInitializedAsync();
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
                await MultiplayerAutoInitializer.EnsureInitializedAsync();
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

            if (texts.Length > 0) texts[0].text = roomName;
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

        // ──────────────────────── CUSTOMIZATION ────────────────────────

        private void SelectSkin(int skinIndex)
        {
            _selectedSkin = Mathf.Clamp(skinIndex, 0, 1);
            if (PlayerProfileManager.Instance != null)
            {
                PlayerProfileManager.Instance.SetSelectedModel(_selectedSkin);
            }
            else
            {
                PlayerPrefs.SetInt("Shootopia_SelectedModelIndex", _selectedSkin);
                PlayerPrefs.Save();
            }

            if (AvatarStageController.Instance != null)
            {
                AvatarStageController.Instance.SetSkin(_selectedSkin);
            }
            UpdateChipVisuals();
        }

        // ──────────────────────── SETTINGS ────────────────────────

        private void InitSettingsValues()
        {
            _selectedSkin = PlayerProfileManager.Instance != null 
                ? PlayerProfileManager.Instance.SelectedModelIndex 
                : PlayerPrefs.GetInt("Shootopia_SelectedModelIndex", 0);

            if (btnSkinAgent != null)
            {
                var txt = btnSkinAgent.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = "VANGUARD";
            }
            if (btnSkinRobot != null)
            {
                var txt = btnSkinRobot.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = "VALKYRIE";
            }
            if (AvatarStageController.Instance != null)
            {
                AvatarStageController.Instance.SetSkin(_selectedSkin);
            }

            // Clean up any accidental leave buttons from modalSettings in MainMenu
            if (modalSettings != null)
            {
                Transform leaveTop = modalSettings.transform.Find("BtnActionLeaveTopLeft");
                if (leaveTop != null) leaveTop.gameObject.SetActive(false);
                Transform oldLeaveCorner = modalSettings.transform.Find("BtnActionLeaveCorner");
                if (oldLeaveCorner != null) Destroy(oldLeaveCorner.gameObject);

                Transform bottomContainer = modalSettings.transform.Find("Bottom_UI_Container");
                if (bottomContainer != null)
                {
                    Transform oldBottomLeave = bottomContainer.Find("BtnActionLeaveCorner") ?? bottomContainer.Find("BtnActionLeaveTopLeft");
                    if (oldBottomLeave != null) Destroy(oldBottomLeave.gameObject);

                    Transform btn1 = bottomContainer.Find("BtnAction1");
                    Transform btn2 = bottomContainer.Find("BtnAction2");
                    Transform btn3 = bottomContainer.Find("BtnAction3");

                    if (btn1 != null)
                    {
                        btnActionReset = btn1.GetComponent<Button>();
                        var txt = btn1.GetComponentInChildren<TextMeshProUGUI>();
                        if (txt != null) txt.text = "RESET";
                        if (btnActionReset != null)
                        {
                            btnActionReset.onClick.RemoveAllListeners();
                            btnActionReset.onClick.AddListener(ResetSettingsToDefaults);
                        }
                    }

                    if (btn2 != null)
                    {
                        btnActionControls = btn2.GetComponent<Button>();
                        var txt = btn2.GetComponentInChildren<TextMeshProUGUI>();
                        if (txt != null) txt.text = "CONTROLS";
                        if (btnActionControls != null)
                        {
                            btnActionControls.onClick.RemoveAllListeners();
                            btnActionControls.onClick.AddListener(OpenControlsModal);
                        }
                    }

                    if (btn3 != null)
                    {
                        btnActionApply = btn3.GetComponent<Button>();
                        var txt = btn3.GetComponentInChildren<TextMeshProUGUI>();
                        if (txt != null) txt.text = "APPLY";
                        if (btnActionApply != null)
                        {
                            btnActionApply.onClick.RemoveAllListeners();
                            btnActionApply.onClick.AddListener(CloseTopModal);
                        }
                    }
                }
            }

            if (modalSettings != null)
            {
                if (sliderMasterVol == null) sliderMasterVol = modalSettings.transform.Find("TabContent_General/Slider_Container/Sound_Slider/Slider")?.GetComponent<Slider>();
                if (labelMasterVol == null) labelMasterVol = modalSettings.transform.Find("TabContent_General/Slider_Container/Sound_Slider/Val_TXT")?.GetComponent<TextMeshProUGUI>();
                if (sliderFOV == null) sliderFOV = modalSettings.transform.Find("TabContent_General/Slider_Container/Row_FOV/Slider")?.GetComponent<Slider>();
                if (labelFOV == null) labelFOV = modalSettings.transform.Find("TabContent_General/Slider_Container/Row_FOV/Val_TXT")?.GetComponent<TextMeshProUGUI>();
            }

            float master = PlayerPrefs.GetFloat("AudioManager_MasterVolume", 1.0f);
            float music = PlayerPrefs.GetFloat("AudioManager_MusicVolume", 0.65f);
            float sfx = PlayerPrefs.GetFloat("AudioManager_SFXVolume", 1.0f);
            float sens = PlayerProfileManager.Instance != null ? PlayerProfileManager.Instance.MouseSensitivity : 1.0f;
            float fov = PlayerPrefs.GetFloat(ThirdPersonCameraController.PREFS_KEY_FOV, ThirdPersonCameraController.DEFAULT_FOV);
            if (Mathf.Approximately(fov, 80f) && !PlayerPrefs.HasKey("Shootopia_UserSetFOV"))
            {
                fov = ThirdPersonCameraController.DEFAULT_FOV;
            }
            bool invertY = (PlayerProfileManager.Instance != null && PlayerProfileManager.Instance.InvertY)
                        || (PlayerPrefs.GetInt("Shootopia_InvertY", 0) == 1);
            bool fullscreen = Screen.fullScreen;

            if (sliderMasterVol != null) { sliderMasterVol.value = master; OnMasterVolChanged(master); }
            if (sliderMusicVol != null && sliderMusicVol != sliderFOV) { sliderMusicVol.value = music; OnMusicVolChanged(music); }
            if (sliderSFXVol != null) { sliderSFXVol.value = sfx; OnSFXVolChanged(sfx); }
            if (sliderMouseSens != null) { sliderMouseSens.value = sens; OnMouseSensChanged(sens); }
            if (labelMouseSens != null) labelMouseSens.text = $"{sens:0.0}x";

            if (sliderFOV != null)
            {
                sliderFOV.minValue = ThirdPersonCameraController.MIN_FOV;
                sliderFOV.maxValue = ThirdPersonCameraController.MAX_FOV;
                sliderFOV.wholeNumbers = true;
                sliderFOV.value = fov;
                sliderFOV.onValueChanged.RemoveListener(OnFOVChanged);
                sliderFOV.onValueChanged.AddListener(OnFOVChanged);
                if (labelFOV != null) labelFOV.text = $"{Mathf.RoundToInt(fov)}\u00B0";
            }

            if (toggleInvertY != null) toggleInvertY.isOn = invertY;
            if (toggleFullscreen != null) toggleFullscreen.isOn = fullscreen;
            if (switchInvertY != null) switchInvertY.SetState(invertY, false);
            if (switchFullscreen != null) switchFullscreen.SetState(fullscreen, false);
        }

        private void OnMasterVolChanged(float val)
        {
            if (labelMasterVol != null) labelMasterVol.text = $"{Mathf.RoundToInt(val * 100)}%";
            if (AudioManager.Instance != null) AudioManager.Instance.SetChannelVolume(AudioChannel.Master, val);
        }

        private void OnMusicVolChanged(float val)
        {
            if (labelMusicVol != null) labelMusicVol.text = $"{Mathf.RoundToInt(val * 100)}%";
            if (AudioManager.Instance != null) AudioManager.Instance.SetChannelVolume(AudioChannel.Music, val);
        }

        private void OnSFXVolChanged(float val)
        {
            if (labelSFXVol != null) labelSFXVol.text = $"{Mathf.RoundToInt(val * 100)}%";
            if (AudioManager.Instance != null) AudioManager.Instance.SetChannelVolume(AudioChannel.SFX, val);
        }

        private void OnMouseSensChanged(float val)
        {
            if (PlayerProfileManager.Instance != null)
            {
                PlayerProfileManager.Instance.SetMouseSensitivity(val);
            }
            if (labelMouseSens != null) labelMouseSens.text = $"{val:0.0}x";
        }

        private void OnFOVChanged(float val)
        {
            if (labelFOV != null) labelFOV.text = $"{Mathf.RoundToInt(val)}\u00B0";
            PlayerPrefs.SetFloat(ThirdPersonCameraController.PREFS_KEY_FOV, val);
            PlayerPrefs.SetInt("Shootopia_UserSetFOV", 1);
            PlayerPrefs.Save();
            if (ThirdPersonCameraController.Instance != null)
            {
                ThirdPersonCameraController.Instance.SetUserFOV(val);
            }
        }

        private void AdjustSensitivity(float delta)
        {
            float current = PlayerProfileManager.Instance != null ? PlayerProfileManager.Instance.MouseSensitivity : 1.0f;
            float newSens = Mathf.Clamp((float)Math.Round(current + delta, 1), 0.2f, 3.0f);
            OnMouseSensChanged(newSens);
            if (sliderMouseSens != null) sliderMouseSens.value = newSens;
        }

        private void ResetSettingsToDefaults()
        {
            if (sliderMasterVol != null) sliderMasterVol.value = 1.0f;
            if (sliderMusicVol != null) sliderMusicVol.value = 0.65f;
            if (sliderSFXVol != null) sliderSFXVol.value = 1.0f;
            OnMouseSensChanged(1.0f);
            if (sliderMouseSens != null) sliderMouseSens.value = 1.0f;
            if (sliderFOV != null) sliderFOV.value = ThirdPersonCameraController.DEFAULT_FOV;
            OnFOVChanged(ThirdPersonCameraController.DEFAULT_FOV);
            if (switchInvertY != null) switchInvertY.SetState(false, true);
            if (switchFullscreen != null) switchFullscreen.SetState(true, true);
        }

        public void OpenControlsModal()
        {
            EnsureControlsModalUI();
            if (modalControls != null)
            {
                OpenModal(modalControls, controlsGroup);
            }
        }

        private void EnsureControlsModalUI()
        {
            if (modalControls != null) return;

            Transform canvasTr = modalSettings != null ? modalSettings.transform.parent : transform;
            Transform existing = canvasTr.Find("ModalControlsSheet");
            if (existing != null)
            {
                modalControls = existing.gameObject;
                controlsGroup = modalControls.GetComponent<CanvasGroup>();
                btnCloseControls = modalControls.GetComponentInChildren<Button>();
                return;
            }

            var modalObj = new GameObject("ModalControlsSheet", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            modalObj.transform.SetParent(canvasTr, false);
            modalObj.transform.SetAsLastSibling();

            var mRt = modalObj.GetComponent<RectTransform>();
            mRt.anchorMin = new Vector2(0.5f, 0.5f);
            mRt.anchorMax = new Vector2(0.5f, 0.5f);
            mRt.sizeDelta = new Vector2(1060f, 660f);
            mRt.anchoredPosition = Vector2.zero;

            var mImg = modalObj.GetComponent<Image>();
            mImg.color = new Color(0.18f, 0.72f, 1.0f, 0.45f);

            controlsGroup = modalObj.GetComponent<CanvasGroup>();

            var backdropObj = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
            backdropObj.transform.SetParent(modalObj.transform, false);
            var bRt = backdropObj.GetComponent<RectTransform>();
            bRt.anchorMin = Vector2.zero;
            bRt.anchorMax = Vector2.one;
            bRt.sizeDelta = new Vector2(-16f, -16f);
            bRt.anchoredPosition = Vector2.zero;
            var bImg = backdropObj.GetComponent<Image>();
            bImg.color = new Color(0.04f, 0.07f, 0.14f, 0.96f);
            bImg.raycastTarget = false;

            var titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(modalObj.transform, false);
            var tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.5f, 1f);
            tRt.anchorMax = new Vector2(0.5f, 1f);
            tRt.pivot = new Vector2(0.5f, 1f);
            tRt.sizeDelta = new Vector2(980f, 40f);
            tRt.anchoredPosition = new Vector2(0f, -22f);

            var tTmp = titleObj.GetComponent<TextMeshProUGUI>();
            tTmp.text = "CONTROLS & KEYBINDINGS";
            tTmp.fontSize = 28;
            tTmp.fontStyle = FontStyles.Bold;
            tTmp.color = new Color(0.92f, 0.97f, 1f, 1f);
            tTmp.alignment = TextAlignmentOptions.Center;

            var subObj = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            subObj.transform.SetParent(modalObj.transform, false);
            var sRt = subObj.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.5f, 1f);
            sRt.anchorMax = new Vector2(0.5f, 1f);
            sRt.pivot = new Vector2(0.5f, 1f);
            sRt.sizeDelta = new Vector2(980f, 24f);
            sRt.anchoredPosition = new Vector2(0f, -60f);

            var sTmp = subObj.GetComponent<TextMeshProUGUI>();
            sTmp.text = "FULL INPUT SPECIFICATION • KEYBOARD / MOUSE & GAMEPAD";
            sTmp.fontSize = 14;
            sTmp.fontStyle = FontStyles.Normal;
            sTmp.color = new Color(0.6f, 0.78f, 0.95f, 1f);
            sTmp.alignment = TextAlignmentOptions.Center;

            var sheetContainer = new GameObject("SheetContainer", typeof(RectTransform));
            sheetContainer.transform.SetParent(modalObj.transform, false);
            var scRt = sheetContainer.GetComponent<RectTransform>();
            scRt.anchorMin = new Vector2(0.5f, 0.5f);
            scRt.anchorMax = new Vector2(0.5f, 0.5f);
            scRt.sizeDelta = new Vector2(1000f, 480f);
            scRt.anchoredPosition = new Vector2(0f, -10f);

            PauseMenuController.BuildControlsSheetView(sheetContainer.transform, tTmp.font);

            var closeBtnObj = new GameObject("BtnCloseControls", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIJuice));
            closeBtnObj.transform.SetParent(modalObj.transform, false);
            var cbRt = closeBtnObj.GetComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(0.5f, 0f);
            cbRt.anchorMax = new Vector2(0.5f, 0f);
            cbRt.pivot = new Vector2(0.5f, 0f);
            cbRt.sizeDelta = new Vector2(220f, 48f);
            cbRt.anchoredPosition = new Vector2(0f, 20f);

            var cbImg = closeBtnObj.GetComponent<Image>();
            cbImg.color = new Color(0.08f, 0.12f, 0.22f, 0.95f);

            btnCloseControls = closeBtnObj.GetComponent<Button>();
            btnCloseControls.onClick.AddListener(CloseTopModal);

            var cbTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            cbTxtObj.transform.SetParent(closeBtnObj.transform, false);
            var cbtRt = cbTxtObj.GetComponent<RectTransform>();
            cbtRt.anchorMin = Vector2.zero;
            cbtRt.anchorMax = Vector2.one;
            cbtRt.sizeDelta = Vector2.zero;

            var cbtTmp = cbTxtObj.GetComponent<TextMeshProUGUI>();
            cbtTmp.text = "BACK TO SETTINGS";
            cbtTmp.fontSize = 16;
            cbtTmp.fontStyle = FontStyles.Bold;
            cbtTmp.color = new Color(0.2f, 0.85f, 1.0f, 1f);
            cbtTmp.alignment = TextAlignmentOptions.Center;

            modalControls = modalObj;
            modalControls.SetActive(false);
        }

        private void OnInvertYChanged(bool val)
        {
            if (PlayerProfileManager.Instance != null) PlayerProfileManager.Instance.SetInvertY(val);
            PlayerPrefs.SetInt("Shootopia_InvertY", val ? 1 : 0);
            PlayerPrefs.Save();
            if (toggleInvertY != null) toggleInvertY.isOn = val;
            if (switchInvertY != null && switchInvertY.IsOn != val) switchInvertY.SetState(val, false);
        }

        private void OnFullscreenChanged(bool val)
        {
            Screen.fullScreen = val;
            if (toggleFullscreen != null) toggleFullscreen.isOn = val;
            if (switchFullscreen != null && switchFullscreen.IsOn != val) switchFullscreen.SetState(val, false);
        }

        // ──────────────────────── MODAL STACK & UI HELPERS ────────────────────────

        public void OpenModal(GameObject modal, CanvasGroup group = null)
        {
            if (modal == null) return;
            if (modal == modalSettings)
            {
                InitSettingsValues();
            }
            _modalStack.Push(modal);
            UIJuice.PopIn(modal.GetComponent<RectTransform>(), group);
        }

        public void CloseTopModal()
        {
            if (_modalStack.Count > 0)
            {
                var top = _modalStack.Pop();
                var group = top.GetComponent<CanvasGroup>();
                UIJuice.PopOut(top.GetComponent<RectTransform>(), group);
            }
        }

        public void ShowBusy(bool show, string message = "WARPING...")
        {
            _isBusy = show;
            if (busyOverlay != null)
            {
                if (show)
                {
                    if (busyLabel != null) busyLabel.text = message;
                    UIJuice.PopIn(busyOverlay.GetComponent<RectTransform>(), busyGroup);
                }
                else
                {
                    UIJuice.PopOut(busyOverlay.GetComponent<RectTransform>(), busyGroup);
                }
            }
        }

        private async void CancelBusy()
        {
            ShowBusy(false);
            if (SessionManager.Instance != null)
            {
                await SessionManager.Instance.LeaveSession();
            }
        }

        public void ShowError(string error)
        {
            ShowBusy(false);
            if (errorModal != null)
            {
                if (errorLabel != null) errorLabel.text = error;
                OpenModal(errorModal, errorGroup);
                if (errorClip != null && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX2D(errorClip, 1.0f);
                }
            }
            else
            {
                NetworkNotificationUI.ShowToast(error, isError: true);
            }
        }

        private void OnSessionError(string error)
        {
            ShowError(error);
        }

        private void UpdateChipVisuals()
        {
            SetChipActive(chipQuickAll, _quickMatchMode == "ALL");
            SetChipActive(chipQuickFFA, _quickMatchMode == "FFA");
            SetChipActive(chipQuickTDM, _quickMatchMode == "TDM");

            SetChipActive(chipQuickMapAll, _quickMatchMap == "ALL");
            SetChipActive(chipQuickMapSectorAlpha, _quickMatchMap == "Map1");
            SetChipActive(chipQuickMapGasStation, _quickMatchMap == "GasStationMap");

            SetChipActive(chipModeFFA, _selectedCreateMode == "FFA");
            SetChipActive(chipModeTDM, _selectedCreateMode == "TDM");

            SetChipActive(chipMapSectorAlpha, _selectedCreateMap == "Map1");
            SetChipActive(chipMapGasStation, _selectedCreateMap == "GasStationMap");

            SetChipActive(chipFilterAll, _browserModeFilter == "ALL");
            SetChipActive(chipFilterFFA, _browserModeFilter == "FFA");
            SetChipActive(chipFilterTDM, _browserModeFilter == "TDM");

            SetChipActive(btnSkinAgent, _selectedSkin == 0);
            SetChipActive(btnSkinRobot, _selectedSkin == 1);

            if (btnCustomize != null)
            {
                var custTxt = btnCustomize.GetComponentInChildren<TextMeshProUGUI>();
                if (custTxt != null)
                {
                    string opName = _selectedSkin == 1 ? "VALKYRIE" : "VANGUARD";
                    custTxt.text = $"{opName} [C]";
                }
            }
        }

        private void SetChipActive(Button chip, bool isActive)
        {
            if (chip == null) return;

            // Check for Rich Map Card elements (card background border, checkmark badge, title)
            var checkmark = chip.transform.Find("CheckmarkBadge");
            var title = chip.transform.Find("MaskContainer/TextContainer/TitleText")?.GetComponent<TextMeshProUGUI>();

            if (checkmark != null || title != null)
            {
                var cardBg = chip.GetComponent<Image>();
                if (cardBg != null)
                {
                    cardBg.color = isActive ? new Color(0.0f, 0.95f, 1.0f, 1.0f) : new Color(0.22f, 0.26f, 0.38f, 0.7f);
                }
                if (checkmark != null) checkmark.gameObject.SetActive(isActive);
                if (title != null) title.color = isActive ? new Color(1.0f, 0.92f, 0.4f, 1.0f) : Color.white;
                return;
            }

            var img = chip.GetComponent<Image>();
            if (img != null)
            {
                img.color = isActive ? new Color(1f, 0.9f, 0.4f, 1f) : new Color(0.7f, 0.7f, 0.8f, 0.85f);
            }
        }

        private void QuitGame()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }
}
