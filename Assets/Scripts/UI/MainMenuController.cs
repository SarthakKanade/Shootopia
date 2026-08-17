using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Services.Multiplayer;

namespace Shootopia
{
    /// <summary>
    /// Master UI Controller for the Smash Karts-inspired Shootopia Main Menu.
    /// Integrates Space Exploration GUI Kit assets, live 3D character preview,
    /// seamless matchmaking, custom room creation, code joining, and room browsing.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("UI Document")]
        [SerializeField] private UIDocument uiDocument;

        [Header("Audio Feedback")]
        [SerializeField] private AudioClip buttonClickClip;
        [SerializeField] private AudioClip errorClip;
        [SerializeField] private AudioClip menuMusicClip;

        private VisualElement _root;

        // ── Top Header & Profile ──
        private Button _btnProfilePill;
        private Label _playerNameLabel;
        private Label _coinAmountLabel;
        private Label _gemAmountLabel;
        private Label _trophyAmountLabel;
        private Button _btnToggleSound;
        private Button _btnTopSettings;
        private bool _isMuted = false;

        // ── Main Action Hub ──
        private Button _btnPlayMatch;
        private Button _btnFilterToggle;
        private Button _btnCreateRoom;
        private Button _btnJoinRoom;
        private Button _btnCustomize;
        private Button _btnQuit;

        // ── Modal: Create Room ──
        private VisualElement _modalCreateRoom;
        private Button _btnCloseCreateRoom;
        private TextField _inputCreateRoomName;
        private Button _chipModeFFA, _chipModeTDM;
        private SliderInt _sliderMaxPlayers;
        private Label _labelMaxPlayers;
        private Toggle _togglePublicRoom;
        private Button _btnConfirmCreate;
        private string _selectedCreateMode = "FFA";

        // ── Modal: Join Room (Browser & Code Tabs) ──
        private VisualElement _modalJoinRoom;
        private Button _btnCloseJoinRoom;
        private Button _tabBtnBrowse, _tabBtnCode;
        private VisualElement _tabContentBrowse, _tabContentCode;
        private Button _chipFilterAll, _chipFilterFFA, _chipFilterTDM;
        private Button _btnRefreshRooms;
        private ScrollView _roomListContainer;
        private VisualElement _browseEmptyState;
        private Button _btnEmptyCreate;
        private TextField _inputJoinCode;
        private Button _btnSubmitJoinCode;
        private string _browserModeFilter = "ALL";
        private List<ISessionInfo> _cachedSessions = new List<ISessionInfo>();

        // ── Modal: Quick Match Filter ──
        private VisualElement _modalQuickFilter;
        private Button _btnCloseQuickFilter;
        private Button _chipQuickAll, _chipQuickFFA, _chipQuickTDM;
        private Button _btnConfirmQuickFilter;
        private string _quickMatchMode = "ALL";

        // ── Modal: Customize ──
        private VisualElement _modalCustomize;
        private Button _btnCloseCustomize, _btnDoneCustomize;
        private Button _btnSkinAgent, _btnSkinRobot;
        private int _selectedSkin = 0;

        // ── Modal: Settings ──
        private VisualElement _modalSettings;
        private Button _btnCloseSettings, _btnApplySettings;
        private Slider _sliderMasterVol, _sliderMusicVol, _sliderSFXVol, _sliderMouseSens;
        private Label _labelMasterVol, _labelMusicVol, _labelSFXVol, _labelMouseSens;
        private Toggle _toggleInvertY, _toggleFullscreen;

        // ── Modal: Edit Profile ──
        private VisualElement _modalEditProfile;
        private Button _btnCloseProfile, _btnSaveProfile;
        private TextField _inputDisplayName;

        // ── Busy / Loading Overlay ──
        private VisualElement _busyOverlay;
        private Label _busyLabel;

        // ── Modal Stack & State ──
        private readonly Stack<VisualElement> _modalStack = new Stack<VisualElement>();
        private bool _isBusy = false;

        // ────────────────────── LIFECYCLE ──────────────────────

        private void OnEnable()
        {
            CursorManager.PushUnlock("MainMenu");

            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                Debug.LogError("[MainMenuController] No UIDocument component found!");
                return;
            }

