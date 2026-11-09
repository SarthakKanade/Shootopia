using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using Unity.Netcode;
using Unity.Services.Multiplayer;

namespace Shootopia
{
    public partial class WaitingRoomController : MonoBehaviour
    {
        public static WaitingRoomController Instance { get; private set; }
        private static bool _isLobbyMenuOpen = false;
        public static bool IsLobbyMenuOpen
        {
            get => Instance != null && Instance.gameObject.activeInHierarchy && _isLobbyMenuOpen;
            private set => _isLobbyMenuOpen = value;
        }

        [Header("Scene Configuration")]
        [SerializeField] private string gameplaySceneName = "Map1";
        public string GameplaySceneName => gameplaySceneName;

        [Header("Centered Modal Window")]
        [SerializeField] private GameObject dimBackdrop;
        [SerializeField] private GameObject lobbyModalDiv;
        [SerializeField] private CanvasGroup lobbyCanvasGroup;
        [SerializeField] private GameObject warmupHudBadge;
        [SerializeField] private Button btnOpenLobbyFromWarmup;
        [SerializeField] private Button btnMinimizeToWarmup;

        [Header("Top Header Bar")]
        [SerializeField] private TextMeshProUGUI roomNameLabel;
        [SerializeField] private TextMeshProUGUI roomModeBadge;
        [SerializeField] private TextMeshProUGUI roomMapLabel;
        [SerializeField] private Image roomMapThumbnail;
        [SerializeField] private Sprite mapSectorAlphaSprite;
        [SerializeField] private Sprite mapGasStationSprite;
        [SerializeField] private TextMeshProUGUI roomCodeDisplay;
        [SerializeField] private Button btnCopyCode;
        [SerializeField] private TextMeshProUGUI btnCopyCodeLabel;
        [SerializeField] private TextMeshProUGUI roomPlayerCountLabel;
        [SerializeField] private Button btnLeave;

        [Header("Roster Panels")]
        [SerializeField] private GameObject panelFFA;
        [SerializeField] private Transform contentFFA;

        [SerializeField] private GameObject panelTeams;
        [SerializeField] private Transform contentAlpha;
        [SerializeField] private Transform contentOmega;
        [SerializeField] private TextMeshProUGUI labelAlphaCount;
        [SerializeField] private TextMeshProUGUI labelOmegaCount;
        [SerializeField] private Button btnSelectAlpha;
        [SerializeField] private Button btnSelectOmega;
        [SerializeField] private Image imgSelectAlphaBg;
        [SerializeField] private Image imgSelectOmegaBg;

        [Header("Player Card Prefab")]
        [SerializeField] private GameObject playerCardPrefab;

        [Header("Bottom Controls")]
        [SerializeField] private TextMeshProUGUI rosterStatusSummary;
        [SerializeField] private GameObject hostSettingsGroup;
        [SerializeField] private Toggle togglePublicRoom;
        [SerializeField] private Button btnReady;
        [SerializeField] private TextMeshProUGUI btnReadyLabel;
        [SerializeField] private Button btnStartMatch;

        [Header("Audio & SFX")]
        [SerializeField] private AudioClip startMatchClip;
        [SerializeField] private AudioClip waitingRoomBgm;
        [SerializeField] private AudioClip gasStationBgm;
        [SerializeField] private AudioClip volcanoBgm;

        private static readonly Color ColorSapphire = new Color(0.18f, 0.72f, 1.0f, 1.0f); // #2EB8FF
        private static readonly Color ColorRuby = new Color(1.0f, 0.28f, 0.42f, 1.0f);     // #FF476B
        private static readonly Color ColorGold = new Color(1.0f, 0.85f, 0.35f, 1.0f);
        private static readonly Color ColorGreen = new Color(0.25f, 0.95f, 0.55f, 1.0f);
        private static readonly Color ColorMuted = new Color(0.65f, 0.7f, 0.8f, 0.75f);

        private bool _isLocalReady = false;
        private int _localTeamIndex = 0; // 0 = Alpha, 1 = Omega
        private bool _matchStarted = false;
        public bool IsMatchStarted => _matchStarted;
        private Coroutine _copyFeedbackCoroutine;

        private bool _isGameOverMode = false;
        public static bool IsGameOverOpen => Instance != null && Instance._isGameOverMode;
        private string _cachedGameOverWinnerInfo;
        private PlayerScoreEntry[] _cachedGameOverScores;

