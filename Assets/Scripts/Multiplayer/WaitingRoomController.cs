using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Unity.Netcode;

namespace Shootopia
{
    /// <summary>
    /// Controls the Waiting Room (Lobby) UI and flow.
    /// Handles player list updates, per-player ready checks, room visibility toggling (host-only),
    /// join code copying, and transitioning players to the gameplay scene (host-only).
    /// </summary>
    public class WaitingRoomController : MonoBehaviour
    {
        public static bool IsLobbyMenuOpen { get; private set; } = true;

        [Header("Scene Configuration")]
        [SerializeField] private string gameplaySceneName = "Map1";
        [SerializeField] private UIDocument uiDocument;

        private VisualElement _root;
        private VisualElement _modalCard;
        private Label _lobbyRoleBadge;
        private Button _btnLeaveLobbyTop;
        private Label _roomNameLabel;
        private Label _roomModeMapLabel;
        private Label _roomPlayerCountLabel;
        private Label _roomCodeDisplay;
        private Button _btnCopyCode;
        private Label _btnCopyCodeLabel;
        private Label _rosterStatusSummary;
        private ScrollView _playerListContainer;
        private VisualElement _hostSettingsGroup;
        private Toggle _visibilityToggle;
        private Button _startMatchButton;
        private Button _readyButton;
        private Label _readyButtonLabel;
        private Button _btnLeaveLobby;
        private VisualElement _hudHint;

        private bool _isLocalReady = false;
        private bool _loadingGameplayScene = false;
        private bool _menuOpen = true;
        private Coroutine _copyFeedbackCoroutine;

        private void OnEnable()
        {
            CursorManager.PushUnlock("WaitingRoom");

            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

            if (uiDocument == null)
            {
                Debug.LogError("[WaitingRoomController] No UIDocument found!");
                return;
            }

            _root = uiDocument.rootVisualElement;
            if (_root == null) return;

            BindElements();
            RegisterCallbacks();

            // Initial UI Refresh
            RefreshLobbyUI();
            LoadingScreenManager.HideOverlay();
        }

        private void OnDisable()
        {
            CursorManager.PopUnlock("WaitingRoom");
            IsLobbyMenuOpen = false;

            UnregisterCallbacks();
        }

        private void BindElements()
        {
            _modalCard = _root.Q<VisualElement>("LobbyModalCard");
            _lobbyRoleBadge = _root.Q<Label>("LobbyRoleBadge");
            _btnLeaveLobbyTop = _root.Q<Button>("BtnLeaveLobbyTop");
            _roomNameLabel = _root.Q<Label>("RoomNameLabel");
            _roomModeMapLabel = _root.Q<Label>("RoomModeMapLabel");
            _roomPlayerCountLabel = _root.Q<Label>("RoomPlayerCountLabel");
            _roomCodeDisplay = _root.Q<Label>("RoomCodeDisplay");
            _btnCopyCode = _root.Q<Button>("BtnCopyCode");
            _btnCopyCodeLabel = _root.Q<Label>("BtnCopyCodeLabel");
            _rosterStatusSummary = _root.Q<Label>("RosterStatusSummary");
            _playerListContainer = _root.Q<ScrollView>("PlayerList");
            _hostSettingsGroup = _root.Q<VisualElement>("HostSettingsGroup");
            _visibilityToggle = _root.Q<Toggle>("VisibilityToggle");
            _startMatchButton = _root.Q<Button>("StartMatchButton");
            _readyButton = _root.Q<Button>("ReadyButton");
            _readyButtonLabel = _root.Q<Label>("ReadyButtonLabel");
            _btnLeaveLobby = _root.Q<Button>("BtnLeaveLobby");
            _hudHint = _root.Q<VisualElement>("LobbyHudHint");
        }

        private void RegisterCallbacks()
        {
            if (_btnLeaveLobbyTop != null) _btnLeaveLobbyTop.clicked += OnLeaveLobbyClicked;
            if (_btnLeaveLobby != null) _btnLeaveLobby.clicked += OnLeaveLobbyClicked;
            if (_btnCopyCode != null) _btnCopyCode.clicked += OnCopyCodeClicked;
            if (_readyButton != null) _readyButton.clicked += OnReadyClicked;
            if (_startMatchButton != null) _startMatchButton.clicked += OnStartMatchClicked;
            if (_visibilityToggle != null) _visibilityToggle.RegisterValueChangedCallback(OnVisibilityToggled);

            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.OnPlayerReadyChanged.AddListener(RefreshLobbyUI);
            }
        }

