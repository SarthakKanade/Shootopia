using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Unity.Netcode;

namespace Shootopia
{
    /// <summary>
    /// Controls the Waiting Room (Lobby) UI and flow.
    /// Handles player list updates, per-player ready checks, room visibility toggling (host-only),
    /// and transitioning players to the gameplay scene (host-only).
    /// </summary>
    public class WaitingRoomController : MonoBehaviour
    {
        [Header("Scene Configuration")]
        [SerializeField] private string gameplaySceneName = "GameScene";
        [SerializeField] private UIDocument uiDocument;

        private VisualElement _root;
        private VisualElement _playerListContainer;
        private Button _readyButton;
        private Button _startMatchButton;
        private Toggle _visibilityToggle;
        private Label _roomInfoLabel;

        private bool _isLocalReady = false;
        private bool _loadingGameplayScene = false;

        private void OnEnable()
        {
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

            // Bind UI Elements
            _playerListContainer = _root.Q<VisualElement>("PlayerList");
            _readyButton = _root.Q<Button>("ReadyButton");
            _startMatchButton = _root.Q<Button>("StartMatchButton");
            _visibilityToggle = _root.Q<Toggle>("VisibilityToggle");
            _roomInfoLabel = _root.Q<Label>("RoomInfoLabel");

            // Setup buttons
            if (_readyButton != null)
            {
                _readyButton.clicked += OnReadyClicked;
            }

            if (_startMatchButton != null)
            {
                _startMatchButton.clicked += OnStartMatchClicked;
            }

            if (_visibilityToggle != null)
            {
                _visibilityToggle.RegisterValueChangedCallback(OnVisibilityToggled);
            }

            // Register to SessionManager callbacks
            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.OnPlayerReadyChanged.AddListener(RefreshLobbyUI);
                SessionManager.Instance.OnSessionLeft.AddListener(OnSessionLeft);
                
                // Display room info
                if (_roomInfoLabel != null)
                {
                    string code = SessionManager.Instance.GetJoinCode();
                    _roomInfoLabel.text = $"Room Code: {code}";
                }
            }

            // Initial UI Refresh
            RefreshLobbyUI();
        }

        private void OnDisable()
        {
            if (_readyButton != null)
            {
                _readyButton.clicked -= OnReadyClicked;
            }

            if (_startMatchButton != null)
            {
                _startMatchButton.clicked -= OnStartMatchClicked;
            }

            if (_visibilityToggle != null)
            {
                _visibilityToggle.UnregisterValueChangedCallback(OnVisibilityToggled);
            }

            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.OnPlayerReadyChanged.RemoveListener(RefreshLobbyUI);
                SessionManager.Instance.OnSessionLeft.RemoveListener(OnSessionLeft);
            }
        }

        private async void OnReadyClicked()
        {
            _isLocalReady = !_isLocalReady;
            Debug.Log($"[WaitingRoomController] Local player ready status toggled to: {_isLocalReady}");
            
            if (_readyButton != null)
            {
                _readyButton.text = _isLocalReady ? "UNREADY" : "READY";
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

            // Prevent infinite recursion by checking if the value is already in sync with the session state
            bool currentPublic = !currentSession.IsPrivate;
            if (evt.newValue == currentPublic)
            {
                return;
            }

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

        private async void OnStartMatchClicked()
        {
            if (_loadingGameplayScene) return;
            _loadingGameplayScene = true;
            Debug.Log("[WaitingRoomController] Host clicked Start Match. Setting MatchStarted and loading gameplay scene...");
            if (SessionManager.Instance != null)
            {
                try
                {
                    await SessionManager.Instance.StartMatch();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WaitingRoomController] Failed to start match: {ex.Message}");
                    _loadingGameplayScene = false;
                    return;
                }
            }
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.SceneManager.LoadScene(gameplaySceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
            else
            {
                SceneManager.LoadScene(gameplaySceneName);
            }
        }

        private void OnSessionLeft()
        {
            Debug.Log("[WaitingRoomController] Stored session left. Returning to Main Menu...");
            // Go back to the first scene or Main Menu
            SceneManager.LoadScene(0);
        }

        private void RefreshLobbyUI()
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
                    // Only load scene locally if Netcode is NOT active/listening (as a fallback).
                    // If Netcode is listening, Netcode's NetworkSceneManager will load the scene automatically.
                    if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
                    {
                        Debug.Log("[WaitingRoomController] MatchStarted property is true. Netcode not active, loading gameplay scene locally...");
                        _loadingGameplayScene = true;
                        SceneManager.LoadScene(gameplaySceneName);
                        return;
                    }
                    else
                    {
                        Debug.Log("[WaitingRoomController] MatchStarted property is true. Netcode is active, waiting for NetworkSceneManager to load scene...");
                    }
                }
            }

            // Configure host-only controls visibility
            if (_startMatchButton != null)
            {
                _startMatchButton.style.display = isHost ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_visibilityToggle != null)
            {
                _visibilityToggle.style.display = isHost ? DisplayStyle.Flex : DisplayStyle.None;
                _visibilityToggle.value = !currentSession.IsPrivate;
            }

            // Clients see the ready toggle; hosts are implicitly ready
            if (_readyButton != null)
            {
                _readyButton.style.display = !isHost ? DisplayStyle.Flex : DisplayStyle.None;
            }

            // Rebuild the player list in UI
            if (_playerListContainer != null)
            {
                _playerListContainer.Clear();

                var playerStates = SessionManager.Instance.GetPlayerReadyStates();
                
                // Track if everyone is ready
                bool everyoneReady = true;

                foreach (var state in playerStates)
                {
                    var playerRow = new VisualElement();
                    playerRow.style.flexDirection = FlexDirection.Row;
                    playerRow.style.justifyContent = Justify.SpaceBetween;
                    playerRow.style.paddingTop = 4f;
                    playerRow.style.paddingBottom = 4f;

                    var nameLabel = new Label(state.displayName);
                    nameLabel.style.fontSize = 14f;
                    nameLabel.style.color = Color.white;

                    var statusLabel = new Label(state.isReady ? "Ready" : "Not Ready");
                    statusLabel.style.fontSize = 14f;
                    statusLabel.style.color = state.isReady ? Color.green : Color.red;

                    playerRow.Add(nameLabel);
                    playerRow.Add(statusLabel);
                    _playerListContainer.Add(playerRow);

                    if (!state.isReady)
                    {
                        everyoneReady = false;
                    }
                }

                // Host can start only if everyone is ready
                if (_startMatchButton != null && isHost)
                {
                    _startMatchButton.SetEnabled(everyoneReady);
                }
            }
        }
    }
}
