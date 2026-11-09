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
    public partial class HUDController : MonoBehaviour
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

        [Header("Active Power-Up Timers & Cooldowns")]
        [SerializeField] private GameObject powerUpRoot;
        [SerializeField] private GameObject shieldBarRoot;
        [SerializeField] private Image shieldFill;
        [SerializeField] private TextMeshProUGUI shieldText;
        [SerializeField] private CanvasGroup shieldCanvasGroup;
        [SerializeField] private GameObject invisibilityBarRoot;
        [SerializeField] private Image invisibilityFill;
        [SerializeField] private TextMeshProUGUI invisibilityText;
        [SerializeField] private CanvasGroup invisibilityCanvasGroup;
        [SerializeField] private GameObject rollBarRoot;
        [SerializeField] private Image rollFill;
        [SerializeField] private TextMeshProUGUI rollText;
        [SerializeField] private CanvasGroup rollCanvasGroup;

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
        private CharacterMotor _localMotor;

        // Multi-kill tracking
        private int _multiKillCount = 0;
        private float _multiKillTimer = 0f;
        private const float MultiKillWindow = 4.5f;

        // Dirty-checking cache fields for thermal / potato optimization
        private int _lastRemainingSeconds = -999;
        private int _lastBlueScore = -999;
        private int _lastRedScore = -999;
        private int _lastFFAKills = -999;
        private int _lastFFARank = -999;
        private NetworkObject _cachedLocalPlayerObj;
        private AudioListener _cachedAudioListener;
        private GameModeBase _cachedGameMode;
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

    }
}
