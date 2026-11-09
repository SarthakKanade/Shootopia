using UnityEngine;
using Unity.Netcode;

namespace Shootopia
{
    public partial class HUDController
    {
private void OnEnable()
        {
            LoadingScreenManager.HideOverlay();

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
            _audioSource.spatialBlend = 0f;

#if UNITY_EDITOR
            if (lowHealthHeartbeatClip == null)
            {
                lowHealthHeartbeatClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Anime SFX Vol 1 AmbroggioMusic/Low Health.mp3");
                if (_heartbeatSource != null) _heartbeatSource.clip = lowHealthHeartbeatClip;
            }
            if (respawnClip == null)
            {
                respawnClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Player/respawn_default.wav");
            }
#endif

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.CustomMessagingManager != null)
            {
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("Shootopia_MatchStarted", OnMatchStartedDirectMessage);
            }

            // Subscribe to static combat events
            GameModeBase.OnKillFeedReceived += OnKillFeedEvent;
            GameModeBase.OnSystemMessageReceived += AddSystemFeedEntry;
            PlayerHealth.OnLocalPlayerHitConfirmed += OnPlayerHitConfirmed;
            PlayerHealth.OnLocalPlayerDied += HandleLocalPlayerDied;
            PlayerHealth.OnLocalPlayerRespawned += HandleLocalPlayerRespawned;

            // Hide overlay banners initially
            EnsurePreMatchCountdownUI();
            EnsurePowerUpTimersUI();
            EnsureLowHealthSensoryUI();
            EnsureDeathBannerCountdownUI();
            EnsurePingBadgeUI();
            if (preMatchRoot != null) preMatchRoot.SetActive(false);
            if (eliminationBannerRoot != null) eliminationBannerRoot.SetActive(false);
            if (deathBannerRoot != null) deathBannerRoot.SetActive(false);
            if (hitMarkerRoot != null) hitMarkerRoot.SetActive(false);
            if (damageFlashGroup != null) damageFlashGroup.alpha = 0f;

            DeviceProfileManager.OnControlModeChanged += HandleControlModeChanged;
            if (playerStatusGroup != null) _defaultPlayerStatusPos = playerStatusGroup.anchoredPosition;
            if (weaponStatusGroup != null) _defaultWeaponStatusPos = weaponStatusGroup.anchoredPosition;
            ApplyControlModeLayout(DeviceProfileManager.EffectiveMode);

            EnsureMobileControls();
        }

        private void OnDisable()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.CustomMessagingManager != null)
            {
                NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler("Shootopia_MatchStarted");
            }

            DeviceProfileManager.OnControlModeChanged -= HandleControlModeChanged;

            if (_mobileControlsInstance != null)
            {
                Destroy(_mobileControlsInstance);
                _mobileControlsInstance = null;
            }

            GameModeBase.OnKillFeedReceived -= OnKillFeedEvent;
            GameModeBase.OnSystemMessageReceived -= AddSystemFeedEntry;
            PlayerHealth.OnLocalPlayerHitConfirmed -= OnPlayerHitConfirmed;
            PlayerHealth.OnLocalPlayerDied -= HandleLocalPlayerDied;
            PlayerHealth.OnLocalPlayerRespawned -= HandleLocalPlayerRespawned;

            if (_heartbeatSource != null && _heartbeatSource.isPlaying)
            {
                _heartbeatSource.Stop();
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetLowHealthMuffled(false);
            }
            if (_listenerLowPassFilter != null)
            {
                _listenerLowPassFilter.enabled = false;
                _listenerLowPassFilter.cutoffFrequency = 22000f;
            }

            CleanUpLocalPlayerReferences();

            if (_damageFlashRoutine != null) { StopCoroutine(_damageFlashRoutine); _damageFlashRoutine = null; }
            if (_hitMarkerRoutine != null) { StopCoroutine(_hitMarkerRoutine); _hitMarkerRoutine = null; }
            if (_eliminationBannerRoutine != null) { StopCoroutine(_eliminationBannerRoutine); _eliminationBannerRoutine = null; }
            if (_deathBannerRoutine != null) { StopCoroutine(_deathBannerRoutine); _deathBannerRoutine = null; }
            if (_preMatchRoutine != null) { StopCoroutine(_preMatchRoutine); _preMatchRoutine = null; }

            if (Instance == this) Instance = null;
        }

        private void OnMatchStartedDirectMessage(ulong senderClientId, FastBufferReader reader)
        {
            Debug.Log("[HUDController] Received direct Shootopia_MatchStarted message.");
            _matchStartedDirectly = true;
        }

        private void Update()
        {
            UpdateLocalPlayerReferences();
            UpdateTimerAndScore();
            UpdateWeaponUI();
            UpdateMultiKillTimer();
            UpdatePowerUpTimers();
            UpdateLowHealthSensory();
            UpdatePingBadge();
        }
    }
}
