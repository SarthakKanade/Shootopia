using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Services.Multiplayer;
using Blocks.Sessions;
using Blocks.Common;

namespace Shootopia
{
    /// <summary>
    /// Bridges the pre-built Building Block UI elements to the central SessionManager.
    /// Intercepts UI button clicks, replaces default handlers, and drives UI updates from SessionManager calls.
    /// </summary>
    public class LobbyUIBridge : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIDocument uiDocument;

        private VisualElement _root;
        
        // Room Creation elements
        private TextField _roomNameInput;
        private Button _createPublicButton;
        private Button _createPrivateButton;
        
        // Room Join elements
        private TextField _joinCodeInput;
        private Button _joinButton;

        private void OnEnable()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

            if (uiDocument == null)
            {
                Debug.LogError("[LobbyUIBridge] No UIDocument found!");
                return;
            }

            _root = uiDocument.rootVisualElement;
            if (_root == null)
            {
                return;
            }

            // Setup bindings dynamically based on what elements exist in the root VisualElement
            SetupCreateRoomUI();
            SetupJoinRoomUI();
            SetupSessionBrowserUI();

            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.OnSessionError.AddListener(OnSessionErrorReceived);
            }
        }

        private void OnDisable()
        {
            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.OnSessionError.RemoveListener(OnSessionErrorReceived);
            }
        }

        private void SetupCreateRoomUI()
        {
            var createSessionElement = _root.Q<CreateSessionElement>();
            if (createSessionElement != null)
            {
                _roomNameInput = createSessionElement.Q<TextField>();
                var defaultCreateButton = createSessionElement.Q<Button>();

                // Hide the default CREATE button
                if (defaultCreateButton != null)
                {
                    defaultCreateButton.style.display = DisplayStyle.None;
                }

                var parent = defaultCreateButton?.parent ?? createSessionElement;

                // Add "Create Public Room" button if not already added
                _createPublicButton = parent.Q<Button>("CreatePublicButton");
                if (_createPublicButton == null)
                {
                    _createPublicButton = new Button { name = "CreatePublicButton", text = "CREATE PUBLIC" };
                    _createPublicButton.AddToClassList(BlocksTheme.Button);
                    _createPublicButton.style.marginRight = 5f;
                    _createPublicButton.clicked += CreatePublicRoom;
                    parent.Add(_createPublicButton);
                }

                // Add "Create Private Room" button if not already added
                _createPrivateButton = parent.Q<Button>("CreatePrivateButton");
                if (_createPrivateButton == null)
                {
                    _createPrivateButton = new Button { name = "CreatePrivateButton", text = "CREATE PRIVATE" };
                    _createPrivateButton.AddToClassList(BlocksTheme.Button);
                    _createPrivateButton.clicked += CreatePrivateRoom;
                    parent.Add(_createPrivateButton);
                }

                // Bind custom buttons enabled state based on whether there's a name input
                if (_roomNameInput != null)
                {
                    _roomNameInput.RegisterValueChangedCallback(evt => {
                        bool hasName = !string.IsNullOrEmpty(evt.newValue);
                        _createPublicButton.SetEnabled(hasName);
                        _createPrivateButton.SetEnabled(hasName);
                    });
                    bool hasNameInit = !string.IsNullOrEmpty(_roomNameInput.value);
                    _createPublicButton.SetEnabled(hasNameInit);
                    _createPrivateButton.SetEnabled(hasNameInit);
                }
            }
        }

        private void SetupJoinRoomUI()
        {
            var joinSessionByCode = _root.Q<JoinSessionByCode>();
            if (joinSessionByCode != null)
            {
                _joinCodeInput = joinSessionByCode.Q<TextField>();
                var defaultJoinButton = joinSessionByCode.Q<Button>();

                if (defaultJoinButton != null)
                {
                    // Hide default JOIN button
                    defaultJoinButton.style.display = DisplayStyle.None;

                    var parent = defaultJoinButton.parent ?? joinSessionByCode;
                    var customJoinButton = parent.Q<Button>("CustomJoinCodeButton");
                    if (customJoinButton == null)
                    {
                        customJoinButton = new Button { name = "CustomJoinCodeButton", text = "JOIN" };
                        customJoinButton.AddToClassList(BlocksTheme.Button);
                        customJoinButton.clicked += JoinByCode;
                        parent.Add(customJoinButton);
                    }

                    // Enable/disable based on join code length
                    if (_joinCodeInput != null)
                    {
                        _joinCodeInput.RegisterValueChangedCallback(evt => {
                            customJoinButton.SetEnabled(evt.newValue != null && evt.newValue.Length >= 6);
                        });
                        customJoinButton.SetEnabled(_joinCodeInput.value != null && _joinCodeInput.value.Length >= 6);
                    }
                }
            }
        }

        private void SetupSessionBrowserUI()
        {
            var sessionBrowser = _root.Q<SessionBrowserElement>();
            if (sessionBrowser != null)
            {
                // Override makeItem and bindItem to display display name, current player count, and game mode
                sessionBrowser.makeItem = () =>
                {
                    var container = new VisualElement();
                    container.AddToClassList(BlocksTheme.ContainerHorizontal);
                    container.AddToClassList(BlocksTheme.ScrollViewElement);
                    container.AddToClassList(BlocksTheme.ContainerSpaceBetween);

                    // Name
                    var sessionNameLabel = new Label { name = "SessionNameLabel" };
                    sessionNameLabel.AddToClassList(BlocksTheme.Label);
                    sessionNameLabel.AddToClassList(BlocksTheme.SpaceLeft);
                    container.Add(sessionNameLabel);

                    // Game Mode
                    var sessionModeLabel = new Label { name = "SessionModeLabel" };
                    sessionModeLabel.AddToClassList(BlocksTheme.Label);
                    sessionModeLabel.style.color = new Color(0.7f, 0.7f, 0.7f); // subtle gray color
                    container.Add(sessionModeLabel);

                    // Player Count
                    var sessionPlayerCountLabel = new Label { name = "SessionPlayerCountLabel" };
                    sessionPlayerCountLabel.AddToClassList(BlocksTheme.Label);
                    sessionPlayerCountLabel.AddToClassList(BlocksTheme.SpaceRight);
                    container.Add(sessionPlayerCountLabel);

                    return container;
                };

                sessionBrowser.bindItem = (element, index) =>
                {
                    var items = sessionBrowser.itemsSource;
                    if (items == null || index < 0 || index >= items.Count) return;

                    var sessionInfo = items[index] as ISessionInfo;
                    if (sessionInfo == null) return;

                    var nameLabel = element.Q<Label>("SessionNameLabel");
                    var modeLabel = element.Q<Label>("SessionModeLabel");
                    var playersLabel = element.Q<Label>("SessionPlayerCountLabel");

                    if (nameLabel != null)
                    {
                        nameLabel.text = sessionInfo.Name;
                    }
                    
                    if (playersLabel != null)
                    {
                        int currentPlayers = sessionInfo.MaxPlayers - sessionInfo.AvailableSlots;
                        playersLabel.text = $"{currentPlayers}/{sessionInfo.MaxPlayers} Players";
                    }

                    if (modeLabel != null)
                    {
                        string mode = "FFA"; // Fallback default
                        if (sessionInfo.Properties != null && sessionInfo.Properties.TryGetValue("GameMode", out var modeProp))
                        {
                            mode = modeProp.Value;
                        }
                        modeLabel.text = mode;
                    }
                };

                // Override JOIN button in the footer to call our custom join code
                var defaultJoin = sessionBrowser.Q<Button>(); // first button is Join
                if (defaultJoin != null && defaultJoin.text == "JOIN")
                {
                    defaultJoin.style.display = DisplayStyle.None;
                    var parent = defaultJoin.parent;
                    
                    var customJoin = parent.Q<Button>("CustomJoinButton");
                    if (customJoin == null)
                    {
                        customJoin = new Button { name = "CustomJoinButton", text = "JOIN" };
                        customJoin.AddToClassList(BlocksTheme.Button);
                        customJoin.style.marginRight = 5f;
                        customJoin.clicked += JoinSelectedSession;
                        parent.Insert(parent.IndexOf(defaultJoin), customJoin);
                    }

                    // Enable/disable custom join based on list selection
                    sessionBrowser.selectionChanged += (selectedObjects) => {
                        customJoin.SetEnabled(sessionBrowser.selectedIndex >= 0);
                    };
                    customJoin.SetEnabled(sessionBrowser.selectedIndex >= 0);
                }

                // Override REFRESH LIST button in the footer to query via SessionManager
                var buttons = sessionBrowser.Query<Button>().ToList();
                foreach (var btn in buttons)
                {
                    if (btn.text == "REFRESH LIST")
                    {
                        btn.style.display = DisplayStyle.None;
                        var parent = btn.parent;
                        
                        var customRefresh = parent.Q<Button>("CustomRefreshButton");
                        if (customRefresh == null)
                        {
                            customRefresh = new Button { name = "CustomRefreshButton", text = "REFRESH LIST" };
                            customRefresh.AddToClassList(BlocksTheme.Button);
                            customRefresh.clicked += RefreshSessionList;
                            parent.Add(customRefresh);
                        }
                    }
                }

                // Initial browser list populate
                RefreshSessionList();
            }
        }

        private async void CreatePublicRoom()
        {
            string roomName = _roomNameInput != null ? _roomNameInput.value : "Public Room";
            if (string.IsNullOrEmpty(roomName)) roomName = "Public Room";

            try
            {
                Debug.Log($"[LobbyUIBridge] Creating public session: {roomName}");
                if (SessionManager.Instance != null)
                {
                    await SessionManager.Instance.CreateSession(roomName, isPublic: true, maxPlayers: 4, gameMode: "FFA");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LobbyUIBridge] Create public room failed: {ex.Message}");
            }
        }

        private async void CreatePrivateRoom()
        {
            string roomName = _roomNameInput != null ? _roomNameInput.value : "Private Room";
            if (string.IsNullOrEmpty(roomName)) roomName = "Private Room";

            try
            {
                Debug.Log($"[LobbyUIBridge] Creating private session: {roomName}");
                if (SessionManager.Instance != null)
                {
                    await SessionManager.Instance.CreateSession(roomName, isPublic: false, maxPlayers: 4, gameMode: "FFA");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LobbyUIBridge] Create private room failed: {ex.Message}");
            }
        }

        private async void JoinByCode()
        {
            string code = _joinCodeInput != null ? _joinCodeInput.value : string.Empty;
            if (string.IsNullOrEmpty(code))
            {
                Debug.LogError("[LobbyUIBridge] Join code is empty!");
                return;
            }

            try
            {
                Debug.Log($"[LobbyUIBridge] Joining session by code: {code}");
                if (SessionManager.Instance != null)
                {
                    await SessionManager.Instance.JoinByCode(code.ToUpper());
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LobbyUIBridge] Join by code failed: {ex.Message}");
            }
        }

        private async void JoinSelectedSession()
        {
            var sessionBrowser = _root.Q<SessionBrowserElement>();
            if (sessionBrowser == null) return;

            int selectedIndex = sessionBrowser.selectedIndex;
            var items = sessionBrowser.itemsSource;
            if (items != null && selectedIndex >= 0 && selectedIndex < items.Count)
            {
                var sessionInfo = items[selectedIndex] as ISessionInfo;
                if (sessionInfo != null)
                {
                    try
                    {
                        Debug.Log($"[LobbyUIBridge] Joining selected session: {sessionInfo.Id}");
                        if (SessionManager.Instance != null)
                        {
                            await SessionManager.Instance.JoinById(sessionInfo.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[LobbyUIBridge] Join by ID failed: {ex.Message}");
                    }
                }
            }
        }

        private async void RefreshSessionList()
        {
            try
            {
                Debug.Log("[LobbyUIBridge] Refreshing session list...");
                if (SessionManager.Instance != null)
                {
                    var sessions = await SessionManager.Instance.GetPublicSessions();
                    var sessionBrowser = _root.Q<SessionBrowserElement>();
                    if (sessionBrowser != null)
                    {
                        sessionBrowser.itemsSource = sessions;
                        sessionBrowser.Rebuild();
                        Debug.Log($"[LobbyUIBridge] Session list refreshed with {sessions.Count} sessions.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LobbyUIBridge] Refresh failed: {ex.Message}");
            }
        }

        private void OnSessionErrorReceived(string error)
        {
            Debug.LogError($"[LobbyUIBridge] Session Error received: {error}");
            
            var errorLabel = _root.Q<Label>("ErrorToast");
            if (errorLabel == null)
            {
                errorLabel = new Label { name = "ErrorToast" };
                errorLabel.style.position = Position.Absolute;
                errorLabel.style.bottom = 20f;
                errorLabel.style.alignSelf = Align.Center;
                errorLabel.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 0.9f);
                errorLabel.style.color = Color.white;
                errorLabel.style.paddingLeft = 10f;
                errorLabel.style.paddingRight = 10f;
                errorLabel.style.paddingTop = 5f;
                errorLabel.style.paddingBottom = 5f;
                errorLabel.style.borderTopLeftRadius = 5f;
                errorLabel.style.borderTopRightRadius = 5f;
                errorLabel.style.borderBottomLeftRadius = 5f;
                errorLabel.style.borderBottomRightRadius = 5f;
                _root.Add(errorLabel);
            }

            errorLabel.text = error;
            errorLabel.style.display = DisplayStyle.Flex;

            StartCoroutine(HideErrorToast(errorLabel));
        }

        private System.Collections.IEnumerator HideErrorToast(Label label)
        {
            yield return new WaitForSeconds(4f);
            if (label != null)
            {
                label.style.display = DisplayStyle.None;
            }
        }
    }
}
