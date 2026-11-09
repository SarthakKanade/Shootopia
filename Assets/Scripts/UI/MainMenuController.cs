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
    public partial class MainMenuController : MonoBehaviour
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

        [Header("Top Header: Region Selector")]
        [SerializeField] private Button btnTopRegion;
        [SerializeField] private TextMeshProUGUI topRegionLabel;

        [Header("Modal: Server Region Select")]
        [SerializeField] private GameObject modalRegionSelect;
        [SerializeField] private CanvasGroup regionSelectGroup;
        [SerializeField] private Button btnCloseRegionSelect;
        [SerializeField] private Button chipRegionAuto;
        [SerializeField] private Button chipRegionIndia;
        [SerializeField] private Button chipRegionAsia;
        [SerializeField] private Button chipRegionEurope;
        [SerializeField] private Button chipRegionUSEast;
        [SerializeField] private TextMeshProUGUI regionStatusText;

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
        [SerializeField] private Toggle toggleLanRoom;

        private TextMeshProUGUI _publicRoomLabel;
        private TextMeshProUGUI _lanRoomLabel;
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

    }
}
