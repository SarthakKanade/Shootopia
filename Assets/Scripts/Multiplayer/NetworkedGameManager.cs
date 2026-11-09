using Unity.Netcode;
using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// Master Networked Game Manager.
    /// Coordinates server-authoritative GameMode instantiation, match states,
    /// warmup lobbies, and scoreboard triggers.
    /// </summary>
    public partial class NetworkedGameManager : NetworkBehaviour
    {
        public static NetworkedGameManager Instance { get; private set; }

        private GameModeBase _activeGameMode;

        public readonly NetworkVariable<Unity.Collections.FixedString32Bytes> ActiveModeName = 
            new NetworkVariable<Unity.Collections.FixedString32Bytes>("FFA", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public readonly NetworkVariable<bool> MatchStarted = 
            new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public GameModeBase ActiveGameMode => _activeGameMode;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureRequiredSceneControllers();

            string initMode = "FFA";
            if (SessionManager.Instance != null)
                initMode = SessionManager.Instance.GetSessionGameMode();
            else if (PlayerPrefs.HasKey("Shootopia_ConfiguredGameMode"))
                initMode = PlayerPrefs.GetString("Shootopia_ConfiguredGameMode");

            ApplyActiveGameMode(initMode);
        }

        private void EnsureRequiredSceneControllers()
        {
            if (gameObject.GetComponent<ResultsScreenController>() == null) gameObject.AddComponent<ResultsScreenController>();
            if (gameObject.GetComponent<ScoreboardController>() == null) gameObject.AddComponent<ScoreboardController>();
            if (DisconnectHandler.Instance == null) new GameObject("DisconnectHandler").AddComponent<DisconnectHandler>();
            if (NetworkNotificationUI.Instance == null) new GameObject("NetworkNotificationUI").AddComponent<NetworkNotificationUI>();
        }

        private void Start() => TrySpawnSelf();

        private void Update()
        {
            if (!IsSpawned) TrySpawnSelf();

            bool isStarted = false;
            try
            {
                isStarted = (_activeGameMode != null && (_activeGameMode.MatchActive.Value || _activeGameMode.IsInProgress)) ||
                            (MatchStarted.Value && _activeGameMode != null && _activeGameMode.PreMatchCountdown.Value >= 0);
            }
            catch { }

            if (isStarted && !WaitingRoomController.IsGameOverOpen && WaitingRoomController.IsLobbyMenuOpen)
            {
                CloseLobbyUI();
            }
        }

        private void TrySpawnSelf()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && !IsSpawned)
            {
                var netObj = GetComponent<NetworkObject>();
                if (netObj != null && !netObj.IsSpawned)
                {
                    netObj.Spawn();
                    Debug.Log("[NetworkedGameManager] Auto-spawned NetworkObject on Server/Host.");
                }
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            ActiveModeName.OnValueChanged += OnActiveModeChanged;
            MatchStarted.OnValueChanged += OnMatchStartedChanged;
            GameModeBase.OnMatchEndedWithScoreboard += OnMatchEndedScoreboard;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.CustomMessagingManager != null)
            {
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("Shootopia_MatchStarted", OnMatchStartedCustomMessageReceived);
            }

            if (IsServer)
            {
                string selectedMode = SessionManager.Instance != null ? SessionManager.Instance.GetSessionGameMode() : "FFA";
                int configuredDuration = SessionManager.Instance != null ? SessionManager.Instance.GetSessionMatchDuration() : 300;
                int configuredScoreLimit = SessionManager.Instance != null ? SessionManager.Instance.GetSessionScoreLimit() : 10;

                ActiveModeName.Value = selectedMode;
                ApplyActiveGameMode(selectedMode);
                if (_activeGameMode != null) _activeGameMode.ConfigureMatchRules(configuredDuration, configuredScoreLimit);

                var waitingRoom = WaitingRoomController.Instance ?? FindAnyObjectByType<WaitingRoomController>(FindObjectsInactive.Include);
                if (waitingRoom != null && !waitingRoom.gameObject.activeSelf) waitingRoom.gameObject.SetActive(true);

                MatchStarted.Value = false;
                if (_activeGameMode != null) _activeGameMode.ResetMatchState();
            }
            else
            {
                string mode = ActiveModeName.Value.ToString();
                if (string.IsNullOrEmpty(mode) && SessionManager.Instance != null)
                    mode = SessionManager.Instance.GetSessionGameMode();
                ApplyActiveGameMode(mode);
                if (MatchStarted.Value || (_activeGameMode != null && _activeGameMode.MatchActive.Value)) CloseLobbyUI();
            }

            if (MatchStarted.Value) CloseLobbyUI();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            ActiveModeName.OnValueChanged -= OnActiveModeChanged;
            MatchStarted.OnValueChanged -= OnMatchStartedChanged;
            GameModeBase.OnMatchEndedWithScoreboard -= OnMatchEndedScoreboard;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.CustomMessagingManager != null)
            {
                NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler("Shootopia_MatchStarted");
            }
            if (_activeGameMode != null) _activeGameMode.MatchActive.OnValueChanged -= OnMatchActiveChanged;
        }

        private void OnActiveModeChanged(Unity.Collections.FixedString32Bytes previousValue, Unity.Collections.FixedString32Bytes newValue)
        {
            ApplyActiveGameMode(newValue.ToString());
        }

        private void OnMatchStartedChanged(bool previousValue, bool newValue)
        {
            if (newValue) CloseLobbyUI();
        }

        private void OnMatchActiveChanged(bool previousValue, bool newValue)
        {
            if (newValue && !previousValue) CloseLobbyUI();
        }

        private void OnMatchStartedCustomMessageReceived(ulong senderClientId, FastBufferReader reader)
        {
            CloseLobbyUI();
        }

        public void ApplyActiveGameMode(string selectedMode)
        {
            if (_activeGameMode != null) _activeGameMode.MatchActive.OnValueChanged -= OnMatchActiveChanged;

            var ffa = GetComponent<FFAMode>();
            var tdm = GetComponent<TDMMode>();
            bool isTDM = !string.IsNullOrEmpty(selectedMode) && selectedMode.ToUpper().Contains("TDM");

            if (isTDM)
            {
                _activeGameMode = tdm != null ? tdm : gameObject.AddComponent<TDMMode>();
                if (ffa != null) ffa.enabled = false;
                _activeGameMode.enabled = true;
            }
            else
            {
                _activeGameMode = ffa != null ? ffa : gameObject.AddComponent<FFAMode>();
                if (tdm != null) tdm.enabled = false;
                _activeGameMode.enabled = true;
            }

            if (_activeGameMode != null)
            {
                GameModeBase.SetActiveInstance(_activeGameMode);
                _activeGameMode.MatchActive.OnValueChanged += OnMatchActiveChanged;
                if (_activeGameMode.MatchActive.Value || _activeGameMode.IsInProgress || MatchStarted.Value) CloseLobbyUI();
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this) Instance = null;
        }
    }
}