        private void UnregisterCallbacks()
        {
            if (_btnLeaveLobbyTop != null) _btnLeaveLobbyTop.clicked -= OnLeaveLobbyClicked;
            if (_btnLeaveLobby != null) _btnLeaveLobby.clicked -= OnLeaveLobbyClicked;
            if (_btnCopyCode != null) _btnCopyCode.clicked -= OnCopyCodeClicked;
            if (_readyButton != null) _readyButton.clicked -= OnReadyClicked;
            if (_startMatchButton != null) _startMatchButton.clicked -= OnStartMatchClicked;
            if (_visibilityToggle != null) _visibilityToggle.UnregisterValueChangedCallback(OnVisibilityToggled);

            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.OnPlayerReadyChanged.RemoveListener(RefreshLobbyUI);
            }
        }

        private void Update()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
            {
                ToggleMenu(!_menuOpen);
            }
        }

        private void ToggleMenu(bool open)
        {
            _menuOpen = open;
            IsLobbyMenuOpen = open;

            if (_modalCard != null)
            {
                _modalCard.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_root != null)
            {
                _root.style.backgroundColor = open ? new Color(0.02f, 0.04f, 0.07f, 0.82f) : new Color(0f, 0f, 0f, 0f);
            }

            if (open)
            {
                CursorManager.PushUnlock("WaitingRoom");
            }
            else
            {
                CursorManager.PopUnlock("WaitingRoom");
            }
        }

        private void OnCopyCodeClicked()
        {
            string code = SessionManager.Instance != null ? SessionManager.Instance.GetJoinCode() : string.Empty;
            if (!string.IsNullOrEmpty(code))
            {
                GUIUtility.systemCopyBuffer = code;
                Debug.Log($"[WaitingRoomController] Copied room code '{code}' to clipboard.");

                if (_copyFeedbackCoroutine != null) StopCoroutine(_copyFeedbackCoroutine);
                _copyFeedbackCoroutine = StartCoroutine(CopyFeedbackRoutine());
            }
        }

        private IEnumerator CopyFeedbackRoutine()
        {
            if (_btnCopyCodeLabel != null)
            {
                _btnCopyCodeLabel.text = "COPIED!";
            }
            yield return new WaitForSeconds(1.5f);
            if (_btnCopyCodeLabel != null)
            {
                _btnCopyCodeLabel.text = "COPY CODE";
            }
        }

        private async void OnReadyClicked()
        {
            _isLocalReady = !_isLocalReady;
            Debug.Log($"[WaitingRoomController] Local player ready status toggled to: {_isLocalReady}");

            if (_readyButtonLabel != null)
            {
                _readyButtonLabel.text = _isLocalReady ? "UNREADY" : "READY UP";
            }

            try
            {
                if (SessionManager.Instance != null)
                {
                    await SessionManager.Instance.SetReadyStatus(_isLocalReady);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WaitingRoomController] SetReadyStatus failed: {ex.Message}");
            }
        }

        private async void OnVisibilityToggled(ChangeEvent<bool> evt)
        {
            if (SessionManager.Instance == null) return;
            var currentSession = SessionManager.Instance.GetCurrentSession();
            if (currentSession == null) return;

            bool currentPublic = !currentSession.IsPrivate;
            if (evt.newValue == currentPublic) return;

            bool isPublic = evt.newValue;
            Debug.Log($"[WaitingRoomController] Toggling session visibility. Public: {isPublic}");

            try
            {
                await SessionManager.Instance.SetSessionVisibility(isPublic);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WaitingRoomController] SetSessionVisibility failed: {ex.Message}");
            }
        }

        private void OnStartMatchClicked()
        {
            if (_loadingGameplayScene) return;
            _loadingGameplayScene = true;
            Debug.Log("[WaitingRoomController] Host clicked Start Match.");
            LoadingScreenManager.ShowOverlay("DEPLOYING TO ARENA...");

            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.StartMatch();
            }
            else
            {
                string targetScene = !string.IsNullOrEmpty(gameplaySceneName) ? gameplaySceneName : "Map1";
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                {
                    NetworkManager.Singleton.SceneManager.LoadScene(targetScene, LoadSceneMode.Single);
                }
                else
                {
                    LoadingScreenManager.LoadScene(targetScene, "DEPLOYING TO ARENA...");
                }
            }
        }

        private async void OnLeaveLobbyClicked()
        {
            Debug.Log("[WaitingRoomController] Leave Lobby clicked. Leaving session...");
            LoadingScreenManager.ShowOverlay("LEAVING ROOM...");

            if (SessionManager.Instance != null)
            {
                try
                {
                    await SessionManager.Instance.LeaveSession();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[WaitingRoomController] Session leave note: {ex.Message}");
                }
            }
            else
            {
                LoadingScreenManager.LoadScene("MainMenu", "RETURNING TO COMMAND BASE...");
            }
        }

        public void RefreshLobbyUI()
        {
            if (_loadingGameplayScene) return;
            if (SessionManager.Instance == null) return;

            var currentSession = SessionManager.Instance.GetCurrentSession();
            if (currentSession == null) return;

            bool isHost = currentSession.IsHost;

            // Check if match has started (for clients)
            if (!isHost && currentSession.Properties != null && currentSession.Properties.TryGetValue("MatchStarted", out var matchStartedProp))
            {
                if (matchStartedProp.Value == "true")
                {
                    if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
                    {
                        Debug.Log("[WaitingRoomController] MatchStarted is true. Loading gameplay scene locally...");
                        _loadingGameplayScene = true;
                        SceneManager.LoadScene(gameplaySceneName);
                        return;
                    }
                    else
                    {
                        Debug.Log("[WaitingRoomController] MatchStarted is true. Waiting for NetworkSceneManager...");
                    }
                }
            }

            // Room Name & Details
            if (_roomNameLabel != null)
            {
                _roomNameLabel.text = !string.IsNullOrEmpty(currentSession.Name) ? currentSession.Name : "Cyber Arena";
            }

            string mode = "FFA";
            if (currentSession.Properties != null && currentSession.Properties.TryGetValue("GameMode", out var modeProp) && !string.IsNullOrEmpty(modeProp.Value))
            {
                mode = modeProp.Value;
            }

            string map = SessionManager.Instance.GetSessionMapName();
            if (_roomModeMapLabel != null)
            {
                _roomModeMapLabel.text = $"{mode.ToUpper()}  •  {map.ToUpper()}";
            }

            // Room Join Code
            string joinCode = SessionManager.Instance.GetJoinCode();
            if (_roomCodeDisplay != null)
            {
                _roomCodeDisplay.text = !string.IsNullOrEmpty(joinCode) ? joinCode : "------";
            }

            // Role Badge
            if (_lobbyRoleBadge != null)
            {
                _lobbyRoleBadge.text = isHost ? "HOST" : "MEMBER";
            }

            // Host controls vs Client controls
            if (_hostSettingsGroup != null)
            {
                _hostSettingsGroup.style.display = isHost ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_visibilityToggle != null && isHost)
            {
                _visibilityToggle.value = !currentSession.IsPrivate;
            }

            if (_startMatchButton != null)
            {
                _startMatchButton.style.display = isHost ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_readyButton != null)
            {
                _readyButton.style.display = !isHost ? DisplayStyle.Flex : DisplayStyle.None;
                if (_readyButtonLabel != null)
                {
                    _readyButtonLabel.text = _isLocalReady ? "UNREADY" : "READY UP";
                }
            }

            // Rebuild Player List
            if (_playerListContainer != null)
            {
                _playerListContainer.Clear();

                var playerStates = SessionManager.Instance.GetPlayerReadyStates();
                int playerCount = playerStates.Count;
                int maxPlayers = currentSession.MaxPlayers > 0 ? currentSession.MaxPlayers : 4;

                if (_roomPlayerCountLabel != null)
                {
                    _roomPlayerCountLabel.text = $"{playerCount}/{maxPlayers}";
                }

                bool allClientsReady = true;

                foreach (var state in playerStates)
                {
                    bool isPlayerHost = (state.playerId == currentSession.Host);

                    var row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row;
                    row.style.justifyContent = Justify.SpaceBetween;
                    row.style.alignItems = Align.Center;
                    row.style.paddingTop = 8f;
                    row.style.paddingBottom = 8f;
                    row.style.paddingLeft = 12f;
                    row.style.paddingRight = 12f;
                    row.style.marginBottom = 6f;
                    row.style.backgroundColor = new Color(0.07f, 0.11f, 0.18f, 0.85f);
                    row.style.borderTopLeftRadius = 8f;
                    row.style.borderTopRightRadius = 8f;
                    row.style.borderBottomLeftRadius = 8f;
                    row.style.borderBottomRightRadius = 8f;
                    row.style.borderLeftWidth = 4f;
                    row.style.borderLeftColor = (state.isReady || isPlayerHost)
                        ? new Color(0.18f, 0.8f, 0.44f)
                        : new Color(0.9f, 0.6f, 0.15f);

                    // Left: Name & Host Badge
                    var leftCol = new VisualElement();
                    leftCol.style.flexDirection = FlexDirection.Row;
                    leftCol.style.alignItems = Align.Center;

                    var nameLabel = new Label(state.displayName);
                    nameLabel.style.fontSize = 15f;
                    nameLabel.style.color = Color.white;
                    nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                    leftCol.Add(nameLabel);

                    if (isPlayerHost)
                    {
                        var hostTag = new Label("HOST");
                        hostTag.style.fontSize = 10f;
                        hostTag.style.marginLeft = 8f;
                        hostTag.style.paddingTop = 2f;
                        hostTag.style.paddingBottom = 2f;
                        hostTag.style.paddingLeft = 6f;
                        hostTag.style.paddingRight = 6f;
                        hostTag.style.borderTopLeftRadius = 4f;
                        hostTag.style.borderTopRightRadius = 4f;
                        hostTag.style.borderBottomLeftRadius = 4f;
                        hostTag.style.borderBottomRightRadius = 4f;
                        hostTag.style.backgroundColor = new Color(0.9f, 0.7f, 0.1f, 0.25f);
                        hostTag.style.color = new Color(1f, 0.85f, 0.2f);
                        hostTag.style.unityFontStyleAndWeight = FontStyle.Bold;
                        leftCol.Add(hostTag);
                    }

                    // Right: Ready Badge
                    bool ready = state.isReady || isPlayerHost;
                    var badge = new Label(ready ? "READY" : "WAITING");
                    badge.style.fontSize = 11f;
                    badge.style.paddingTop = 3f;
                    badge.style.paddingBottom = 3f;
                    badge.style.paddingLeft = 8f;
                    badge.style.paddingRight = 8f;
                    badge.style.borderTopLeftRadius = 4f;
                    badge.style.borderTopRightRadius = 4f;
                    badge.style.borderBottomLeftRadius = 4f;
                    badge.style.borderBottomRightRadius = 4f;
                    badge.style.unityFontStyleAndWeight = FontStyle.Bold;
                    badge.style.color = ready ? new Color(0.18f, 0.8f, 0.44f) : new Color(0.9f, 0.6f, 0.15f);
                    badge.style.backgroundColor = ready ? new Color(0.18f, 0.8f, 0.44f, 0.2f) : new Color(0.9f, 0.6f, 0.15f, 0.2f);

                    row.Add(leftCol);
                    row.Add(badge);
                    _playerListContainer.Add(row);

                    if (!isPlayerHost && !state.isReady)
                    {
                        allClientsReady = false;
                    }
                }

                if (_rosterStatusSummary != null)
                {
                    if (playerCount <= 1)
                    {
                        _rosterStatusSummary.text = "SOLO LOBBY (READY TO DEPLOY)";
                        _rosterStatusSummary.style.color = new Color(0.18f, 0.8f, 0.44f);
                    }
                    else if (allClientsReady)
                    {
                        _rosterStatusSummary.text = "ALL PLAYERS READY";
                        _rosterStatusSummary.style.color = new Color(0.18f, 0.8f, 0.44f);
                    }
                    else
                    {
                        _rosterStatusSummary.text = "WAITING FOR READY";
                        _rosterStatusSummary.style.color = new Color(0.9f, 0.6f, 0.15f);
                    }
                }

                if (_startMatchButton != null && isHost)
                {
                    _startMatchButton.SetEnabled(allClientsReady || playerCount == 1);
                }
            }
        }
    }
}
