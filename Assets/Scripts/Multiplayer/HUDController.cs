using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

namespace Shootopia
{
    /// <summary>
    /// Master UGUI Controller for the In-Game Gameplay HUD.
    /// Manages player health bar, equipped weapon/ammo display, velocity gauge,
    /// dynamic kill feed with weapon icons, match countdown timer, animated elimination banners,
    /// hit markers, and damage flash.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        public static HUDController Instance { get; private set; }

        [Header("Match Header (Call of Duty Style)")]
        [SerializeField] private GameObject matchHeaderRoot;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI matchLimitText;
        [SerializeField] private TextMeshProUGUI gameModeBadgeText;

        [Header("TDM Team Scores & Bars")]
        [SerializeField] private GameObject tdmScoreRoot;
        [SerializeField] private TextMeshProUGUI teamBlueNameText;
        [SerializeField] private TextMeshProUGUI teamBlueScoreText;
        [SerializeField] private Image teamBlueScoreBar;
        [SerializeField] private TextMeshProUGUI teamRedNameText;
        [SerializeField] private TextMeshProUGUI teamRedScoreText;
        [SerializeField] private Image teamRedScoreBar;

        [Header("FFA Score & Placement")]
        [SerializeField] private GameObject ffaScoreRoot;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI placementText;

        [Header("Pre-Match Countdown")]
        [SerializeField] private GameObject preMatchRoot;
        [SerializeField] private CanvasGroup preMatchGroup;
        [SerializeField] private TextMeshProUGUI preMatchCountdownText;
        [SerializeField] private TextMeshProUGUI preMatchSubText;
        [SerializeField] private AudioClip countdownBeepClip;
        [SerializeField] private AudioClip fightHornClip;

        [Header("Player Health & Velocity")]
        [SerializeField] private Image healthFill;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private Image velocityFill;
        [SerializeField] private TextMeshProUGUI velocityStreakText;

        [Header("Weapon & Ammo Display")]
        [SerializeField] private Image weaponIcon;
        [SerializeField] private TextMeshProUGUI weaponCategoryText;
        [SerializeField] private TextMeshProUGUI weaponNameText;
        [SerializeField] private TextMeshProUGUI ammoText;
        [SerializeField] private GameObject cooldownBarRoot;
        [SerializeField] private Image cooldownFill;

        [Header("Mobile Adaptation Layout")]
        [SerializeField] private RectTransform playerStatusGroup;
        [SerializeField] private RectTransform weaponStatusGroup;
        [SerializeField] private Vector2 mobilePlayerStatusOffset = new Vector2(0f, 150f);
        [SerializeField] private Vector2 mobileWeaponStatusOffset = new Vector2(0f, 180f);
        [SerializeField] private GameObject mobileControlsPrefab;
        private Vector2 _defaultPlayerStatusPos;
        private Vector2 _defaultWeaponStatusPos;
        private GameObject _mobileControlsInstance;

        [Header("Kill Feed & System Feed")]
        [SerializeField] private Transform killFeedContainer;
        [SerializeField] private GameObject killFeedEntryPrefab;

        [Header("Kill Feed & HUD Weapon Icons")]
        [SerializeField] private Sprite defaultWeaponSprite;
        [SerializeField] private Sprite pistolSprite;
        [SerializeField] private Sprite rifleSprite;
        [SerializeField] private Sprite sniperSprite;
        [SerializeField] private Sprite flameSprite;
        [SerializeField] private Sprite cryoSprite;
        [SerializeField] private Sprite voidSprite;
        [SerializeField] private Sprite bounceSprite;
        [SerializeField] private Sprite meleeSprite;
        [SerializeField] private Sprite blasterSprite;
        [SerializeField] private Sprite grenadeSprite;
        [SerializeField] private Sprite mineSprite;
        [SerializeField] private Sprite smokeSprite;
        [SerializeField] private Sprite skullSprite;

        [Header("Combat Feel & Juice")]
        [SerializeField] private CanvasGroup damageFlashGroup;
        [SerializeField] private GameObject hitMarkerRoot;
        [SerializeField] private Image[] hitMarkerTicks;

        [Header("Dynamic Elimination Banner (COD Style)")]
        [SerializeField] private GameObject eliminationBannerRoot;
        [SerializeField] private CanvasGroup eliminationBannerGroup;
        [SerializeField] private TextMeshProUGUI eliminationTitleText;
        [SerializeField] private TextMeshProUGUI eliminationSubtitleText;
        [SerializeField] private TextMeshProUGUI eliminationXpText;
        [SerializeField] private Image eliminationIcon;

        [Header("Death Recap Banner (COD Style)")]
        [SerializeField] private GameObject deathBannerRoot;
        [SerializeField] private CanvasGroup deathBannerGroup;
        [SerializeField] private TextMeshProUGUI deathTitleText;
        [SerializeField] private TextMeshProUGUI deathSubtitleText;
        [SerializeField] private Image deathIcon;

        [Header("Combat Audio")]
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private AudioClip killSound;
        [SerializeField] private AudioClip headshotKillSound;
        [SerializeField] private AudioClip deathSound;

        [Header("Active Power-Up Timers")]
        [SerializeField] private GameObject powerUpRoot;
        [SerializeField] private GameObject shieldBarRoot;
        [SerializeField] private Image shieldFill;
        [SerializeField] private TextMeshProUGUI shieldText;
        [SerializeField] private CanvasGroup shieldCanvasGroup;
        [SerializeField] private GameObject invisibilityBarRoot;
        [SerializeField] private Image invisibilityFill;
        [SerializeField] private TextMeshProUGUI invisibilityText;
        [SerializeField] private CanvasGroup invisibilityCanvasGroup;

        [Header("Low-Health Sensory Feedback (<30% HP)")]
        [SerializeField] private CanvasGroup lowHealthVignetteGroup;
        [SerializeField] private AudioClip lowHealthHeartbeatClip;

        [Header("Respawn Feedback")]
        [SerializeField] private AudioClip respawnClip;
        [SerializeField] private TextMeshProUGUI respawnCountdownText;
        [SerializeField] private Image respawnProgressBarFill;
        [SerializeField] private GameObject respawnCountdownRoot;

        [Header("Network Health (Ping / Latency Badge)")]
        [SerializeField] private GameObject pingBadgeRoot;
        [SerializeField] private TextMeshProUGUI pingText;
        [SerializeField] private Image pingIndicatorDot;
        private float _lastPingUpdateTime = 0f;

        private AudioSource _audioSource;
        private AudioSource _heartbeatSource;
        private AudioLowPassFilter _listenerLowPassFilter;
        private static Sprite _vignetteSprite;
        private bool _isWaitingForRespawn = false;

        private Coroutine _damageFlashRoutine;
        private Coroutine _hitMarkerRoutine;
        private Coroutine _eliminationBannerRoutine;
        private Coroutine _deathBannerRoutine;
        private Coroutine _preMatchRoutine;

        // Player caching
        private PlayerHealth _localHealth;
        private WeaponController _localWeaponController;
        private PlayerShield _localShield;
        private PlayerInvisibility _localInvisibility;

        // Multi-kill tracking
        private int _multiKillCount = 0;
        private float _multiKillTimer = 0f;
        private const float MultiKillWindow = 4.5f;

        // Countdown tracking
        private int _lastDisplayedCountdown = -999;
        private bool _matchStartedDirectly = false;

        private void Awake()
        {
            Instance = this;
            EnsurePreMatchCountdownUI();
            EnsurePowerUpTimersUI();
            EnsureLowHealthSensoryUI();
            EnsureDeathBannerCountdownUI();
            EnsurePingBadgeUI();
        }

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

        private void EnsureMobileControls()
        {
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            }

            if (_mobileControlsInstance != null || MobileControlsCanvas.Instance != null) return;

            GameObject prefabToSpawn = mobileControlsPrefab;
            if (prefabToSpawn == null)
            {
                prefabToSpawn = Resources.Load<GameObject>("UI/MobileControlsCanvas");
            }