        private void Awake()
        {
            Instance = this;
            if (dimBackdrop == null)
            {
                var dt = transform.Find("DimBackdrop");
                if (dt != null) dimBackdrop = dt.gameObject;
            }
        }

        private void Start()
        {
            if (!_matchStarted && !_isGameOverMode)
            {
                PlayWaitingRoomMusic();
            }
        }

        private void OnEnable()
        {
            RegisterCustomMessaging();
            GameModeBase.OnMatchEndedWithScoreboard += OnMatchEndedScoreboardReceived;

            if (_isGameOverMode)
            {
                RefreshGameOverUI();
                return;
            }

            var activeMode = NetworkedGameManager.Instance != null ? NetworkedGameManager.Instance.ActiveGameMode : GameModeBase.Instance;
            if (activeMode == null)
            {
                activeMode = (GameModeBase)FindAnyObjectByType<FFAMode>() ?? (GameModeBase)FindAnyObjectByType<TDMMode>();
            }

            bool isHost = (SessionManager.Instance != null && SessionManager.Instance.IsHost) ||
                          (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost);

            bool isAlreadyStarted = false;
            if (!isHost)
            {
                try
                {
                    isAlreadyStarted = (NetworkedGameManager.Instance != null && NetworkedGameManager.Instance.IsSpawned && NetworkedGameManager.Instance.MatchStarted.Value) || 
                                       (activeMode != null && activeMode.IsSpawned && (activeMode.MatchActive.Value || activeMode.IsInProgress));
                }
                catch { }
            }

            if (isAlreadyStarted)
            {
                OnMatchStarted();
                return;
            }

            _matchStarted = false;
            SetLobbyVisible(true);

            RegisterCallbacks();
            RefreshLobbyUI();
            LoadingScreenManager.HideOverlay();

            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.OnSessionJoined.AddListener(RefreshLobbyUI);
                SessionManager.Instance.OnSessionLeft.AddListener(OnSessionLeftLocally);
                SessionManager.Instance.OnPlayerReadyChanged.AddListener(RefreshLobbyUI);
                SessionManager.Instance.OnSessionUpdated.AddListener(RefreshLobbyUI);
            }

            PlayWaitingRoomMusic();
        }

        private void OnDisable()
        {
            UnregisterCustomMessaging();
            GameModeBase.OnMatchEndedWithScoreboard -= OnMatchEndedScoreboardReceived;

            if (IsLobbyMenuOpen)
            {
                CursorManager.PopUnlock("WaitingRoom");
                IsLobbyMenuOpen = false;
            }

            if (dimBackdrop != null) dimBackdrop.SetActive(false);

            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.OnSessionJoined.RemoveListener(RefreshLobbyUI);
                SessionManager.Instance.OnSessionLeft.RemoveListener(OnSessionLeftLocally);
                SessionManager.Instance.OnPlayerReadyChanged.RemoveListener(RefreshLobbyUI);
                SessionManager.Instance.OnSessionUpdated.RemoveListener(RefreshLobbyUI);
            }

            UnregisterCallbacks();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                _isLobbyMenuOpen = false;
                Instance = null;
            }
        }

        private void Update()
        {
            if (_isGameOverMode) return;
            if (_matchStarted) return;

            bool isHost = (SessionManager.Instance != null && SessionManager.Instance.IsHost) ||
                          (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost);

            if (isHost) return;

            var activeMode = NetworkedGameManager.Instance != null ? NetworkedGameManager.Instance.ActiveGameMode : GameModeBase.Instance;
            if (activeMode == null)
            {
                activeMode = (GameModeBase)FindAnyObjectByType<FFAMode>() ?? (GameModeBase)FindAnyObjectByType<TDMMode>();
            }

            bool isNetworkMatchStarted = false;
            try
            {
                isNetworkMatchStarted = (NetworkedGameManager.Instance != null && NetworkedGameManager.Instance.IsSpawned && NetworkedGameManager.Instance.MatchStarted.Value) ||
                                         (activeMode != null && activeMode.IsSpawned && (activeMode.MatchActive.Value || activeMode.IsInProgress));
            }
            catch { }

            if (isNetworkMatchStarted)
            {
                OnMatchStarted();
                return;
            }

            if (Keyboard.current != null)
            {
                if (Keyboard.current.tabKey.wasPressedThisFrame)
                {
                    ToggleLobbyVisibility();
                }
                else if (Keyboard.current.escapeKey.wasPressedThisFrame && IsLobbyMenuOpen)
                {
                    SetLobbyVisible(false);
                }
            }
        }

    }
}