            _root = uiDocument.rootVisualElement;
            if (_root == null) return;

            BindElements();
            RegisterCallbacks();
            RefreshProfileDisplay();

            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.OnSessionError.AddListener(OnSessionError);
            }
        }

        private async void Start()
        {
            // Play Menu Theme Music
            if (menuMusicClip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMusic(menuMusicClip, 0.8f);
            }

            // Ensure 3D Avatar Stage is bound to UI texture
            if (AvatarStageController.Instance != null)
            {
                AvatarStageController.Instance.BindToUIDocument();
            }

            // Ensure UGS and Auth are initialized in the background
            await MultiplayerAutoInitializer.EnsureInitializedAsync();
        }

        private void OnDisable()
        {
            CursorManager.PopUnlock("MainMenu");
            StopAllCoroutines();

            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.OnSessionError.RemoveListener(OnSessionError);
            }
        }

        // ────────────────────── ELEMENT BINDING ──────────────────────

        private void BindElements()
        {
            // Header
            _btnProfilePill = _root.Q<Button>("BtnProfilePill");
            _playerNameLabel = _root.Q<Label>("PlayerNameLabel");
            _coinAmountLabel = _root.Q<Label>("CoinAmountLabel");
            _gemAmountLabel = _root.Q<Label>("GemAmountLabel");
            _trophyAmountLabel = _root.Q<Label>("TrophyAmountLabel");
            _btnToggleSound = _root.Q<Button>("BtnToggleSound");
            _btnTopSettings = _root.Q<Button>("BtnTopSettings");

            // Main Actions
            _btnPlayMatch = _root.Q<Button>("BtnPlayMatch");
            _btnFilterToggle = _root.Q<Button>("BtnFilterToggle");
            _btnCreateRoom = _root.Q<Button>("BtnCreateRoom");
            _btnJoinRoom = _root.Q<Button>("BtnJoinRoom");
            _btnCustomize = _root.Q<Button>("BtnCustomize");
            _btnQuit = _root.Q<Button>("BtnQuit");

            // Modal: Create Room
            _modalCreateRoom = _root.Q<VisualElement>("ModalCreateRoom");
            _btnCloseCreateRoom = _root.Q<Button>("BtnCloseCreateRoom");
            _inputCreateRoomName = _root.Q<TextField>("InputCreateRoomName");
            _chipModeFFA = _root.Q<Button>("ChipModeFFA");
            _chipModeTDM = _root.Q<Button>("ChipModeTDM");
            _sliderMaxPlayers = _root.Q<SliderInt>("SliderMaxPlayers");
            _labelMaxPlayers = _root.Q<Label>("LabelMaxPlayers");
            _togglePublicRoom = _root.Q<Toggle>("TogglePublicRoom");
            _btnConfirmCreate = _root.Q<Button>("BtnConfirmCreate");

            // Modal: Join Room
            _modalJoinRoom = _root.Q<VisualElement>("ModalJoinRoom");
            _btnCloseJoinRoom = _root.Q<Button>("BtnCloseJoinRoom");
            _tabBtnBrowse = _root.Q<Button>("TabBtnBrowse");
            _tabBtnCode = _root.Q<Button>("TabBtnCode");
            _tabContentBrowse = _root.Q<VisualElement>("TabContentBrowse");
            _tabContentCode = _root.Q<VisualElement>("TabContentCode");
            _chipFilterAll = _root.Q<Button>("ChipFilterAll");
            _chipFilterFFA = _root.Q<Button>("ChipFilterFFA");
            _chipFilterTDM = _root.Q<Button>("ChipFilterTDM");
            _btnRefreshRooms = _root.Q<Button>("BtnRefreshRooms");
            _roomListContainer = _root.Q<ScrollView>("RoomListContainer");
            _browseEmptyState = _root.Q<VisualElement>("BrowseEmptyState");
            _btnEmptyCreate = _root.Q<Button>("BtnEmptyCreate");
            _inputJoinCode = _root.Q<TextField>("InputJoinCode");
            _btnSubmitJoinCode = _root.Q<Button>("BtnSubmitJoinCode");

            // Modal: Quick Match Filter
            _modalQuickFilter = _root.Q<VisualElement>("ModalQuickFilter");
            _btnCloseQuickFilter = _root.Q<Button>("BtnCloseQuickFilter");
            _chipQuickAll = _root.Q<Button>("ChipQuickAll");
            _chipQuickFFA = _root.Q<Button>("ChipQuickFFA");
            _chipQuickTDM = _root.Q<Button>("ChipQuickTDM");
            _btnConfirmQuickFilter = _root.Q<Button>("BtnConfirmQuickFilter");

            // Modal: Customize
            _modalCustomize = _root.Q<VisualElement>("ModalCustomize");
            _btnCloseCustomize = _root.Q<Button>("BtnCloseCustomize");
            _btnDoneCustomize = _root.Q<Button>("BtnDoneCustomize");
            _btnSkinAgent = _root.Q<Button>("BtnSkinAgent");
            _btnSkinRobot = _root.Q<Button>("BtnSkinRobot");

            // Modal: Settings
            _modalSettings = _root.Q<VisualElement>("ModalSettings");
            _btnCloseSettings = _root.Q<Button>("BtnCloseSettings");
            _btnApplySettings = _root.Q<Button>("BtnApplySettings");
            _sliderMasterVol = _root.Q<Slider>("SliderMasterVol");
            _labelMasterVol = _root.Q<Label>("LabelMasterVol");
            _sliderMusicVol = _root.Q<Slider>("SliderMusicVol");
            _labelMusicVol = _root.Q<Label>("LabelMusicVol");
            _sliderSFXVol = _root.Q<Slider>("SliderSFXVol");
            _labelSFXVol = _root.Q<Label>("LabelSFXVol");
            _sliderMouseSens = _root.Q<Slider>("SliderMouseSens");
            _labelMouseSens = _root.Q<Label>("LabelMouseSens");
            _toggleInvertY = _root.Q<Toggle>("ToggleInvertY");
            _toggleFullscreen = _root.Q<Toggle>("ToggleFullscreen");

            // Modal: Edit Profile
            _modalEditProfile = _root.Q<VisualElement>("ModalEditProfile");
            _btnCloseProfile = _root.Q<Button>("BtnCloseProfile");
            _inputDisplayName = _root.Q<TextField>("InputDisplayName");
            _btnSaveProfile = _root.Q<Button>("BtnSaveProfile");

            // Busy Overlay
            _busyOverlay = _root.Q<VisualElement>("BusyOverlay");
            _busyLabel = _root.Q<Label>("BusyLabel");

#if UNITY_WEBGL
            if (_btnQuit != null) _btnQuit.style.display = DisplayStyle.None;
            if (_toggleFullscreen != null) _toggleFullscreen.style.display = DisplayStyle.None;
#endif
        }

        // ────────────────────── CALLBACK REGISTRATION ──────────────────────

        private void RegisterCallbacks()
        {
            // Header & Utility
            SafeClick(_btnProfilePill, () => OpenModal(_modalEditProfile, SetupProfileModal));
            SafeClick(_btnTopSettings, () => OpenModal(_modalSettings, SetupSettingsModal));
            SafeClick(_btnToggleSound, ToggleMuteAudio);

            // Main Actions
            SafeClick(_btnPlayMatch, OnPlayMatchClicked);
            SafeClick(_btnFilterToggle, () => OpenModal(_modalQuickFilter));
            SafeClick(_btnCreateRoom, () => OpenModal(_modalCreateRoom));
            SafeClick(_btnJoinRoom, () => { OpenModal(_modalJoinRoom); SwitchJoinTab("Browse"); RefreshBrowserRooms(); });
            SafeClick(_btnCustomize, () => OpenModal(_modalCustomize));
            SafeClick(_btnQuit, OnQuitClicked);

            // Create Room Modal
            SafeClick(_btnCloseCreateRoom, CloseTopModal);
            SafeClick(_chipModeFFA, () => SetCreateMode("FFA"));
            SafeClick(_chipModeTDM, () => SetCreateMode("TDM"));
            if (_sliderMaxPlayers != null)
            {
                _sliderMaxPlayers.RegisterValueChangedCallback(evt =>
                {
                    if (_labelMaxPlayers != null) _labelMaxPlayers.text = evt.newValue.ToString();
                });
            }
            SafeClick(_btnConfirmCreate, OnConfirmCreateRoom);

            // Join Room Modal
            SafeClick(_btnCloseJoinRoom, CloseTopModal);
            SafeClick(_tabBtnBrowse, () => SwitchJoinTab("Browse"));
            SafeClick(_tabBtnCode, () => SwitchJoinTab("Code"));
            SafeClick(_chipFilterAll, () => SetBrowserFilter("ALL"));
            SafeClick(_chipFilterFFA, () => SetBrowserFilter("FFA"));
            SafeClick(_chipFilterTDM, () => SetBrowserFilter("TDM"));
            SafeClick(_btnRefreshRooms, RefreshBrowserRooms);
            SafeClick(_btnEmptyCreate, () => { CloseTopModal(); OpenModal(_modalCreateRoom); });
            if (_inputJoinCode != null)
            {
                _inputJoinCode.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue != null)
                        _inputJoinCode.SetValueWithoutNotify(evt.newValue.ToUpper());
                });
            }
            SafeClick(_btnSubmitJoinCode, OnSubmitJoinCode);

            // Quick Match Filter Modal
            SafeClick(_btnCloseQuickFilter, CloseTopModal);
            SafeClick(_chipQuickAll, () => SetQuickMatchMode("ALL"));
            SafeClick(_chipQuickFFA, () => SetQuickMatchMode("FFA"));
            SafeClick(_chipQuickTDM, () => SetQuickMatchMode("TDM"));
            SafeClick(_btnConfirmQuickFilter, () => { CloseTopModal(); OnPlayMatchClicked(); });

            // Customize Modal
            SafeClick(_btnCloseCustomize, CloseTopModal);
            SafeClick(_btnDoneCustomize, CloseTopModal);
            SafeClick(_btnSkinAgent, () => SelectSkin(0));
            SafeClick(_btnSkinRobot, () => SelectSkin(1));

            // Settings Modal
            SafeClick(_btnCloseSettings, CloseTopModal);
            SafeClick(_btnApplySettings, ApplyAndSaveSettings);

            if (_sliderMasterVol != null) _sliderMasterVol.RegisterValueChangedCallback(evt =>
            {
                if (_labelMasterVol != null) _labelMasterVol.text = $"{Mathf.RoundToInt(evt.newValue * 100)}%";
                if (AudioManager.Instance != null) AudioManager.Instance.SetChannelVolume(AudioChannel.Master, evt.newValue);
            });
            if (_sliderMusicVol != null) _sliderMusicVol.RegisterValueChangedCallback(evt =>
            {
                if (_labelMusicVol != null) _labelMusicVol.text = $"{Mathf.RoundToInt(evt.newValue * 100)}%";
                if (AudioManager.Instance != null) AudioManager.Instance.SetChannelVolume(AudioChannel.Music, evt.newValue);
            });
            if (_sliderSFXVol != null) _sliderSFXVol.RegisterValueChangedCallback(evt =>
            {
                if (_labelSFXVol != null) _labelSFXVol.text = $"{Mathf.RoundToInt(evt.newValue * 100)}%";
                if (AudioManager.Instance != null) AudioManager.Instance.SetChannelVolume(AudioChannel.SFX, evt.newValue);
            });
            if (_sliderMouseSens != null) _sliderMouseSens.RegisterValueChangedCallback(evt =>
            {
                if (_labelMouseSens != null) _labelMouseSens.text = $"{evt.newValue:F1}";
            });

            // Profile Modal
            SafeClick(_btnCloseProfile, CloseTopModal);
            SafeClick(_btnSaveProfile, OnSaveProfile);
        }

        private void SafeClick(Button btn, Action action)
        {
            if (btn != null) btn.clicked += () => { PlayButtonSound(); action(); };
        }

        // ────────────────────── MODAL STACK SYSTEM ──────────────────────

        private void OpenModal(VisualElement modal, Action onOpen = null)
        {
            if (modal == null) return;

            modal.style.display = DisplayStyle.Flex;
            _modalStack.Push(modal);

            modal.RemoveFromClassList("modal-backdrop--visible");
            var frame = modal.Q(className: "modal-frame");
            if (frame != null) frame.RemoveFromClassList("modal-card--visible");

            modal.schedule.Execute(() =>
            {
                modal.AddToClassList("modal-backdrop--visible");
                if (frame != null) frame.AddToClassList("modal-card--visible");
            }).ExecuteLater(20);

            onOpen?.Invoke();
        }

        private void CloseTopModal()
        {
            if (_modalStack.Count == 0) return;

            var modal = _modalStack.Pop();
            modal.RemoveFromClassList("modal-backdrop--visible");
            var frame = modal.Q(className: "modal-frame");
            if (frame != null) frame.RemoveFromClassList("modal-card--visible");

            modal.schedule.Execute(() =>
            {
                modal.style.display = DisplayStyle.None;
            }).ExecuteLater(220);
        }

        // ────────────────────── PLAY MATCH (QUICK MATCH) ──────────────────────

        private async void OnPlayMatchClicked()
        {
            if (_isBusy) return;
            _isBusy = true;

            ShowBusy("MATCHMAKING...");

            try
            {
                await MultiplayerAutoInitializer.EnsureInitializedAsync();

                if (SessionManager.Instance != null)
                {
                    var sessions = await SessionManager.Instance.GetPublicSessions();
                    ISessionInfo target = null;

                    foreach (var s in sessions)
                    {
                        if (s != null && s.AvailableSlots > 0)
                        {
                            if (_quickMatchMode == "ALL")
                            {
                                target = s;
                                break;
                            }

                            if (s.Properties != null && s.Properties.TryGetValue("GameMode", out var modeProp))
                            {
                                if (modeProp.Value.Equals(_quickMatchMode, StringComparison.OrdinalIgnoreCase))
                                {
                                    target = s;
                                    break;
                                }
                            }
                        }
                    }

                    if (target != null)
                    {
                        Debug.Log($"[MainMenuController] Matching session found: {target.Name} ({target.Id}). Joining...");
                        await SessionManager.Instance.JoinById(target.Id);
                    }
                    else
                    {
                        string hostMode = _quickMatchMode == "ALL" ? "FFA" : _quickMatchMode;
                        string myName = PlayerProfileManager.Instance != null ? PlayerProfileManager.Instance.DisplayName : "Agent";
                        string roomName = $"{myName}'s Arena";

                        Debug.Log($"[MainMenuController] No matching session found. Hosting {roomName} ({hostMode})...");
                        await SessionManager.Instance.CreateSession(roomName, isPublic: true, maxPlayers: 4, gameMode: hostMode, mapName: "Map1");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MainMenuController] Matchmaking failed: {ex.Message}");
                HideBusy();
                ShowNotification($"Matchmaking failed: {ex.Message}");
            }
            finally
            {
                _isBusy = false;
            }
        }

        private void SetQuickMatchMode(string mode)
        {
            _quickMatchMode = mode;
            SetChipActive(_chipQuickAll, mode == "ALL");
            SetChipActive(_chipQuickFFA, mode == "FFA");
            SetChipActive(_chipQuickTDM, mode == "TDM");
        }

        // ────────────────────── CREATE CUSTOM ROOM ──────────────────────

        private void SetCreateMode(string mode)
        {
            _selectedCreateMode = mode;
            SetChipActive(_chipModeFFA, mode == "FFA");
            SetChipActive(_chipModeTDM, mode == "TDM");
        }

        private async void OnConfirmCreateRoom()
        {
            if (_isBusy) return;
            _isBusy = true;

            string roomName = _inputCreateRoomName != null && !string.IsNullOrWhiteSpace(_inputCreateRoomName.value)
                ? _inputCreateRoomName.value.Trim()
                : $"{PlayerProfileManager.Instance?.DisplayName ?? "Agent"}'s Room";

            int maxPlayers = _sliderMaxPlayers != null ? _sliderMaxPlayers.value : 4;
            bool isPublic = _togglePublicRoom != null ? _togglePublicRoom.value : true;

            CloseTopModal();
            ShowBusy($"CREATING {roomName.ToUpper()}...");

            try
            {
                await MultiplayerAutoInitializer.EnsureInitializedAsync();

                if (SessionManager.Instance != null)
                {
                    await SessionManager.Instance.CreateSession(roomName, isPublic, maxPlayers, _selectedCreateMode, "Map1");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MainMenuController] CreateRoom failed: {ex.Message}");
                HideBusy();
                ShowNotification($"Failed to host arena: {ex.Message}");
            }
            finally
            {
                _isBusy = false;
            }
        }

        // ────────────────────── JOIN ROOM (BROWSER + CODE) ──────────────────────

        private void SwitchJoinTab(string tab)
        {
            if (_tabContentBrowse != null) _tabContentBrowse.style.display = tab == "Browse" ? DisplayStyle.Flex : DisplayStyle.None;
            if (_tabContentCode != null) _tabContentCode.style.display = tab == "Code" ? DisplayStyle.Flex : DisplayStyle.None;

            SetChipActive(_tabBtnBrowse, tab == "Browse");
            SetChipActive(_tabBtnCode, tab == "Code");

            if (tab == "Code" && _inputJoinCode != null)
            {
                _inputJoinCode.Focus();
            }
        }

        private void SetBrowserFilter(string filter)
        {
            _browserModeFilter = filter;
            SetChipActive(_chipFilterAll, filter == "ALL");
            SetChipActive(_chipFilterFFA, filter == "FFA");
            SetChipActive(_chipFilterTDM, filter == "TDM");
            RenderBrowserRooms();
        }

        private async void RefreshBrowserRooms()
        {
            if (_isBusy) return;
            _isBusy = true;

            if (_btnRefreshRooms != null) _btnRefreshRooms.SetEnabled(false);

            try
            {
                await MultiplayerAutoInitializer.EnsureInitializedAsync();

                if (SessionManager.Instance != null)
                {
                    _cachedSessions = await SessionManager.Instance.GetPublicSessions();
                    RenderBrowserRooms();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MainMenuController] RefreshBrowserRooms error: {ex.Message}");
                ShowNotification("Could not fetch online rooms. Check your connection.");
            }
            finally
            {
                _isBusy = false;
                if (_btnRefreshRooms != null) _btnRefreshRooms.SetEnabled(true);
            }
        }

        private void RenderBrowserRooms()
        {
            if (_roomListContainer == null) return;
            _roomListContainer.Clear();

            var filtered = new List<ISessionInfo>();
            foreach (var s in _cachedSessions)
            {
                if (s == null) continue;
                string mode = "FFA";
                if (s.Properties != null && s.Properties.TryGetValue("GameMode", out var prop))
                    mode = prop.Value;

                if (_browserModeFilter == "ALL" || _browserModeFilter.Equals(mode, StringComparison.OrdinalIgnoreCase))
                    filtered.Add(s);
            }

            bool hasRooms = filtered.Count > 0;
            if (_browseEmptyState != null) _browseEmptyState.style.display = hasRooms ? DisplayStyle.None : DisplayStyle.Flex;
            if (_roomListContainer != null) _roomListContainer.style.display = hasRooms ? DisplayStyle.Flex : DisplayStyle.None;

            foreach (var session in filtered)
            {
                _roomListContainer.Add(CreateRoomCard(session));
            }
        }

        private VisualElement CreateRoomCard(ISessionInfo session)
        {
            var card = new VisualElement();
            card.AddToClassList("room-card-arcade");

            // Left Side: Name + Tags
            var left = new VisualElement();
            left.AddToClassList("room-card-left");

            var nameLabel = new Label(session.Name ?? "Arena Match");
            nameLabel.AddToClassList("room-card-title");
            left.Add(nameLabel);

            var tagsRow = new VisualElement();
            tagsRow.AddToClassList("room-card-tags-row");

            string mode = "FFA";
            if (session.Properties != null && session.Properties.TryGetValue("GameMode", out var modeProp))
                mode = modeProp.Value;

            var modeBadge = new Label(mode);
            modeBadge.AddToClassList("tag-badge");
            modeBadge.AddToClassList(mode.Equals("TDM", StringComparison.OrdinalIgnoreCase) ? "tag-badge-tdm" : "tag-badge-ffa");
            tagsRow.Add(modeBadge);

            int currentPlayers = session.MaxPlayers - session.AvailableSlots;
            var playersBadge = new Label($"👤 {currentPlayers}/{session.MaxPlayers}");
            playersBadge.AddToClassList("tag-badge");
            playersBadge.AddToClassList("tag-badge-players");
            tagsRow.Add(playersBadge);

            left.Add(tagsRow);
            card.Add(left);

            // Right Side: Join Button
            var btnJoin = new Button(() => OnJoinSpecificRoom(session.Id)) { text = "JOIN" };
            btnJoin.AddToClassList("btn-card-join");
            card.Add(btnJoin);

            return card;
        }

        private async void OnJoinSpecificRoom(string sessionId)
        {
            if (_isBusy) return;
            _isBusy = true;

            CloseTopModal();
            ShowBusy("JOINING ARENA...");

            try
            {
                await MultiplayerAutoInitializer.EnsureInitializedAsync();

                if (SessionManager.Instance != null)
                {
                    await SessionManager.Instance.JoinById(sessionId);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MainMenuController] Join specific room failed: {ex.Message}");
                HideBusy();
                ShowNotification($"Could not join arena: {ex.Message}");
            }
            finally
            {
                _isBusy = false;
            }
        }

        private async void OnSubmitJoinCode()
        {
            if (_isBusy) return;

            string code = _inputJoinCode != null ? _inputJoinCode.value.Trim().ToUpper() : "";
            if (string.IsNullOrEmpty(code) || code.Length < 4)
            {
                ShowNotification("Please enter a valid room code.");
                return;
            }

            _isBusy = true;
            CloseTopModal();
            ShowBusy($"JOINING ROOM {code}...");

            try
            {
                await MultiplayerAutoInitializer.EnsureInitializedAsync();

                if (SessionManager.Instance != null)
                {
                    await SessionManager.Instance.JoinByCode(code);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MainMenuController] JoinByCode failed: {ex.Message}");
                HideBusy();
                ShowNotification($"Invalid or expired code: {ex.Message}");
            }
            finally
            {
                _isBusy = false;
            }
        }

        // ────────────────────── CUSTOMIZATION ──────────────────────

        private void SelectSkin(int skinIndex)
        {
            _selectedSkin = skinIndex;
            SetChipActive(_btnSkinAgent, skinIndex == 0);
            SetChipActive(_btnSkinRobot, skinIndex == 1);

            if (AvatarStageController.Instance != null)
            {
                AvatarStageController.Instance.SetSkin(skinIndex);
            }
        }

        // ────────────────────── SETTINGS & AUDIO ──────────────────────

        private void SetupSettingsModal()
        {
            if (_sliderMasterVol != null && _labelMasterVol != null)
            {
                float val = PlayerPrefs.GetFloat("Vol_Master", 0.8f);
                _sliderMasterVol.SetValueWithoutNotify(val);
                _labelMasterVol.text = $"{Mathf.RoundToInt(val * 100)}%";
            }

            if (_sliderMusicVol != null && _labelMusicVol != null)
            {
                float val = PlayerPrefs.GetFloat("Vol_Music", 0.6f);
                _sliderMusicVol.SetValueWithoutNotify(val);
                _labelMusicVol.text = $"{Mathf.RoundToInt(val * 100)}%";
            }

            if (_sliderSFXVol != null && _labelSFXVol != null)
            {
                float val = PlayerPrefs.GetFloat("Vol_SFX", 0.8f);
                _sliderSFXVol.SetValueWithoutNotify(val);
                _labelSFXVol.text = $"{Mathf.RoundToInt(val * 100)}%";
            }

            if (_sliderMouseSens != null && _labelMouseSens != null)
            {
                float sens = PlayerPrefs.GetFloat("MouseSens", 2.0f);
                _sliderMouseSens.SetValueWithoutNotify(sens);
                _labelMouseSens.text = $"{sens:F1}";
            }

            if (_toggleInvertY != null)
            {
                _toggleInvertY.SetValueWithoutNotify(PlayerPrefs.GetInt("InvertY", 0) == 1);
            }

            if (_toggleFullscreen != null)
            {
                _toggleFullscreen.SetValueWithoutNotify(Screen.fullScreen);
            }
        }

        private void ApplyAndSaveSettings()
        {
            if (_sliderMasterVol != null) PlayerPrefs.SetFloat("Vol_Master", _sliderMasterVol.value);
            if (_sliderMusicVol != null) PlayerPrefs.SetFloat("Vol_Music", _sliderMusicVol.value);
            if (_sliderSFXVol != null) PlayerPrefs.SetFloat("Vol_SFX", _sliderSFXVol.value);
            if (_sliderMouseSens != null) PlayerPrefs.SetFloat("MouseSens", _sliderMouseSens.value);
            if (_toggleInvertY != null) PlayerPrefs.SetInt("InvertY", _toggleInvertY.value ? 1 : 0);

            if (_toggleFullscreen != null)
            {
                Screen.fullScreen = _toggleFullscreen.value;
            }

            PlayerPrefs.Save();
            CloseTopModal();
            ShowNotification("Settings Saved Successfully!");
        }

        private void ToggleMuteAudio()
        {
            _isMuted = !_isMuted;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetChannelVolume(AudioChannel.Master, _isMuted ? 0f : 0.8f);
            }
        }

        // ────────────────────── PROFILE ──────────────────────

        private void SetupProfileModal()
        {
            if (_inputDisplayName != null)
            {
                string currentName = PlayerProfileManager.Instance != null
                    ? PlayerProfileManager.Instance.DisplayName
                    : "Agent_3293";
                _inputDisplayName.value = currentName;
            }
        }

        private void OnSaveProfile()
        {
            if (_inputDisplayName != null && !string.IsNullOrWhiteSpace(_inputDisplayName.value))
            {
                string newName = _inputDisplayName.value.Trim();
                if (PlayerProfileManager.Instance != null)
                {
                    PlayerProfileManager.Instance.SetDisplayName(newName);
                }
                RefreshProfileDisplay();
            }

            CloseTopModal();
        }

        private void RefreshProfileDisplay()
        {
            if (_playerNameLabel != null)
            {
                _playerNameLabel.text = PlayerProfileManager.Instance != null
                    ? PlayerProfileManager.Instance.DisplayName
                    : "Agent_3293";
            }
        }

        // ────────────────────── BUSY & NOTIFICATIONS ──────────────────────

        private void ShowBusy(string message)
        {
            if (_busyLabel != null) _busyLabel.text = message;
            if (_busyOverlay != null)
            {
                _busyOverlay.style.display = DisplayStyle.Flex;
                _busyOverlay.schedule.Execute(() => _busyOverlay.AddToClassList("modal-backdrop--visible")).ExecuteLater(10);
            }
        }

        private void HideBusy()
        {
            if (_busyOverlay != null)
            {
                _busyOverlay.RemoveFromClassList("modal-backdrop--visible");
                _busyOverlay.schedule.Execute(() => _busyOverlay.style.display = DisplayStyle.None).ExecuteLater(200);
            }
        }

        private void ShowNotification(string message)
        {
            Debug.Log($"[MainMenuController] Notification: {message}");
            NetworkNotificationUI.ShowToast(message, 3.5f, isError: false);
        }

        private void OnSessionError(string error)
        {
            HideBusy();
            NetworkNotificationUI.ShowToast(error, 4.5f, isError: true);
            PlayErrorSound();
        }

        private void SetChipActive(Button chip, bool active)
        {
            if (chip == null) return;
            if (active) chip.AddToClassList("mode-chip-btn--active");
            else chip.RemoveFromClassList("mode-chip-btn--active");
        }

        private void PlayButtonSound()
        {
            if (buttonClickClip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX2D(buttonClickClip);
            }
        }

        private void PlayErrorSound()
        {
            if (errorClip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX2D(errorClip);
            }
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