            if (prefabToSpawn != null)
            {
                _mobileControlsInstance = Instantiate(prefabToSpawn);
                _mobileControlsInstance.name = "MobileControlsCanvas";
            }
        }

        private void HandleControlModeChanged(InputControlMode mode)
        {
            ApplyControlModeLayout(mode);
        }

        private void ApplyControlModeLayout(InputControlMode mode)
        {
            bool isMobile = (mode == InputControlMode.MobileTouch);
            if (playerStatusGroup != null)
            {
                playerStatusGroup.anchoredPosition = isMobile
                    ? _defaultPlayerStatusPos + mobilePlayerStatusOffset
                    : _defaultPlayerStatusPos;
            }
            if (weaponStatusGroup != null)
            {
                weaponStatusGroup.anchoredPosition = isMobile
                    ? _defaultWeaponStatusPos + mobileWeaponStatusOffset
                    : _defaultWeaponStatusPos;
            }
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

        private void UpdateMultiKillTimer()
        {
            if (_multiKillCount > 0)
            {
                _multiKillTimer -= Time.deltaTime;
                if (_multiKillTimer <= 0f)
                {
                    _multiKillCount = 0;
                }
            }
        }

        private void UpdateLocalPlayerReferences()
        {
            if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient == null)
            {
                CleanUpLocalPlayerReferences();
                return;
            }

            var localPlayerObj = NetworkManager.Singleton.LocalClient.PlayerObject;
            if (localPlayerObj == null)
            {
                CleanUpLocalPlayerReferences();
                return;
            }

            var hp = localPlayerObj.GetComponent<PlayerHealth>();
            var wc = localPlayerObj.GetComponent<WeaponController>();
            var shield = localPlayerObj.GetComponent<PlayerShield>();
            var invis = localPlayerObj.GetComponent<PlayerInvisibility>();

            if (hp != _localHealth)
            {
                if (_localHealth != null)
                {
                    _localHealth.CurrentHp.OnValueChanged -= OnHealthChanged;
                }
                _localHealth = hp;
                if (_localHealth != null)
                {
                    _localHealth.CurrentHp.OnValueChanged += OnHealthChanged;
                    OnHealthChanged(0f, _localHealth.CurrentHp.Value);
                }
            }

            _localWeaponController = wc;
            _localShield = shield;
            _localInvisibility = invis;
        }

        private void CleanUpLocalPlayerReferences()
        {
            if (_localHealth != null)
            {
                _localHealth.CurrentHp.OnValueChanged -= OnHealthChanged;
                _localHealth = null;
            }
            _localWeaponController = null;
            _localShield = null;
            _localInvisibility = null;
        }

        private void OnHealthChanged(float previousValue, float newValue)
        {
            if (_localHealth == null) return;
            float maxHp = _localHealth.GetMaxHp();
            if (maxHp <= 0) maxHp = 100f;
            float percentage = Mathf.Clamp01(newValue / maxHp);

            if (healthFill != null)
            {
                healthFill.fillAmount = percentage;
            }

            if (healthText != null)
            {
                healthText.text = $"{Mathf.RoundToInt(newValue)} / {Mathf.RoundToInt(maxHp)}";
            }

            // Damage flash vignette
            if (newValue < previousValue && previousValue > 0f)
            {
                float damageTaken = previousValue - newValue;
                TriggerDamageFlash(damageTaken);
            }
        }

        private void UpdateTimerAndScore()
        {
            var activeMode = NetworkedGameManager.Instance != null ? NetworkedGameManager.Instance.ActiveGameMode : GameModeBase.Instance;
            if (activeMode == null)
            {
                var ffaFallback = FindAnyObjectByType<FFAMode>();
                var tdmFallback = FindAnyObjectByType<TDMMode>();
                if (ffaFallback != null && ffaFallback.enabled) activeMode = ffaFallback;
                else if (tdmFallback != null && tdmFallback.enabled) activeMode = tdmFallback;
                else activeMode = (GameModeBase)ffaFallback ?? (GameModeBase)tdmFallback;
            }

            // 1. Handle Pre-Match Countdown (3... 2... 1... FIGHT!)
            if (activeMode != null)
            {
                int countdown = activeMode.PreMatchCountdown.Value;
                if (countdown != _lastDisplayedCountdown)
                {
                    _lastDisplayedCountdown = countdown;
                    HandleCountdownTick(countdown);
                }
            }

            bool isMatchOfficiallyStarted = _matchStartedDirectly || 
                                            (WaitingRoomController.Instance != null && WaitingRoomController.Instance.IsMatchStarted);
            try
            {
                isMatchOfficiallyStarted = isMatchOfficiallyStarted || 
                                           (NetworkedGameManager.Instance != null && NetworkedGameManager.Instance.MatchStarted.Value) ||
                                           (activeMode != null && (activeMode.MatchActive.Value || activeMode.IsInProgress));
            }
            catch { }

            // 2. Handle Match Time Display
            if (activeMode != null && (activeMode.IsInProgress || activeMode.MatchActive.Value))
            {
                float timeRemaining = activeMode.TimeRemaining;
                int minutes = Mathf.FloorToInt(timeRemaining / 60f);
                int seconds = Mathf.FloorToInt(timeRemaining % 60f);

                if (timerText != null)
                {
                    timerText.text = $"{minutes:00}:{seconds:00}";
                    // Pulse red when under 30 seconds
                    if (timeRemaining <= 30f && timeRemaining > 0f)
                    {
                        float pulse = (Mathf.Sin(Time.time * 8f) + 1f) * 0.5f;
                        timerText.color = Color.Lerp(Color.white, new Color(1f, 0.25f, 0.25f), pulse);
                    }
                    else
                    {
                        timerText.color = Color.white;
                    }
                }
            }
            else if (activeMode != null && activeMode.PreMatchCountdown.Value > 0)
            {
                if (timerText != null)
                {
                    timerText.text = $"STARTING IN {activeMode.PreMatchCountdown.Value}...";
                    timerText.color = new Color(0.0f, 0.94f, 1.0f);
                }
            }
            else
            {
                if (timerText != null)
                {
                    if (isMatchOfficiallyStarted)
                    {
                        float timeRemaining = activeMode != null ? activeMode.TimeRemaining : 300f;
                        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
                        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
                        timerText.text = $"{minutes:00}:{seconds:00}";
                        timerText.color = Color.white;
                    }
                    else
                    {
                        bool isLobbyOpen = WaitingRoomController.IsLobbyMenuOpen;
                        timerText.text = isLobbyOpen ? "WARMUP" : "05:00";
                        timerText.color = Color.white;
                    }
                }
            }

            // 3. Handle Score & Placement Tracking
            UpdateScoreAndPlacement(activeMode);
        }

        private void UpdateScoreAndPlacement(GameModeBase activeMode)
        {
            if (activeMode == null)
            {
                var ffaFallback = FindAnyObjectByType<FFAMode>();
                var tdmFallback = FindAnyObjectByType<TDMMode>();
                if (ffaFallback != null && ffaFallback.enabled) activeMode = ffaFallback;
                else if (tdmFallback != null && tdmFallback.enabled) activeMode = tdmFallback;
                else activeMode = (GameModeBase)ffaFallback ?? (GameModeBase)tdmFallback;
            }

            bool isMatchOfficiallyStarted = _matchStartedDirectly || 
                                            (WaitingRoomController.Instance != null && WaitingRoomController.Instance.IsMatchStarted);
            try
            {
                isMatchOfficiallyStarted = isMatchOfficiallyStarted || 
                                           (NetworkedGameManager.Instance != null && NetworkedGameManager.Instance.MatchStarted.Value) ||
                                           (activeMode != null && (activeMode.MatchActive.Value || activeMode.IsInProgress));
            }
            catch { }

            if (activeMode == null)
            {
                if (matchLimitText != null) matchLimitText.text = "";
                if (gameModeBadgeText != null) gameModeBadgeText.text = isMatchOfficiallyStarted ? "MATCH ACTIVE" : "WARMUP";
                if (tdmScoreRoot != null) tdmScoreRoot.SetActive(false);
                if (ffaScoreRoot != null) ffaScoreRoot.SetActive(false);
                if (placementText != null) placementText.text = "";
                if (scoreText != null) scoreText.text = "";
                return;
            }

            ulong localId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0;

            if (activeMode is TDMMode tdm)
            {
                if (tdmScoreRoot != null) tdmScoreRoot.SetActive(true);
                if (ffaScoreRoot != null) ffaScoreRoot.SetActive(false);

                int blue = tdm.ScoreBlue;
                int red = tdm.ScoreRed;
                int limit = (tdm != null && tdm.TargetScoreLimit.Value > 0) ? tdm.TargetScoreLimit.Value : 15;

                int localTeam = tdm.GetPlayerTeam(localId);

                if (teamBlueScoreText != null) teamBlueScoreText.text = blue.ToString();
                if (teamRedScoreText != null) teamRedScoreText.text = red.ToString();

                if (teamBlueScoreBar != null) teamBlueScoreBar.fillAmount = Mathf.Clamp01((float)blue / limit);
                if (teamRedScoreBar != null) teamRedScoreBar.fillAmount = Mathf.Clamp01((float)red / limit);

                if (teamBlueNameText != null)
                {
                    teamBlueNameText.text = (localTeam == 0) ? "ALLIES <size=11><color=#00F0FF>[YOU]</color></size>" : "BLUE";
                }
                if (teamRedNameText != null)
                {
                    teamRedNameText.text = (localTeam == 1) ? "AXIS <size=11><color=#FF4444>[YOU]</color></size>" : "RED";
                }

                if (matchLimitText != null) matchLimitText.text = $"FIRST TO {limit}";
                if (gameModeBadgeText != null) gameModeBadgeText.text = "TEAM DEATHMATCH";

                // Fallback backwards-compatible labels
                if (placementText != null) placementText.text = "TDM";
                if (scoreText != null) scoreText.text = $"<color=#00F0FF>{blue}</color> <size=13>—</size> <color=#FF4444>{red}</color>";
            }
            else if (activeMode is FFAMode ffa)
            {
                if (tdmScoreRoot != null) tdmScoreRoot.SetActive(false);
                if (ffaScoreRoot != null) ffaScoreRoot.SetActive(true);

                int kills = ffa.GetPlayerKills(localId);
                int rank = ffa.GetPlayerRank(localId);
                int totalPlayers = NetworkManager.Singleton != null ? NetworkManager.Singleton.ConnectedClientsList.Count : 1;
                int limit = (ffa != null && ffa.TargetScoreLimit.Value > 0) ? ffa.TargetScoreLimit.Value : 10;

                if (placementText != null)
                {
                    placementText.text = $"#{rank} <size=12><color=#8A99B5>/ {totalPlayers}</color></size>";
                }

                if (scoreText != null)
                {
                    scoreText.text = $"<color=#00F0FF>{kills}</color> <size=12><color=#8A99B5>/ {limit} KILLS</color></size>";
                }

                if (matchLimitText != null) matchLimitText.text = $"FIRST TO {limit}";
                if (gameModeBadgeText != null) gameModeBadgeText.text = "FREE FOR ALL";
            }
            else
            {
                if (tdmScoreRoot != null) tdmScoreRoot.SetActive(false);
                if (ffaScoreRoot != null) ffaScoreRoot.SetActive(false);
                if (gameModeBadgeText != null) gameModeBadgeText.text = isMatchOfficiallyStarted ? "MATCH ACTIVE" : "WARMUP";
                if (matchLimitText != null) matchLimitText.text = "";
            }
        }

        public void EnsurePreMatchCountdownUI()
        {
            if (preMatchRoot != null && preMatchCountdownText != null) return;

            // Check if already in canvas hierarchy
            var existing = transform.Find("PreMatchCountdownRoot");
            if (existing != null)
            {
                preMatchRoot = existing.gameObject;
                preMatchGroup = existing.GetComponent<CanvasGroup>();
                preMatchCountdownText = existing.Find("CountdownNumber")?.GetComponent<TextMeshProUGUI>();
                preMatchSubText = existing.Find("SubText")?.GetComponent<TextMeshProUGUI>();
                if (preMatchRoot != null && preMatchCountdownText != null)
                {
                    preMatchRoot.SetActive(false);
                    return;
                }
            }

            // Create PreMatchCountdownRoot in exact center of screen
            var rootObj = new GameObject("PreMatchCountdownRoot", typeof(RectTransform), typeof(CanvasGroup));
            rootObj.transform.SetParent(transform, false);
            var rt = rootObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 35f);
            rt.sizeDelta = new Vector2(480f, 260f);

            var cg = rootObj.GetComponent<CanvasGroup>();
            cg.alpha = 1f;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            // Background Card
            var bgObj = new GameObject("BackgroundPanel", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(rootObj.transform, false);
            var bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.pivot = new Vector2(0.5f, 0.5f);
            bgRt.anchoredPosition = Vector2.zero;
            bgRt.sizeDelta = Vector2.zero;
            var bgImg = bgObj.GetComponent<Image>();
            bgImg.color = new Color(0.04f, 0.07f, 0.15f, 0.90f);
            bgImg.raycastTarget = false;

            // Sample an existing sliced sprite if present on canvas
            var sampleImg = GetComponentInChildren<Image>();
            if (sampleImg != null && sampleImg.sprite != null)
            {
                bgImg.sprite = sampleImg.sprite;
                bgImg.type = Image.Type.Sliced;
            }

            // Glowing Accent Top & Bottom lines
            var topBarObj = new GameObject("TopAccent", typeof(RectTransform), typeof(Image));
            topBarObj.transform.SetParent(bgObj.transform, false);
            var topBarRt = topBarObj.GetComponent<RectTransform>();
            topBarRt.anchorMin = new Vector2(0.12f, 1f);
            topBarRt.anchorMax = new Vector2(0.88f, 1f);
            topBarRt.pivot = new Vector2(0.5f, 1f);
            topBarRt.anchoredPosition = new Vector2(0f, -6f);
            topBarRt.sizeDelta = new Vector2(0f, 3f);
            var topImg = topBarObj.GetComponent<Image>();
            topImg.color = new Color(0.0f, 0.94f, 1.0f, 0.9f);
            topImg.raycastTarget = false;

            var botBarObj = new GameObject("BottomAccent", typeof(RectTransform), typeof(Image));
            botBarObj.transform.SetParent(bgObj.transform, false);
            var botBarRt = botBarObj.GetComponent<RectTransform>();
            botBarRt.anchorMin = new Vector2(0.12f, 0f);
            botBarRt.anchorMax = new Vector2(0.88f, 0f);
            botBarRt.pivot = new Vector2(0.5f, 0f);
            botBarRt.anchoredPosition = new Vector2(0f, 6f);
            botBarRt.sizeDelta = new Vector2(0f, 3f);
            var botImg = botBarObj.GetComponent<Image>();
            botImg.color = new Color(0.0f, 0.94f, 1.0f, 0.9f);
            botImg.raycastTarget = false;

            // Subtitle text
            var subObj = new GameObject("SubText", typeof(RectTransform), typeof(TextMeshProUGUI));
            subObj.transform.SetParent(rootObj.transform, false);
            var subRt = subObj.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0f, 0.68f);
            subRt.anchorMax = new Vector2(1f, 0.96f);
            subRt.pivot = new Vector2(0.5f, 0.5f);
            subRt.anchoredPosition = Vector2.zero;
            subRt.sizeDelta = Vector2.zero;
            var subTmp = subObj.GetComponent<TextMeshProUGUI>();
            subTmp.text = "MATCH STARTING IN";
            subTmp.fontSize = 22f;
            subTmp.fontStyle = FontStyles.Bold;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = new Color(0.0f, 0.94f, 1.0f, 0.95f);
            subTmp.characterSpacing = 8f;
            subTmp.raycastTarget = false;

            // Main Countdown Number / Text
            var numObj = new GameObject("CountdownNumber", typeof(RectTransform), typeof(TextMeshProUGUI));
            numObj.transform.SetParent(rootObj.transform, false);
            var numRt = numObj.GetComponent<RectTransform>();
            numRt.anchorMin = new Vector2(0f, 0.05f);
            numRt.anchorMax = new Vector2(1f, 0.72f);
            numRt.pivot = new Vector2(0.5f, 0.5f);
            numRt.anchoredPosition = Vector2.zero;
            numRt.sizeDelta = Vector2.zero;
            var numTmp = numObj.GetComponent<TextMeshProUGUI>();
            numTmp.text = "3";
            numTmp.fontSize = 110f;
            numTmp.fontStyle = FontStyles.Bold;
            numTmp.alignment = TextAlignmentOptions.Center;
            numTmp.color = new Color(0.0f, 0.94f, 1.0f, 1.0f);
            numTmp.raycastTarget = false;

            // Match font with timerText or scoreText if assigned
            if (timerText != null && timerText.font != null)
            {
                subTmp.font = timerText.font;
                numTmp.font = timerText.font;
            }
            else if (scoreText != null && scoreText.font != null)
            {
                subTmp.font = scoreText.font;
                numTmp.font = scoreText.font;
            }

            preMatchRoot = rootObj;
            preMatchGroup = cg;
            preMatchCountdownText = numTmp;
            preMatchSubText = subTmp;
            rootObj.SetActive(false);
        }

        private void HandleCountdownTick(int countdown)
        {
            EnsurePreMatchCountdownUI();
            if (preMatchRoot == null || preMatchCountdownText == null) return;

            if (_preMatchRoutine != null) StopCoroutine(_preMatchRoutine);
            _preMatchRoutine = StartCoroutine(CountdownAnimateRoutine(countdown));
        }

        private IEnumerator CountdownAnimateRoutine(int countdown)
        {
            if (countdown > 0)
            {
                preMatchRoot.SetActive(true);
                if (preMatchGroup != null) preMatchGroup.alpha = 1f;

                if (preMatchSubText != null)
                {
                    preMatchSubText.text = "MATCH STARTING IN";
                    preMatchSubText.color = new Color(0.7f, 0.9f, 1.0f, 0.95f);
                }

                preMatchCountdownText.text = countdown.ToString();
                preMatchCountdownText.fontSize = 110f;

                Color numColor;
                switch (countdown)
                {
                    case 3:
                        numColor = new Color(0.0f, 0.94f, 1.0f); // Neon Cyan
                        break;
                    case 2:
                        numColor = new Color(1.0f, 0.85f, 0.15f); // Electric Amber
                        break;
                    case 1:
                        numColor = new Color(1.0f, 0.28f, 0.28f); // High-Alert Crimson
                        break;
                    default:
                        numColor = new Color(0.0f, 0.94f, 1.0f);
                        break;
                }
                preMatchCountdownText.color = numColor;

                if (countdownBeepClip != null && _audioSource != null)
                {
                    float originalPitch = _audioSource.pitch;
                    _audioSource.pitch = countdown == 1 ? 1.25f : (countdown == 2 ? 1.12f : 1.0f);
                    _audioSource.PlayOneShot(countdownBeepClip, 0.9f);
                    _audioSource.pitch = originalPitch;
                }

                // Punch scale with smooth spring bounce
                var rt = preMatchCountdownText.GetComponent<RectTransform>();
                if (rt != null)
                {
                    float popDuration = 0.28f;
                    float elapsed = 0f;
                    while (elapsed < popDuration)
                    {
                        elapsed += Time.deltaTime;
                        float t = Mathf.Clamp01(elapsed / popDuration);
                        float scale = 1f + (1f - t) * 0.55f * Mathf.Cos(t * Mathf.PI * 1.5f);
                        rt.localScale = Vector3.one * Mathf.Max(scale, 1f);
                        yield return null;
                    }
                    rt.localScale = Vector3.one;
                }

                yield return new WaitForSeconds(0.6f);
            }
            else if (countdown == 0)
            {
                preMatchRoot.SetActive(true);
                if (preMatchGroup != null) preMatchGroup.alpha = 1f;

                if (preMatchSubText != null)
                {
                    preMatchSubText.text = "ENGAGE ALL HOSTILES";
                    preMatchSubText.color = new Color(1.0f, 0.92f, 0.5f, 0.95f);
                }

                preMatchCountdownText.text = "FIGHT!";
                preMatchCountdownText.fontSize = 100f;
                preMatchCountdownText.color = new Color(1.0f, 0.88f, 0.1f); // Radiant Gold

                if (fightHornClip != null && _audioSource != null)
                {
                    _audioSource.PlayOneShot(fightHornClip, 1.0f);
                }

                // Impact punch scale for FIGHT!
                var rt = preMatchCountdownText.GetComponent<RectTransform>();
                if (rt != null)
                {
                    float popDuration = 0.38f;
                    float elapsed = 0f;
                    while (elapsed < popDuration)
                    {
                        elapsed += Time.deltaTime;
                        float t = Mathf.Clamp01(elapsed / popDuration);
                        float scale = 1f + (1f - t) * 0.85f * Mathf.Cos(t * Mathf.PI * 1.5f);
                        rt.localScale = Vector3.one * Mathf.Max(scale, 1f);
                        yield return null;
                    }
                    rt.localScale = Vector3.one;
                }

                yield return new WaitForSeconds(0.65f);

                // Smooth fade out & subtle expansion
                if (preMatchGroup != null)
                {
                    float elapsed = 0f;
                    float dur = 0.4f;
                    Vector3 startScale = preMatchRoot.transform.localScale;
                    while (elapsed < dur)
                    {
                        elapsed += Time.deltaTime;
                        float t = Mathf.Clamp01(elapsed / dur);
                        preMatchGroup.alpha = 1f - t;
                        preMatchRoot.transform.localScale = Vector3.Lerp(startScale, startScale * 1.15f, t);
                        yield return null;
                    }
                }

                preMatchRoot.SetActive(false);
                preMatchRoot.transform.localScale = Vector3.one;
                if (preMatchGroup != null) preMatchGroup.alpha = 1f;
            }
            else
            {
                preMatchRoot.SetActive(false);
            }
        }

        private void UpdateWeaponUI()
        {
            if (_localWeaponController == null || _localWeaponController.CurrentAttach == null)
            {
                if (weaponIcon != null)
                {
                    weaponIcon.sprite = pistolSprite != null ? pistolSprite : defaultWeaponSprite;
                    weaponIcon.color = Color.white;
                    weaponIcon.gameObject.SetActive(true);
                }
                if (weaponCategoryText != null) weaponCategoryText.text = "PISTOL";
                if (weaponNameText != null) weaponNameText.text = "SIDEARM";
                if (ammoText != null) ammoText.text = "∞";
                if (cooldownBarRoot != null) cooldownBarRoot.SetActive(false);
                return;
            }

            var attach = _localWeaponController.CurrentAttach;
            var weaponData = attach.gameplayData;

            string rawName = "";
            if (weaponData != null && !string.IsNullOrEmpty(weaponData.weaponName))
            {
                rawName = weaponData.weaponName;
            }
            else if (attach != null)
            {
                rawName = attach.name;
            }

            if (weaponIcon != null)
            {
                Sprite icon = (weaponData != null && weaponData.icon != null) ? weaponData.icon : GetWeaponSprite(rawName);
                if (icon != null)
                {
                    weaponIcon.sprite = icon;
                    weaponIcon.color = Color.white;
                    weaponIcon.gameObject.SetActive(true);
                }
            }

            if (weaponCategoryText != null)
            {
                weaponCategoryText.text = weaponData != null ? weaponData.category.ToString().ToUpper() : "WEAPON";
            }

            if (weaponNameText != null)
            {
                weaponNameText.text = !string.IsNullOrEmpty(rawName) ? rawName.ToUpper() : "WEAPON";
            }

            if (ammoText != null)
            {
                if (weaponData != null && weaponData.durabilityType == DurabilityType.Ammo)
                {
                    ammoText.text = $"{_localWeaponController.CurrentAmmo} / {weaponData.MagazineSize}";
                }
                else if (weaponData != null && weaponData.durabilityType == DurabilityType.Uses)
                {
                    ammoText.text = $"{_localWeaponController.CurrentUses} USES";
                }
                else if (weaponData != null && weaponData.durabilityType == DurabilityType.TimeBased)
                {
                    ammoText.text = $"{_localWeaponController.TimeRemaining:0.0}s";
                }
                else
                {
                    ammoText.text = "∞";
                }
            }
        }

        // ──────────────────────── COMBAT FEEL / JUICE ────────────────────────

        public void TriggerDamageFlash(float damage)
        {
            if (damageFlashGroup == null) return;
            if (_damageFlashRoutine != null) StopCoroutine(_damageFlashRoutine);
            _damageFlashRoutine = StartCoroutine(DamageFlashRoutine(Mathf.Clamp01(damage / 50f)));
        }

        private IEnumerator DamageFlashRoutine(float intensity)
        {
            damageFlashGroup.alpha = Mathf.Lerp(0.35f, 0.85f, intensity);
            float duration = 0.35f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                damageFlashGroup.alpha = Mathf.Lerp(intensity, 0f, elapsed / duration);
                yield return null;
            }
            damageFlashGroup.alpha = 0f;
        }

        private void OnPlayerHitConfirmed(bool isKill)
        {
            ShowHitMarker(isKill);
        }

        public void ShowHitMarker(bool isKill)
        {
            if (hitSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(hitSound, 0.7f);
            }

            if (hitMarkerRoot == null) return;
            if (_hitMarkerRoutine != null) StopCoroutine(_hitMarkerRoutine);
            _hitMarkerRoutine = StartCoroutine(HitMarkerRoutine(isKill));
        }

        private IEnumerator HitMarkerRoutine(bool isKill)
        {
            hitMarkerRoot.SetActive(true);
            Color tickColor = isKill ? new Color(1f, 0.2f, 0.2f, 1f) : new Color(1f, 1f, 1f, 1f);

            if (hitMarkerTicks != null)
            {
                foreach (var tick in hitMarkerTicks)
                {
                    if (tick != null) tick.color = tickColor;
                }
            }

            float duration = 0.22f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float a = Mathf.Lerp(1f, 0f, elapsed / duration);
                if (hitMarkerTicks != null)
                {
                    foreach (var tick in hitMarkerTicks)
                    {
                        if (tick != null) tick.color = new Color(tickColor.r, tickColor.g, tickColor.b, a);
                    }
                }
                yield return null;
            }

            hitMarkerRoot.SetActive(false);
        }

        // ──────────────────────── DYNAMIC ELIMINATION & DEATH BANNERS ────────────────────────

        public void ShowEliminationBanner(string victimName, string weaponName)
        {
            _multiKillCount++;
            _multiKillTimer = MultiKillWindow;

            if (killSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(_multiKillCount > 1 && headshotKillSound != null ? headshotKillSound : killSound, 1.0f);
            }

            if (eliminationBannerRoot == null) return;
            if (_eliminationBannerRoutine != null) StopCoroutine(_eliminationBannerRoutine);
            _eliminationBannerRoutine = StartCoroutine(EliminationBannerRoutine(victimName, weaponName, _multiKillCount));
        }

        private IEnumerator EliminationBannerRoutine(string victimName, string weaponName, int streak)
        {
            string streakTitle = "ELIMINATED";
            string xpAmount = "+100";
            if (streak == 2) { streakTitle = "DOUBLE KILL!"; xpAmount = "+200"; }
            else if (streak == 3) { streakTitle = "TRIPLE KILL!"; xpAmount = "+300"; }
            else if (streak >= 4) { streakTitle = "RAMPAGE!"; xpAmount = "+500"; }

            if (eliminationXpText != null)
            {
                eliminationXpText.text = $"{xpAmount} PTS";
                eliminationXpText.color = new Color(1.0f, 0.85f, 0.2f);
            }

            if (eliminationTitleText != null)
            {
                eliminationTitleText.text = streakTitle;
                eliminationTitleText.color = streak > 1 ? new Color(1.0f, 0.85f, 0.2f) : new Color(0.0f, 0.94f, 1.0f);
            }

            if (eliminationSubtitleText != null)
            {
                string wpn = string.IsNullOrEmpty(weaponName) ? "" : $" • <color=#8A99B5>{weaponName.ToUpper()}</color>";
                eliminationSubtitleText.text = $"<color=#FFFFFF>{victimName.ToUpper()}</color>{wpn}";
            }

            if (eliminationIcon != null && skullSprite != null)
            {
                eliminationIcon.sprite = skullSprite;
            }

            eliminationBannerRoot.SetActive(true);
            if (eliminationBannerGroup != null) eliminationBannerGroup.alpha = 1f;

            var rt = eliminationBannerRoot.GetComponent<RectTransform>();
            if (rt != null) UIJuice.PunchScale(rt, 0.32f);

            yield return new WaitForSeconds(2.0f);

            if (eliminationBannerGroup != null)
            {
                float duration = 0.35f;
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    eliminationBannerGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                    yield return null;
                }
            }

            eliminationBannerRoot.SetActive(false);
            if (eliminationBannerGroup != null) eliminationBannerGroup.alpha = 1f;
        }

        public void ShowDeathBanner(string killerName, string weaponName)
        {
            if (deathSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(deathSound, 0.9f);
            }

            if (deathBannerRoot == null) return;
            if (_deathBannerRoutine != null) StopCoroutine(_deathBannerRoutine);
            _deathBannerRoutine = StartCoroutine(DeathBannerRoutine(killerName, weaponName));
        }

        private void HandleLocalPlayerDied(float respawnDuration)
        {
            if (deathBannerRoot != null && !deathBannerRoot.activeSelf)
            {
                ShowDeathBanner("Hazard", "Environment");
            }
        }

        private void HandleLocalPlayerRespawned()
        {
            _isWaitingForRespawn = false;
        }

        private IEnumerator DeathBannerRoutine(string killerName, string weaponName)
        {
            EnsureDeathBannerCountdownUI();

            if (deathTitleText != null)
            {
                deathTitleText.text = "YOU WERE ELIMINATED";
                deathTitleText.color = new Color(1f, 0.3f, 0.3f);
            }

            if (deathSubtitleText != null)
            {
                string wpn = string.IsNullOrEmpty(weaponName) ? "" : $" WITH <color=#FFE066>{weaponName.ToUpper()}</color>";
                deathSubtitleText.text = $"KILLED BY <color=#FFFFFF>{killerName.ToUpper()}</color>{wpn}";
            }

            if (deathIcon != null && skullSprite != null)
            {
                deathIcon.sprite = skullSprite;
                deathIcon.color = new Color(1f, 0.3f, 0.3f, 0.9f);
            }

            deathBannerRoot.SetActive(true);
            if (deathBannerGroup != null) deathBannerGroup.alpha = 1f;

            var rt = deathBannerRoot.GetComponent<RectTransform>();
            if (rt != null) UIJuice.PunchScale(rt, 0.25f);

            float respawnDuration = 3.0f;
            float elapsed = 0f;
            _isWaitingForRespawn = true;

            while (elapsed < respawnDuration && _isWaitingForRespawn)
            {
                elapsed += Time.deltaTime;
                float remaining = Mathf.Max(0f, respawnDuration - elapsed);
                int seconds = Mathf.CeilToInt(remaining);

                if (respawnCountdownText != null)
                {
                    respawnCountdownText.text = $"RESPAWNING IN  <color=#FFE066><b>{seconds}</b></color>";
                }
                if (respawnProgressBarFill != null)
                {
                    respawnProgressBarFill.fillAmount = Mathf.Clamp01(1f - (elapsed / respawnDuration));
                }
                yield return null;
            }

            if (respawnCountdownText != null)
            {
                respawnCountdownText.text = "<color=#00F0FF><b>DEPLOYING...</b></color>";
            }
            if (respawnProgressBarFill != null)
            {
                respawnProgressBarFill.fillAmount = 0f;
            }

            if (respawnClip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(respawnClip, 0.9f);
            }

            yield return new WaitForSeconds(0.25f);

            if (deathBannerGroup != null)
            {
                float duration = 0.35f;
                float fadeElapsed = 0f;
                while (fadeElapsed < duration)
                {
                    fadeElapsed += Time.deltaTime;
                    deathBannerGroup.alpha = Mathf.Lerp(1f, 0f, fadeElapsed / duration);
                    yield return null;
                }
            }

            deathBannerRoot.SetActive(false);
            if (deathBannerGroup != null) deathBannerGroup.alpha = 1f;
            _isWaitingForRespawn = false;
        }

        private void EnsureDeathBannerCountdownUI()
        {
            if (deathBannerRoot == null) return;

            // Lower DeathBanner a little down from y = 150 to y = 95
            var rt = deathBannerRoot.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(0f, 95f);
                if (rt.sizeDelta.y < 120f)
                {
                    rt.sizeDelta = new Vector2(Mathf.Max(rt.sizeDelta.x, 420f), 120f);
                }
            }

            // Adjust Title & Subtitle vertical anchors so there is generous breathing room above countdown box
            if (deathTitleText != null)
            {
                var titleRt = deathTitleText.rectTransform;
                titleRt.anchorMin = new Vector2(0.18f, 0.58f);
                titleRt.anchorMax = new Vector2(0.92f, 0.90f);
            }
            if (deathSubtitleText != null)
            {
                var subRt = deathSubtitleText.rectTransform;
                subRt.anchorMin = new Vector2(0.08f, 0.35f);
                subRt.anchorMax = new Vector2(0.92f, 0.58f);
            }
            if (deathIcon != null)
            {
                var iconRt = deathIcon.rectTransform;
                iconRt.anchorMin = new Vector2(0.10f, 0.74f);
                iconRt.anchorMax = new Vector2(0.10f, 0.74f);
            }

            var existing = deathBannerRoot.transform.Find("RespawnCountdownBox");
            if (existing != null)
            {
                respawnCountdownRoot = existing.gameObject;
                var existBoxRt = existing.GetComponent<RectTransform>();
                if (existBoxRt != null)
                {
                    existBoxRt.anchoredPosition = new Vector2(0f, 6f);
                }
                respawnCountdownText = existing.Find("CountdownText")?.GetComponent<TextMeshProUGUI>();
                respawnProgressBarFill = existing.Find("ProgressBar/Fill")?.GetComponent<Image>();
                return;
            }

            var boxObj = new GameObject("RespawnCountdownBox", typeof(RectTransform));
            boxObj.transform.SetParent(deathBannerRoot.transform, false);
            var boxRt = boxObj.GetComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0.05f, 0f);
            boxRt.anchorMax = new Vector2(0.95f, 0f);
            boxRt.pivot = new Vector2(0.5f, 0f);
            boxRt.anchoredPosition = new Vector2(0f, 6f);
            boxRt.sizeDelta = new Vector2(0f, 32f);

            var txtObj = new GameObject("CountdownText", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(boxObj.transform, false);
            var txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = new Vector2(0f, 0.32f);
            txtRt.anchorMax = new Vector2(1f, 1f);
            txtRt.anchoredPosition = Vector2.zero;
            txtRt.sizeDelta = Vector2.zero;
            respawnCountdownText = txtObj.GetComponent<TextMeshProUGUI>();
            respawnCountdownText.text = "RESPAWNING IN  3";
            respawnCountdownText.fontSize = 15;
            respawnCountdownText.fontStyle = FontStyles.Bold;
            respawnCountdownText.alignment = TextAlignmentOptions.Center;
            respawnCountdownText.color = new Color(1f, 0.90f, 0.4f, 1f);

            var barBgObj = new GameObject("ProgressBar", typeof(RectTransform), typeof(Image));
            barBgObj.transform.SetParent(boxObj.transform, false);
            var barBgRt = barBgObj.GetComponent<RectTransform>();
            barBgRt.anchorMin = new Vector2(0.12f, 0f);
            barBgRt.anchorMax = new Vector2(0.88f, 0f);
            barBgRt.pivot = new Vector2(0.5f, 0f);
            barBgRt.anchoredPosition = new Vector2(0f, 2f);
            barBgRt.sizeDelta = new Vector2(0f, 4f);
            var barBgImg = barBgObj.GetComponent<Image>();
            barBgImg.color = new Color(0.1f, 0.15f, 0.25f, 0.8f);

            var fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(barBgObj.transform, false);
            var fillRt = fillObj.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.anchoredPosition = Vector2.zero;
            fillRt.sizeDelta = Vector2.zero;
            respawnProgressBarFill = fillObj.GetComponent<Image>();
            respawnProgressBarFill.color = new Color(1.0f, 0.82f, 0.2f, 0.95f);
            respawnProgressBarFill.type = Image.Type.Filled;
            respawnProgressBarFill.fillMethod = Image.FillMethod.Horizontal;
            respawnProgressBarFill.fillAmount = 1f;

            respawnCountdownRoot = boxObj;
        }

        // ──────────────────────── NETWORK HEALTH (PING / LATENCY) ────────────────────────

        public void EnsurePingBadgeUI()
        {
            if (pingBadgeRoot != null && pingText != null) return;

            Transform existing = transform.Find("PingBadgeRoot");
            if (existing != null)
            {
                pingBadgeRoot = existing.gameObject;
                pingText = existing.GetComponentInChildren<TextMeshProUGUI>();
                var dotTr = existing.Find("PingDot");
                if (dotTr != null) pingIndicatorDot = dotTr.GetComponent<Image>();
                return;
            }

            var badgeObj = new GameObject("PingBadgeRoot", typeof(RectTransform), typeof(Image));
            badgeObj.transform.SetParent(transform, false);

            var rt = badgeObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(105f, 28f);
            rt.anchoredPosition = new Vector2(-20f, -16f);

            var bgImg = badgeObj.GetComponent<Image>();
            bgImg.color = new Color(0.04f, 0.08f, 0.16f, 0.85f);

            // Subtle neon border frame or indicator
            var dotObj = new GameObject("PingDot", typeof(RectTransform), typeof(Image));
            dotObj.transform.SetParent(badgeObj.transform, false);

            var dRt = dotObj.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0f, 0.5f);
            dRt.anchorMax = new Vector2(0f, 0.5f);
            dRt.pivot = new Vector2(0.5f, 0.5f);
            dRt.sizeDelta = new Vector2(8f, 8f);
            dRt.anchoredPosition = new Vector2(14f, 0f);

            pingIndicatorDot = dotObj.GetComponent<Image>();
            pingIndicatorDot.color = new Color(0.15f, 0.95f, 0.75f, 1f);

            // Text
            var textObj = new GameObject("PingText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(badgeObj.transform, false);

            var tRt = textObj.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = new Vector2(24f, 2f);
            tRt.offsetMax = new Vector2(-6f, -2f);

            pingText = textObj.GetComponent<TextMeshProUGUI>();
            pingText.text = "-- ms";
            pingText.fontSize = 13;
            pingText.fontStyle = FontStyles.Bold;
            pingText.alignment = TextAlignmentOptions.MidlineLeft;
            pingText.color = new Color(0.85f, 0.95f, 1f, 1f);
            if (timerText != null) pingText.font = timerText.font;

            pingBadgeRoot = badgeObj;
        }

        private void UpdatePingBadge()
        {
            if (Time.unscaledTime - _lastPingUpdateTime < 0.5f) return;
            _lastPingUpdateTime = Time.unscaledTime;

            if (pingText == null) return;

            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            {
                if (pingBadgeRoot != null && pingBadgeRoot.activeSelf) pingBadgeRoot.SetActive(false);
                return;
            }

            if (pingBadgeRoot != null && !pingBadgeRoot.activeSelf) pingBadgeRoot.SetActive(true);

            bool isHost = NetworkManager.Singleton.IsHost;
            int ping = NetworkPingUtility.GetCurrentPing(NetworkManager.Singleton.LocalClientId);
            Color pingColor = NetworkPingUtility.GetPingColor(ping);

            pingText.text = NetworkPingUtility.FormatPingString(ping, isHost);
            pingText.color = pingColor;

            if (pingIndicatorDot != null)
            {
                pingIndicatorDot.color = pingColor;
            }
        }

        // ──────────────────────── POWER-UP TIMERS HUD ────────────────────────

        public void EnsurePowerUpTimersUI()
        {
            if (powerUpRoot != null && shieldBarRoot != null && invisibilityBarRoot != null) return;

            Transform existing = transform.Find("PowerUpStatusGroup");
            if (existing != null)
            {
                powerUpRoot = existing.gameObject;
                shieldBarRoot = existing.Find("ShieldCard")?.gameObject;
                shieldFill = existing.Find("ShieldCard/Fill")?.GetComponent<Image>();
                shieldText = existing.Find("ShieldCard/Label")?.GetComponent<TextMeshProUGUI>();
                shieldCanvasGroup = shieldBarRoot?.GetComponent<CanvasGroup>();

                invisibilityBarRoot = existing.Find("InvisCard")?.gameObject;
                invisibilityFill = existing.Find("InvisCard/Fill")?.GetComponent<Image>();
                invisibilityText = existing.Find("InvisCard/Label")?.GetComponent<TextMeshProUGUI>();
                invisibilityCanvasGroup = invisibilityBarRoot?.GetComponent<CanvasGroup>();

                if (shieldBarRoot != null && invisibilityBarRoot != null)
                {
                    shieldBarRoot.SetActive(false);
                    invisibilityBarRoot.SetActive(false);
                    return;
                }
            }

            // Create PowerUpStatusGroup anchored just above HealthFrame (bottom-left)
            var rootObj = new GameObject("PowerUpStatusGroup", typeof(RectTransform), typeof(VerticalLayoutGroup));
            rootObj.transform.SetParent(transform, false);
            var rt = rootObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(32f, 118f);
            rt.sizeDelta = new Vector2(310f, 70f);

            var vlg = rootObj.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 6f;
            vlg.childAlignment = TextAnchor.LowerLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            powerUpRoot = rootObj;

            // 1. Force Field Shield Card
            var shieldCard = new GameObject("ShieldCard", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            shieldCard.transform.SetParent(rootObj.transform, false);
            var scRt = shieldCard.GetComponent<RectTransform>();
            scRt.sizeDelta = new Vector2(310f, 28f);

            var scBg = shieldCard.GetComponent<Image>();
            scBg.color = new Color(0.04f, 0.08f, 0.16f, 0.92f);
            scBg.raycastTarget = false;
            shieldCanvasGroup = shieldCard.GetComponent<CanvasGroup>();

            // Cyan left accent border
            var scAccent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            scAccent.transform.SetParent(shieldCard.transform, false);
            var accRt = scAccent.GetComponent<RectTransform>();
            accRt.anchorMin = new Vector2(0f, 0f);
            accRt.anchorMax = new Vector2(0f, 1f);
            accRt.pivot = new Vector2(0f, 0.5f);
            accRt.anchoredPosition = Vector2.zero;
            accRt.sizeDelta = new Vector2(4f, 0f);
            var accImg = scAccent.GetComponent<Image>();
            accImg.color = new Color(0f, 0.94f, 1f, 0.95f);
            accImg.raycastTarget = false;

            // Fill Bar
            var scFillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            scFillObj.transform.SetParent(shieldCard.transform, false);
            var scFillRt = scFillObj.GetComponent<RectTransform>();
            scFillRt.anchorMin = Vector2.zero;
            scFillRt.anchorMax = Vector2.one;
            scFillRt.pivot = new Vector2(0f, 0.5f);
            scFillRt.anchoredPosition = Vector2.zero;
            scFillRt.sizeDelta = Vector2.zero;
            shieldFill = scFillObj.GetComponent<Image>();
            shieldFill.color = new Color(0f, 0.82f, 1f, 0.35f);
            shieldFill.type = Image.Type.Filled;
            shieldFill.fillMethod = Image.FillMethod.Horizontal;
            shieldFill.fillOrigin = 0;
            shieldFill.fillAmount = 1f;
            shieldFill.raycastTarget = false;

            // Label
            var scLabelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            scLabelObj.transform.SetParent(shieldCard.transform, false);
            var scLabelRt = scLabelObj.GetComponent<RectTransform>();
            scLabelRt.anchorMin = Vector2.zero;
            scLabelRt.anchorMax = Vector2.one;
            scLabelRt.anchoredPosition = new Vector2(10f, 0f);
            scLabelRt.sizeDelta = new Vector2(-16f, 0f);
            shieldText = scLabelObj.GetComponent<TextMeshProUGUI>();
            shieldText.text = "<color=#00F0FF>SHIELD</color>  50/50 HP  •  15.0s";
            shieldText.fontSize = 13;
            shieldText.fontStyle = FontStyles.Bold;
            shieldText.alignment = TextAlignmentOptions.MidlineLeft;
            shieldText.color = Color.white;
            shieldText.raycastTarget = false;

            shieldBarRoot = shieldCard;
            shieldBarRoot.SetActive(false);

            // 2. Invisibility Cloak Card
            var invisCard = new GameObject("InvisCard", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            invisCard.transform.SetParent(rootObj.transform, false);
            var icRt = invisCard.GetComponent<RectTransform>();
            icRt.sizeDelta = new Vector2(310f, 28f);

            var icBg = invisCard.GetComponent<Image>();
            icBg.color = new Color(0.09f, 0.04f, 0.18f, 0.92f);
            icBg.raycastTarget = false;
            invisibilityCanvasGroup = invisCard.GetComponent<CanvasGroup>();

            // Purple left accent border
            var icAccent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            icAccent.transform.SetParent(invisCard.transform, false);
            var icAccRt = icAccent.GetComponent<RectTransform>();
            icAccRt.anchorMin = new Vector2(0f, 0f);
            icAccRt.anchorMax = new Vector2(0f, 1f);
            icAccRt.pivot = new Vector2(0f, 0.5f);
            icAccRt.anchoredPosition = Vector2.zero;
            icAccRt.sizeDelta = new Vector2(4f, 0f);
            var icAccImg = icAccent.GetComponent<Image>();
            icAccImg.color = new Color(0.72f, 0.38f, 1f, 0.95f);
            icAccImg.raycastTarget = false;

            // Fill Bar
            var icFillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            icFillObj.transform.SetParent(invisCard.transform, false);
            var icFillRt = icFillObj.GetComponent<RectTransform>();
            icFillRt.anchorMin = Vector2.zero;
            icFillRt.anchorMax = Vector2.one;
            icFillRt.pivot = new Vector2(0f, 0.5f);
            icFillRt.anchoredPosition = Vector2.zero;
            icFillRt.sizeDelta = Vector2.zero;
            invisibilityFill = icFillObj.GetComponent<Image>();
            invisibilityFill.color = new Color(0.68f, 0.32f, 1f, 0.35f);
            invisibilityFill.type = Image.Type.Filled;
            invisibilityFill.fillMethod = Image.FillMethod.Horizontal;
            invisibilityFill.fillOrigin = 0;
            invisibilityFill.fillAmount = 1f;
            invisibilityFill.raycastTarget = false;

            // Label
            var icLabelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            icLabelObj.transform.SetParent(invisCard.transform, false);
            var icLabelRt = icLabelObj.GetComponent<RectTransform>();
            icLabelRt.anchorMin = Vector2.zero;
            icLabelRt.anchorMax = Vector2.one;
            icLabelRt.anchoredPosition = new Vector2(10f, 0f);
            icLabelRt.sizeDelta = new Vector2(-16f, 0f);
            invisibilityText = icLabelObj.GetComponent<TextMeshProUGUI>();
            invisibilityText.text = "<color=#D47BFF>STEALTH ACTIVE</color>  •  10.0s";
            invisibilityText.fontSize = 13;
            invisibilityText.fontStyle = FontStyles.Bold;
            invisibilityText.alignment = TextAlignmentOptions.MidlineLeft;
            invisibilityText.color = Color.white;
            invisibilityText.raycastTarget = false;

            invisibilityBarRoot = invisCard;
            invisibilityBarRoot.SetActive(false);
        }

        private void UpdatePowerUpTimers()
        {
            // 1. Force Field Shield
            if (_localShield != null && _localShield.IsShieldActive)
            {
                if (shieldBarRoot != null && !shieldBarRoot.activeSelf) shieldBarRoot.SetActive(true);

                float curHp = _localShield.CurrentShieldHp;
                float maxHp = _localShield.MaxShieldHp;
                float timeRem = _localShield.ShieldTimeRemaining;
                float maxDur = _localShield.MaxShieldDuration;

                if (shieldFill != null)
                {
                    shieldFill.fillAmount = maxDur > 0f ? Mathf.Clamp01(timeRem / maxDur) : 0f;
                }

                if (shieldText != null)
                {
                    string timeStr = timeRem > 0.05f ? $"{timeRem:F1}s" : "EXPIRING";
                    shieldText.text = $"<color=#00F0FF>SHIELD</color>  <b>{Mathf.RoundToInt(curHp)}</b>/{Mathf.RoundToInt(maxHp)} HP  •  {timeStr}";
                }

                // Pulse warning if <= 3.0s left
                if (timeRem <= 3.0f && shieldCanvasGroup != null)
                {
                    float pulse = 0.6f + 0.4f * Mathf.Sin(Time.time * 10f);
                    shieldCanvasGroup.alpha = pulse;
                }
                else if (shieldCanvasGroup != null)
                {
                    shieldCanvasGroup.alpha = 1f;
                }
            }
            else
            {
                if (shieldBarRoot != null && shieldBarRoot.activeSelf) shieldBarRoot.SetActive(false);
            }

            // 2. Invisibility Cloak
            if (_localInvisibility != null && _localInvisibility.IsInvisible)
            {
                if (invisibilityBarRoot != null && !invisibilityBarRoot.activeSelf) invisibilityBarRoot.SetActive(true);

                float timeRem = _localInvisibility.InvisibilityTimeRemaining;
                float totalDur = _localInvisibility.InvisibilityTotalDuration;

                if (invisibilityFill != null)
                {
                    invisibilityFill.fillAmount = totalDur > 0f ? Mathf.Clamp01(timeRem / totalDur) : 0f;
                }

                if (invisibilityText != null)
                {
                    string timeStr = timeRem > 0.05f ? $"{timeRem:F1}s" : "EXPIRING";
                    invisibilityText.text = $"<color=#D47BFF>STEALTH ACTIVE</color>  •  <b>{timeStr}</b>";
                }

                // Pulse warning if <= 2.5s left
                if (timeRem <= 2.5f && invisibilityCanvasGroup != null)
                {
                    float pulse = 0.6f + 0.4f * Mathf.Sin(Time.time * 12f);
                    invisibilityCanvasGroup.alpha = pulse;
                }
                else if (invisibilityCanvasGroup != null)
                {
                    invisibilityCanvasGroup.alpha = 1f;
                }
            }
            else
            {
                if (invisibilityBarRoot != null && invisibilityBarRoot.activeSelf) invisibilityBarRoot.SetActive(false);
            }
        }

        // ──────────────────────── LOW-HEALTH SENSORY FEEDBACK (<30% HP) ────────────────────────

        public void EnsureLowHealthSensoryUI()
        {
            if (lowHealthVignetteGroup != null && _heartbeatSource != null) return;

            Transform existing = transform.Find("LowHealthVignette");
            if (existing != null)
            {
                lowHealthVignetteGroup = existing.GetComponent<CanvasGroup>();
            }

            if (lowHealthVignetteGroup == null)
            {
                var vigObj = new GameObject("LowHealthVignette", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
                vigObj.transform.SetParent(transform, false);
                vigObj.transform.SetAsFirstSibling(); // Render beneath HUD text and crosshairs

                var rt = vigObj.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = Vector2.zero;

                lowHealthVignetteGroup = vigObj.GetComponent<CanvasGroup>();
                lowHealthVignetteGroup.alpha = 0f;
                lowHealthVignetteGroup.blocksRaycasts = false;
                lowHealthVignetteGroup.interactable = false;

                var img = vigObj.GetComponent<Image>();
                img.raycastTarget = false;

                if (_vignetteSprite == null)
                {
                    int w = 256, h = 256;
                    Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                    tex.wrapMode = TextureWrapMode.Clamp;
                    Vector2 center = new Vector2(w * 0.5f, h * 0.5f);
                    float maxDist = Vector2.Distance(Vector2.zero, center);
                    Color[] cols = new Color[w * h];
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            float d = Vector2.Distance(new Vector2(x, y), center);
                            float norm = Mathf.Clamp01(d / maxDist);
                            float a = Mathf.SmoothStep(0.2f, 1.0f, norm);
                            a = Mathf.Pow(a, 1.8f);
                            cols[y * w + x] = new Color(0.85f, 0.04f, 0.04f, a);
                        }
                    }
                    tex.SetPixels(cols);
                    tex.Apply();
                    _vignetteSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
                }
                img.sprite = _vignetteSprite;
                img.color = Color.white;
            }

            if (_heartbeatSource == null)
            {
                _heartbeatSource = gameObject.AddComponent<AudioSource>();
                _heartbeatSource.spatialBlend = 0f; // 2D Audio
                _heartbeatSource.loop = true;
                _heartbeatSource.playOnAwake = false;
                _heartbeatSource.priority = 64; // High priority combat audio
            }

            if (_heartbeatSource.clip == null && lowHealthHeartbeatClip != null)
            {
                _heartbeatSource.clip = lowHealthHeartbeatClip;
            }
        }

        private void EnsureAudioListenerFilter()
        {
            if (_listenerLowPassFilter != null) return;
            var listener = FindAnyObjectByType<AudioListener>();
            if (listener != null)
            {
                _listenerLowPassFilter = listener.GetComponent<AudioLowPassFilter>();
                if (_listenerLowPassFilter == null)
                {
                    _listenerLowPassFilter = listener.gameObject.AddComponent<AudioLowPassFilter>();
                }
                _listenerLowPassFilter.cutoffFrequency = 22000f;
                _listenerLowPassFilter.enabled = false;
            }
        }

        private void UpdateLowHealthSensory()
        {
            bool isAlive = _localHealth != null && !_localHealth.IsDead;
            float maxHp = isAlive ? _localHealth.GetMaxHp() : 100f;
            float curHp = isAlive ? _localHealth.CurrentHp.Value : 100f;
            float hpRatio = isAlive ? Mathf.Clamp01(curHp / Mathf.Max(1f, maxHp)) : 1f;

            bool isLowHealth = isAlive && hpRatio <= 0.30f && curHp > 0f;

            if (isLowHealth)
            {
                float severity = (0.30f - hpRatio) / 0.30f; // 0 at 30% HP, 1 at 0% HP

                // Pulsating red perimeter vignette
                float pulseHz = Mathf.Lerp(1.4f, 3.2f, severity);
                float pulseWave = 0.65f + 0.35f * Mathf.Sin(Time.time * pulseHz * Mathf.PI * 2f);
                float targetAlpha = Mathf.Lerp(0.35f, 0.85f, severity) * pulseWave;

                if (lowHealthVignetteGroup != null)
                {
                    lowHealthVignetteGroup.alpha = Mathf.MoveTowards(lowHealthVignetteGroup.alpha, targetAlpha, Time.deltaTime * 6f);
                }

                // Heartbeat audio loop
                if (_heartbeatSource != null && _heartbeatSource.clip != null)
                {
                    if (!_heartbeatSource.isPlaying)
                    {
                        _heartbeatSource.volume = 0.1f;
                        _heartbeatSource.Play();
                    }
                    float targetVol = Mathf.Lerp(0.25f, 0.90f, severity);
                    _heartbeatSource.volume = Mathf.MoveTowards(_heartbeatSource.volume, targetVol, Time.deltaTime * 2.5f);
                    _heartbeatSource.pitch = Mathf.Lerp(0.95f, 1.25f, severity);
                }

                // Audio muffling via AudioManager
                float targetCutoff = Mathf.Lerp(1600f, 650f, severity);
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.SetLowHealthMuffled(true, targetCutoff);
                }

                // Listener filter on AudioListener
                EnsureAudioListenerFilter();
                if (_listenerLowPassFilter != null)
                {
                    _listenerLowPassFilter.enabled = true;
                    _listenerLowPassFilter.cutoffFrequency = Mathf.MoveTowards(_listenerLowPassFilter.cutoffFrequency, targetCutoff, Time.deltaTime * 4000f);
                }
            }
            else
            {
                // Smooth recovery / fade out
                if (lowHealthVignetteGroup != null && lowHealthVignetteGroup.alpha > 0f)
                {
                    lowHealthVignetteGroup.alpha = Mathf.MoveTowards(lowHealthVignetteGroup.alpha, 0f, Time.deltaTime * 3.5f);
                }

                if (_heartbeatSource != null && _heartbeatSource.isPlaying)
                {
                    _heartbeatSource.volume = Mathf.MoveTowards(_heartbeatSource.volume, 0f, Time.deltaTime * 3.5f);
                    if (_heartbeatSource.volume <= 0.01f)
                    {
                        _heartbeatSource.Stop();
                    }
                }

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.SetLowHealthMuffled(false);
                }

                if (_listenerLowPassFilter != null && _listenerLowPassFilter.enabled)
                {
                    _listenerLowPassFilter.cutoffFrequency = Mathf.MoveTowards(_listenerLowPassFilter.cutoffFrequency, 22000f, Time.deltaTime * 12000f);
                    if (_listenerLowPassFilter.cutoffFrequency >= 21000f)
                    {
                        _listenerLowPassFilter.enabled = false;
                    }
                }
            }
        }

        // ──────────────────────── KILL FEED ────────────────────────

        public Sprite GetWeaponSprite(string weaponName)
        {
            if (string.IsNullOrEmpty(weaponName)) return defaultWeaponSprite != null ? defaultWeaponSprite : pistolSprite;
            string lower = weaponName.ToLower();
            if (lower.Contains("flame") || lower.Contains("fire")) return flameSprite != null ? flameSprite : defaultWeaponSprite;
            if (lower.Contains("freeze") || lower.Contains("cryo") || lower.Contains("ice")) return cryoSprite != null ? cryoSprite : defaultWeaponSprite;
            if (lower.Contains("void") || lower.Contains("gravity") || lower.Contains("blackhole")) return voidSprite != null ? voidSprite : defaultWeaponSprite;
            if (lower.Contains("bounce") || lower.Contains("trick") || lower.Contains("laser")) return bounceSprite != null ? bounceSprite : defaultWeaponSprite;
            if (lower.Contains("sniper")) return sniperSprite != null ? sniperSprite : defaultWeaponSprite;
            if (lower.Contains("knife") || lower.Contains("sword") || lower.Contains("melee") || lower.Contains("axe")) return meleeSprite != null ? meleeSprite : defaultWeaponSprite;
            if (lower.Contains("blaster") || lower.Contains("area") || lower.Contains("rocket") || lower.Contains("blast")) return blasterSprite != null ? blasterSprite : (rifleSprite != null ? rifleSprite : defaultWeaponSprite);
            if (lower.Contains("grenade") || lower.Contains("explosive") || lower.Contains("bomb")) return grenadeSprite != null ? grenadeSprite : defaultWeaponSprite;
            if (lower.Contains("mine") || lower.Contains("trip") || lower.Contains("trap")) return mineSprite != null ? mineSprite : defaultWeaponSprite;
            if (lower.Contains("smoke") || lower.Contains("canister") || lower.Contains("gas")) return smokeSprite != null ? smokeSprite : defaultWeaponSprite;
            if (lower.Contains("pistol") || lower.Contains("sidearm")) return pistolSprite != null ? pistolSprite : defaultWeaponSprite;
            if (lower.Contains("machine") || lower.Contains("rifle") || lower.Contains("gun") || lower.Contains("shooter") || lower.Contains("ar-15")) return rifleSprite != null ? rifleSprite : defaultWeaponSprite;
            return defaultWeaponSprite != null ? defaultWeaponSprite : pistolSprite;
        }

        private void OnKillFeedEvent(string killerName, string victimName, string weaponName, ulong killerId, ulong victimId)
        {
            ulong localClientId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 9999;
            bool isLocalKiller = (killerId == localClientId);
            bool isLocalVictim = (victimId == localClientId);

            var activeMode = NetworkedGameManager.Instance != null ? NetworkedGameManager.Instance.ActiveGameMode : GameModeBase.Instance;
            if (activeMode == null)
            {
                var ffaFallback = FindAnyObjectByType<FFAMode>();
                var tdmFallback = FindAnyObjectByType<TDMMode>();
                if (ffaFallback != null && ffaFallback.enabled) activeMode = ffaFallback;
                else if (tdmFallback != null && tdmFallback.enabled) activeMode = tdmFallback;
                else activeMode = (GameModeBase)ffaFallback ?? (GameModeBase)tdmFallback;
            }
            bool isKillerFriendly = false;
            bool isVictimFriendly = false;

            if (activeMode is TDMMode tdm)
            {
                int localTeam = tdm.GetPlayerTeam(localClientId);
                int killerTeam = tdm.GetPlayerTeam(killerId);
                int victimTeam = tdm.GetPlayerTeam(victimId);
                if (localTeam >= 0)
                {
                    isKillerFriendly = (killerTeam == localTeam);
                    isVictimFriendly = (victimTeam == localTeam);
                }
            }

            // Trigger local player banners
            if (isLocalKiller && !isLocalVictim)
            {
                ShowEliminationBanner(victimName, weaponName);
            }
            else if (isLocalVictim)
            {
                string displayKiller = (killerId == victimId) ? (weaponName == "Lava" ? "Lava" : "Self") : killerName;
                ShowDeathBanner(displayKiller, weaponName);
            }

            // Spawn Kill Feed Item with icon & team identification
            Sprite icon = GetWeaponSprite(weaponName);
            AddKillFeedEntry(killerName, victimName, weaponName, icon, isLocalKiller, isLocalVictim, isKillerFriendly, isVictimFriendly);
        }

        public void AddKillFeedEntry(string killer, string victim, string weapon, bool isLocalKiller, bool isLocalVictim)
        {
            Sprite icon = GetWeaponSprite(weapon);
            AddKillFeedEntry(killer, victim, weapon, icon, isLocalKiller, isLocalVictim, false, false);
        }

        public void AddKillFeedEntry(string killer, string victim, string weapon, Sprite weaponIcon, bool isLocalKiller, bool isLocalVictim, bool isKillerFriendly = false, bool isVictimFriendly = false)
        {
            if (killFeedContainer == null) return;

            // Enforce max active entries
            if (killFeedContainer.childCount >= 6)
            {
                var oldest = killFeedContainer.GetChild(0);
                if (oldest != null) Destroy(oldest.gameObject);
            }

            GameObject entryObj = null;
            if (killFeedEntryPrefab != null)
            {
                entryObj = Instantiate(killFeedEntryPrefab, killFeedContainer);
            }
            else
            {
                entryObj = new GameObject("KillEntry");
                entryObj.transform.SetParent(killFeedContainer, false);
                var txt = entryObj.AddComponent<TextMeshProUGUI>();
                txt.fontSize = 15;
                txt.color = Color.white;
            }

            var kfItem = entryObj.GetComponent<KillFeedItem>();
            if (kfItem != null)
            {
                kfItem.Initialize(killer, victim, weapon, weaponIcon, isLocalKiller, isLocalVictim, isKillerFriendly, isVictimFriendly);
            }
            else
            {
                var label = entryObj.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    string wpnStr = !string.IsNullOrEmpty(weapon) ? $" [{weapon}] " : " ➔ ";
                    label.text = $"<color=#00F0FF>{killer}</color>{wpnStr}<color=#FF4D4D>{victim}</color>";
                }
                StartCoroutine(FadeAndDestroyKillEntry(entryObj, 4.0f));
            }
        }

        public void ClearKillFeed()
        {
            if (killFeedContainer == null) return;
            for (int i = killFeedContainer.childCount - 1; i >= 0; i--)
            {
                var child = killFeedContainer.GetChild(i);
                if (child != null) Destroy(child.gameObject);
            }
        }

        public void AddSystemFeedEntry(string message)
        {
            if (killFeedContainer == null) return;

            var entryObj = new GameObject("SystemEntry");
            entryObj.transform.SetParent(killFeedContainer, false);
            var txt = entryObj.AddComponent<TextMeshProUGUI>();
            txt.fontSize = 14;
            txt.color = new Color(0.6f, 0.9f, 1f, 0.9f);
            txt.text = $"• {message}";

            StartCoroutine(FadeAndDestroyKillEntry(entryObj, 4.0f));
        }

        private IEnumerator FadeAndDestroyKillEntry(GameObject entry, float lifetime)
        {
            yield return new WaitForSeconds(lifetime);
            if (entry == null) yield break;
            var cg = entry.GetComponent<CanvasGroup>();
            if (cg == null) cg = entry.AddComponent<CanvasGroup>();

            float fadeDuration = 0.5f;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                if (entry == null) yield break;
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                yield return null;
            }

            if (entry != null) Destroy(entry);
        }
    }
}
