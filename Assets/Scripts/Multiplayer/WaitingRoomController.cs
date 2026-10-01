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
    /// <summary>
    /// Master UGUI Controller for the Waiting Room (Lobby) interface.
    /// Operates as a centered modal window over the active map.
    /// Supports both Free-For-All (all players unified, Sapphire/Blue theme)
    /// and Team Modes (Alpha Sapphire Blue vs Omega Ruby Red) with interactive team switching,
    /// ready status synchronization, warmup sandbox toggling with [TAB], and match start flow.
    /// </summary>
    public class WaitingRoomController : MonoBehaviour
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

        // Colors
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

            bool isAlreadyStarted = false;
            try
            {
                isAlreadyStarted = (NetworkedGameManager.Instance != null && NetworkedGameManager.Instance.MatchStarted.Value) || 
                                   (activeMode != null && (activeMode.MatchActive.Value || activeMode.IsInProgress));
            }
            catch { }

            if (isAlreadyStarted)
            {
                OnMatchStarted();
                return;
            }

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

            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.OnSessionJoined.RemoveListener(RefreshLobbyUI);
                SessionManager.Instance.OnSessionLeft.RemoveListener(OnSessionLeftLocally);
                SessionManager.Instance.OnPlayerReadyChanged.RemoveListener(RefreshLobbyUI);
                SessionManager.Instance.OnSessionUpdated.RemoveListener(RefreshLobbyUI);
            }

            UnregisterCallbacks();
        }

        private void RegisterCustomMessaging()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.CustomMessagingManager != null)
            {
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("Shootopia_MatchStarted", OnMatchStartedDirectMessage);
            }
        }

        private void UnregisterCustomMessaging()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.CustomMessagingManager != null)
            {
                NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler("Shootopia_MatchStarted");
            }
        }

        private void OnMatchStartedDirectMessage(ulong senderClientId, FastBufferReader reader)
        {
            Debug.Log("[WaitingRoomController] Direct Shootopia_MatchStarted message received. Immediately starting match locally.");
            OnMatchStarted();
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

            // Check if match started on network
            var activeMode = NetworkedGameManager.Instance != null ? NetworkedGameManager.Instance.ActiveGameMode : GameModeBase.Instance;
            if (activeMode == null)
            {
                activeMode = (GameModeBase)FindAnyObjectByType<FFAMode>() ?? (GameModeBase)FindAnyObjectByType<TDMMode>();
            }

            bool isNetworkMatchStarted = false;
            try
            {
                isNetworkMatchStarted = (NetworkedGameManager.Instance != null && NetworkedGameManager.Instance.MatchStarted.Value) ||
                                         (activeMode != null && (activeMode.MatchActive.Value || activeMode.IsInProgress));
            }
            catch { }

            if (isNetworkMatchStarted)
            {
                OnMatchStarted();
                return;
            }

            // [TAB] or [ESC] toggles between Lobby Menu and Warmup Gameplay
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
                    isNetworkMatchStarted = (NetworkedGameManager.Instance != null && NetworkedGameManager.Instance.MatchStarted.Value) ||
                                             (activeMode != null && (activeMode.MatchActive.Value || activeMode.IsInProgress));
                }
                catch { }

                if (_matchStarted || isNetworkMatchStarted)
                {
                    visible = false;
                    _matchStarted = true;
                }
            }

            IsLobbyMenuOpen = visible;

            if (visible)
            {
                CursorManager.PushUnlock("WaitingRoom");
                if (lobbyModalDiv != null) lobbyModalDiv.SetActive(true);
                if (lobbyCanvasGroup != null)
                {
                    lobbyCanvasGroup.alpha = 1f;
                    lobbyCanvasGroup.blocksRaycasts = true;
                    lobbyCanvasGroup.interactable = true;
                }
                if (warmupHudBadge != null) warmupHudBadge.SetActive(false);
            }
            else
            {
                CursorManager.PopUnlock("WaitingRoom");
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

        public void RefreshLobbyUI()
        {
            if (_matchStarted) return;

            var session = SessionManager.Instance != null ? SessionManager.Instance.GetCurrentSession() : null;
            bool isHost = session != null && session.IsHost;

            // Room Name
            if (roomNameLabel != null)
            {
                roomNameLabel.text = session != null ? session.Name.ToUpper() : "CUSTOM ARENA";
            }

            // Mode & Map
            string mode = "FFA";
            if (session != null && session.Properties != null && session.Properties.TryGetValue("GameMode", out var modeProp))
            {
                mode = modeProp.Value.ToUpper();
            }

            bool isTeamMode = (mode == "TDM" || mode.Contains("TEAM"));

            if (roomModeBadge != null)
            {
                roomModeBadge.text = isTeamMode ? "TEAM DEATHMATCH" : "FREE-FOR-ALL";
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

            // Room Code
            string code = SessionManager.Instance != null ? SessionManager.Instance.GetJoinCode() : "------";
            if (string.IsNullOrEmpty(code)) code = "------";
            if (roomCodeDisplay != null)
            {
                roomCodeDisplay.text = code;
            }

            // Player Count
            int count = session != null && session.Players != null ? session.Players.Count : 1;
            int max = session != null ? session.MaxPlayers : 8;
            if (roomPlayerCountLabel != null)
            {
                roomPlayerCountLabel.text = $"{count}/{max} PLAYERS";
            }

            // Host Controls
            if (hostSettingsGroup != null)
            {
                hostSettingsGroup.SetActive(isHost);
            }

            if (btnStartMatch != null)
            {
                btnStartMatch.gameObject.SetActive(isHost);
                btnStartMatch.interactable = CanStartMatch(mode, count);
            }

            if (btnReady != null)
            {
                btnReady.gameObject.SetActive(!isHost);
                if (btnReadyLabel != null)
                {
                    btnReadyLabel.text = _isLocalReady ? "UNREADY" : "READY";
                }
            }

            // Toggle Panels based on Game Mode
            if (panelFFA != null) panelFFA.SetActive(!isTeamMode);
            if (panelTeams != null) panelTeams.SetActive(isTeamMode);

            // Update Team Switch buttons interactivity
            if (isTeamMode)
            {
                if (btnSelectAlpha != null) btnSelectAlpha.interactable = !_isLocalReady;
                if (btnSelectOmega != null) btnSelectOmega.interactable = !_isLocalReady;

                // Highlight active team button
                if (imgSelectAlphaBg != null)
                {
                    imgSelectAlphaBg.color = (_localTeamIndex == 0) ? new Color(0.2f, 0.75f, 1f, 1f) : new Color(0.2f, 0.3f, 0.5f, 0.6f);
                }
                if (imgSelectOmegaBg != null)
                {
                    imgSelectOmegaBg.color = (_localTeamIndex == 1) ? new Color(1f, 0.3f, 0.45f, 1f) : new Color(0.5f, 0.2f, 0.3f, 0.6f);
                }
            }

            PopulateRoster(session, isHost, isTeamMode);
        }

        private bool CanStartMatch(string mode, int playerCount)
        {
            // Allow the host to start as long as there is at least 1 player in the session
            return playerCount >= 1;
        }

        private void PopulateRoster(ISession session, bool isHost, bool isTeamMode)
        {
            // Clear existing cards
            if (contentFFA != null) foreach (Transform child in contentFFA) Destroy(child.gameObject);
            if (contentAlpha != null) foreach (Transform child in contentAlpha) Destroy(child.gameObject);
            if (contentOmega != null) foreach (Transform child in contentOmega) Destroy(child.gameObject);

            var readyStates = SessionManager.Instance != null ? SessionManager.Instance.GetPlayerReadyStates() : null;
            if (readyStates == null || readyStates.Count == 0) return;

            string localPlayerId = (session != null && session.CurrentPlayer != null) ? session.CurrentPlayer.Id : "";
            string hostId = session != null ? session.Host : "";
            int readyCount = 0;
            int alphaCount = 0;
            int omegaCount = 0;

            foreach (var p in readyStates)
            {
                bool isPlayerHost = (p.playerId == hostId);
                bool isLocal = (p.playerId == localPlayerId);
                bool isReady = (p.isReady || isPlayerHost);
                if (isReady) readyCount++;

                if (isLocal)
                {
                    _localTeamIndex = p.teamIndex;
                }

                Transform targetParent = contentFFA;
                Color themeColor = ColorSapphire;

                if (isTeamMode)
                {
                    if (p.teamIndex == 0)
                    {
                        targetParent = contentAlpha;
                        themeColor = ColorSapphire;
                        alphaCount++;
                    }
                    else
                    {
                        targetParent = contentOmega;
                        themeColor = ColorRuby;
                        omegaCount++;
                    }
                }

                if (targetParent != null && playerCardPrefab != null)
                {
                    CreatePlayerCard(targetParent, p, isPlayerHost, isLocal, isReady, themeColor);
                }
            }

            // Update Counts
            if (labelAlphaCount != null) labelAlphaCount.text = $"({alphaCount})";
            if (labelOmegaCount != null) labelOmegaCount.text = $"({omegaCount})";

            if (rosterStatusSummary != null)
            {
                if (isHost)
                {
                    rosterStatusSummary.text = (readyCount >= readyStates.Count)
                        ? "<color=#45F595>ALL PLAYERS READY — LAUNCH ARENA</color>"
                        : $"<color=#FFD555>{readyCount}/{readyStates.Count} PLAYERS READY</color>";
                }
                else
                {
                    rosterStatusSummary.text = _isLocalReady
                        ? "<color=#45F595>YOU ARE READY! WAITING FOR HOST...</color>"
                        : "<color=#FFD555>CLICK READY TO CONFIRM</color>";
                }
            }
        }

        private void CreatePlayerCard(Transform parent, PlayerReadyState player, bool isHost, bool isLocal, bool isReady, Color nameColor)
        {
            var card = Instantiate(playerCardPrefab, parent);
            var rect = card.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(parent.GetComponent<RectTransform>() != null ? parent.GetComponent<RectTransform>().rect.width - 16 : 380, 56);
            }

            var texts = card.GetComponentsInChildren<TextMeshProUGUI>();
            var images = card.GetComponentsInChildren<Image>();

            string namePrefix = "";
            if (isHost)
            {
                namePrefix = "<color=#FFD555>👑 </color>";
            }
            else if (isReady)
            {
                namePrefix = "<color=#45F595>✔ </color>";
            }

            string displayName = namePrefix + player.displayName;
            if (isLocal) displayName += " <color=#FFD555>(YOU)</color>";

            foreach (var t in texts)
            {
                if (t.gameObject.name == "PlayerName" || t.gameObject.name == "Name")
                {
                    t.text = displayName;
                    t.color = nameColor;
                }
                else if (t.gameObject.name == "StatusText" || t.gameObject.name == "RoleBadge" || t.gameObject.name == "ReadyStatus")
                {
                    // No text for host/client role; ready state is shown via green tick in front of name and icons
                    t.text = "";
                    t.gameObject.SetActive(false);
                }
            }

            foreach (var img in images)
            {
                if (img.gameObject.name.Contains("Crown") || img.gameObject.name.Contains("HostIcon"))
                {
                    img.gameObject.SetActive(isHost);
                    if (isHost) img.color = ColorGold;
                }
                if (img.gameObject.name.Contains("ReadyBadge") || img.gameObject.name.Contains("StatusIndicator"))
                {
                    img.gameObject.SetActive(!isHost && isReady);
                    img.color = ColorGreen;
                }
                if (img.gameObject.name == "CardBorder" || img.gameObject.name == "BorderGlow")
                {
                    img.color = nameColor;
                }
            }
        }

        public async void SwitchTeam(int newTeamIndex)
        {
            if (_isLocalReady) return; // Locked when ready
            _localTeamIndex = newTeamIndex;

            if (SessionManager.Instance != null)
            {
                await SessionManager.Instance.SetPlayerTeam(newTeamIndex);
            }

            RefreshLobbyUI();
        }

        private async void ToggleReady()
        {
            _isLocalReady = !_isLocalReady;
            if (SessionManager.Instance != null)
            {
                await SessionManager.Instance.SetReadyStatus(_isLocalReady);
            }
            RefreshLobbyUI();
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

        /// <summary>
        /// Called when the match officially starts: closes Waiting Room UI completely.
        /// </summary>
        public void OnMatchStarted()
        {
            _matchStarted = true;
            _isGameOverMode = false;
            IsLobbyMenuOpen = false;
            CursorManager.PopUnlock("WaitingRoom");
            if (lobbyModalDiv != null) lobbyModalDiv.SetActive(false);
            if (warmupHudBadge != null) warmupHudBadge.SetActive(false);
            // Keep gameObject active so event listeners, audio, and GameOver triggers remain alive.
        }

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

        private void PopulateGameOverRoster(bool isTeamMode, PlayerScoreEntry[] scores, GameModeBase activeMode)
        {
            if (scores == null) scores = Array.Empty<PlayerScoreEntry>();

            if (panelFFA != null) panelFFA.SetActive(!isTeamMode);
            if (panelTeams != null) panelTeams.SetActive(isTeamMode);

            ulong localClientId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : ulong.MaxValue;

            if (!isTeamMode)
            {
                if (contentFFA != null)
                {
                    foreach (Transform child in contentFFA) Destroy(child.gameObject);

                    var sorted = new List<PlayerScoreEntry>(scores);
                    sorted.Sort((a, b) =>
                    {
                        int cmp = b.Kills.CompareTo(a.Kills);
                        if (cmp != 0) return cmp;
                        return a.Deaths.CompareTo(b.Deaths);
                    });

                    for (int i = 0; i < sorted.Count; i++)
                    {
                        var entry = sorted[i];
                        bool isWinner = (i == 0);
                        bool isLocal = (entry.ClientId == localClientId);
                        CreateGameOverPlayerCard(contentFFA, i + 1, entry, isWinner, isLocal, isWinner ? ColorGold : ColorSapphire);
                    }
                }
            }
            else
            {
                if (btnSelectAlpha != null) btnSelectAlpha.interactable = false;
                if (btnSelectOmega != null) btnSelectOmega.interactable = false;

                if (contentAlpha != null) foreach (Transform child in contentAlpha) Destroy(child.gameObject);
                if (contentOmega != null) foreach (Transform child in contentOmega) Destroy(child.gameObject);

                var tdm = activeMode as TDMMode ?? FindAnyObjectByType<TDMMode>();

                var alphaList = new List<PlayerScoreEntry>();
                var omegaList = new List<PlayerScoreEntry>();

                foreach (var entry in scores)
                {
                    int team = -1;
                    if (tdm != null)
                    {
                        team = tdm.GetPlayerTeam(entry.ClientId);
                    }
                    if (team == -1 && SessionManager.Instance != null)
                    {
                        string pName = PlayerNameRegistry.GetName(entry.ClientId);
                        var states = SessionManager.Instance.GetPlayerReadyStates();
                        if (states != null)
                        {
                            foreach (var st in states)
                            {
                                if (st.displayName == pName)
                                {
                                    team = st.teamIndex;
                                    break;
                                }
                            }
                        }
                    }
                    if (team == -1) team = 0;

                    if (team == 0) alphaList.Add(entry);
                    else omegaList.Add(entry);
                }

                alphaList.Sort((a, b) =>
                {
                    int cmp = b.Kills.CompareTo(a.Kills);
                    if (cmp != 0) return cmp;
                    return a.Deaths.CompareTo(b.Deaths);
                });

                omegaList.Sort((a, b) =>
                {
                    int cmp = b.Kills.CompareTo(a.Kills);
                    if (cmp != 0) return cmp;
                    return a.Deaths.CompareTo(b.Deaths);
                });

                int alphaKills = tdm != null ? tdm.ScoreBlue : 0;
                int omegaKills = tdm != null ? tdm.ScoreRed : 0;
                if (alphaKills == 0 && omegaKills == 0)
                {
                    foreach (var a in alphaList) alphaKills += a.Kills;
                    foreach (var o in omegaList) omegaKills += o.Kills;
                }

                if (labelAlphaCount != null) labelAlphaCount.text = $"{alphaKills} KILLS";
                if (labelOmegaCount != null) labelOmegaCount.text = $"{omegaKills} KILLS";

                bool alphaWon = alphaKills > omegaKills;
                bool omegaWon = omegaKills > alphaKills;

                for (int i = 0; i < alphaList.Count; i++)
                {
                    var entry = alphaList[i];
                    bool isLocal = (entry.ClientId == localClientId);
                    CreateGameOverPlayerCard(contentAlpha, i + 1, entry, alphaWon && i == 0, isLocal, ColorSapphire);
                }

                for (int i = 0; i < omegaList.Count; i++)
                {
                    var entry = omegaList[i];
                    bool isLocal = (entry.ClientId == localClientId);
                    CreateGameOverPlayerCard(contentOmega, i + 1, entry, omegaWon && i == 0, isLocal, ColorRuby);
                }
            }
        }

        private void CreateGameOverPlayerCard(Transform parent, int rank, PlayerScoreEntry entry, bool isTop, bool isLocal, Color themeColor)
        {
            if (playerCardPrefab == null || parent == null) return;

            var card = Instantiate(playerCardPrefab, parent);
            var rect = card.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(parent.GetComponent<RectTransform>() != null ? parent.GetComponent<RectTransform>().rect.width - 16 : 380, 56);
            }

            var texts = card.GetComponentsInChildren<TextMeshProUGUI>();
            var images = card.GetComponentsInChildren<Image>();

            string displayName = PlayerNameRegistry.GetName(entry.ClientId);
            if (string.IsNullOrEmpty(displayName)) displayName = $"Player {entry.ClientId}";
            if (isLocal) displayName += " <color=#FFD555>(YOU)</color>";

            foreach (var t in texts)
            {
                if (t.gameObject.name == "PlayerName" || t.gameObject.name == "Name")
                {
                    t.text = $"#{rank}  {displayName}";
                    t.color = isLocal ? ColorGold : themeColor;
                }
                else if (t.gameObject.name == "StatusText" || t.gameObject.name == "RoleBadge" || t.gameObject.name == "ReadyStatus")
                {
                    // State kills clearly, no score PTS
                    t.text = isTop
                        ? $"<color=#FFD555><b>{entry.Kills} KILLS</b></color> <color=#A0B0C0><size=11>({entry.Deaths} D)</size></color>"
                        : $"<b>{entry.Kills} KILLS</b> <color=#A0B0C0><size=11>({entry.Deaths} D)</size></color>";
                    t.color = isTop ? ColorGold : ColorGreen;
                }
            }

            foreach (var img in images)
            {
                if (img.gameObject.name.Contains("Crown") || img.gameObject.name.Contains("HostIcon"))
                {
                    img.gameObject.SetActive(isTop);
                    if (isTop) img.color = ColorGold;
                }
                if (img.gameObject.name.Contains("ReadyBadge") || img.gameObject.name.Contains("StatusIndicator"))
                {
                    img.color = isTop ? ColorGold : themeColor;
                }
                if (img.gameObject.name.Contains("CardBorder") || img.gameObject.name.Contains("BorderGlow"))
                {
                    img.color = isTop ? ColorGold : themeColor;
                }
            }
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

            if (SessionManager.Instance != null && SessionManager.Instance.GetCurrentSession() != null)
            {
                try { await SessionManager.Instance.LeaveSession(); } catch { }
            }
            else
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.ShutdownInProgress)
                {
                    try { NetworkManager.Singleton.Shutdown(); } catch { }
                }
                SceneManager.LoadScene("MainMenu");
            }
        }
    }
}
