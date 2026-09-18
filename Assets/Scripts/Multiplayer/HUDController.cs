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
        [SerializeField] private AudioClip countdownBeepClip;
        [SerializeField] private AudioClip fightHornClip;

        [Header("Player Health & Velocity")]
        [SerializeField] private Image healthFill;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private Image velocityFill;
        [SerializeField] private TextMeshProUGUI velocityStreakText;

        [Header("Weapon & Ammo Display")]
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

        [Header("Kill Feed Weapon Icons")]
        [SerializeField] private Sprite defaultWeaponSprite;
        [SerializeField] private Sprite pistolSprite;
        [SerializeField] private Sprite rifleSprite;
        [SerializeField] private Sprite sniperSprite;
        [SerializeField] private Sprite flameSprite;
        [SerializeField] private Sprite cryoSprite;
        [SerializeField] private Sprite voidSprite;
        [SerializeField] private Sprite bounceSprite;
        [SerializeField] private Sprite meleeSprite;
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

        private AudioSource _audioSource;
        private Coroutine _damageFlashRoutine;
        private Coroutine _hitMarkerRoutine;
        private Coroutine _eliminationBannerRoutine;
        private Coroutine _deathBannerRoutine;
        private Coroutine _preMatchRoutine;

        // Player caching
        private PlayerHealth _localHealth;
        private WeaponController _localWeaponController;

        // Multi-kill tracking
        private int _multiKillCount = 0;
        private float _multiKillTimer = 0f;
        private const float MultiKillWindow = 4.5f;

        // Countdown tracking
        private int _lastDisplayedCountdown = -999;

        private void Awake()
        {
            Instance = this;
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

            // Subscribe to static combat events
            GameModeBase.OnKillFeedReceived += OnKillFeedEvent;
            GameModeBase.OnSystemMessageReceived += AddSystemFeedEntry;
            PlayerHealth.OnLocalPlayerHitConfirmed += OnPlayerHitConfirmed;

            // Hide overlay banners initially
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
            DeviceProfileManager.OnControlModeChanged -= HandleControlModeChanged;

            if (_mobileControlsInstance != null)
            {
                Destroy(_mobileControlsInstance);
                _mobileControlsInstance = null;
            }

            GameModeBase.OnKillFeedReceived -= OnKillFeedEvent;
            GameModeBase.OnSystemMessageReceived -= AddSystemFeedEntry;
            PlayerHealth.OnLocalPlayerHitConfirmed -= OnPlayerHitConfirmed;

            CleanUpLocalPlayerReferences();

            if (_damageFlashRoutine != null) { StopCoroutine(_damageFlashRoutine); _damageFlashRoutine = null; }
            if (_hitMarkerRoutine != null) { StopCoroutine(_hitMarkerRoutine); _hitMarkerRoutine = null; }
            if (_eliminationBannerRoutine != null) { StopCoroutine(_eliminationBannerRoutine); _eliminationBannerRoutine = null; }
            if (_deathBannerRoutine != null) { StopCoroutine(_deathBannerRoutine); _deathBannerRoutine = null; }
            if (_preMatchRoutine != null) { StopCoroutine(_preMatchRoutine); _preMatchRoutine = null; }

            if (Instance == this) Instance = null;
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
        }

        private void CleanUpLocalPlayerReferences()
        {
            if (_localHealth != null)
            {
                _localHealth.CurrentHp.OnValueChanged -= OnHealthChanged;
                _localHealth = null;
            }
            _localWeaponController = null;
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

            // 2. Handle Match Time Display
            if (activeMode != null && activeMode.IsInProgress)
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
                    bool isLobbyOpen = WaitingRoomController.IsLobbyMenuOpen;
                    timerText.text = isLobbyOpen ? "WARMUP" : "05:00";
                    timerText.color = Color.white;
                }
            }

            // 3. Handle Score & Placement Tracking
            UpdateScoreAndPlacement(activeMode);
        }

        private void UpdateScoreAndPlacement(GameModeBase activeMode)
        {
            if (activeMode == null)
            {
                if (matchLimitText != null) matchLimitText.text = "";
                if (gameModeBadgeText != null) gameModeBadgeText.text = "WARMUP";
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
                const int limit = 15;

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

                if (placementText != null)
                {
                    placementText.text = $"#{rank} <size=12><color=#8A99B5>/ {totalPlayers}</color></size>";
                }

                if (scoreText != null)
                {
                    scoreText.text = $"<color=#00F0FF>{kills}</color> <size=12><color=#8A99B5>/ 10 KILLS</color></size>";
                }

                if (matchLimitText != null) matchLimitText.text = "FIRST TO 10";
                if (gameModeBadgeText != null) gameModeBadgeText.text = "FREE FOR ALL";
            }
            else
            {
                if (tdmScoreRoot != null) tdmScoreRoot.SetActive(false);
                if (ffaScoreRoot != null) ffaScoreRoot.SetActive(false);
                if (gameModeBadgeText != null) gameModeBadgeText.text = "WARMUP";
                if (matchLimitText != null) matchLimitText.text = "";
            }
        }

        private void HandleCountdownTick(int countdown)
        {
            if (preMatchRoot == null || preMatchCountdownText == null) return;

            if (_preMatchRoutine != null) StopCoroutine(_preMatchRoutine);
            _preMatchRoutine = StartCoroutine(CountdownAnimateRoutine(countdown));
        }

        private IEnumerator CountdownAnimateRoutine(int countdown)
        {
            if (countdown > 0)
            {
                preMatchRoot.SetActive(true);
                preMatchCountdownText.text = countdown.ToString();
                preMatchCountdownText.color = countdown == 1 ? new Color(1f, 0.4f, 0.4f) : new Color(0.0f, 0.94f, 1.0f);

                if (countdownBeepClip != null && _audioSource != null)
                {
                    _audioSource.PlayOneShot(countdownBeepClip, 0.8f);
                }

                // Punch scale
                var rt = preMatchCountdownText.GetComponent<RectTransform>();
                if (rt != null) UIJuice.PunchScale(rt, 0.35f);

                yield return new WaitForSeconds(0.9f);
            }
            else if (countdown == 0)
            {
                preMatchRoot.SetActive(true);
                preMatchCountdownText.text = "FIGHT!";
                preMatchCountdownText.color = new Color(1.0f, 0.84f, 0.0f); // Gold

                if (fightHornClip != null && _audioSource != null)
                {
                    _audioSource.PlayOneShot(fightHornClip, 1.0f);
                }

                var rt = preMatchCountdownText.GetComponent<RectTransform>();
                if (rt != null) UIJuice.PunchScale(rt, 0.5f);

                yield return new WaitForSeconds(1.0f);

                if (preMatchGroup != null)
                {
                    float elapsed = 0f;
                    float dur = 0.4f;
                    while (elapsed < dur)
                    {
                        elapsed += Time.deltaTime;
                        preMatchGroup.alpha = 1f - (elapsed / dur);
                        yield return null;
                    }
                }

                preMatchRoot.SetActive(false);
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
                if (weaponCategoryText != null) weaponCategoryText.text = "PISTOL";
                if (weaponNameText != null) weaponNameText.text = "SIDEARM";
                if (ammoText != null) ammoText.text = "∞";
                if (cooldownBarRoot != null) cooldownBarRoot.SetActive(false);
                return;
            }

            var attach = _localWeaponController.CurrentAttach;
            var weaponData = attach.gameplayData;

            if (weaponCategoryText != null)
            {
                weaponCategoryText.text = weaponData != null ? weaponData.category.ToString().ToUpper() : "WEAPON";
            }

            if (weaponNameText != null)
            {
                weaponNameText.text = weaponData != null ? weaponData.weaponName.ToUpper() : attach.name.ToUpper();
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

        private IEnumerator DeathBannerRoutine(string killerName, string weaponName)
        {
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

            yield return new WaitForSeconds(2.5f);

            if (deathBannerGroup != null)
            {
                float duration = 0.35f;
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    deathBannerGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                    yield return null;
                }
            }

            deathBannerRoot.SetActive(false);
            if (deathBannerGroup != null) deathBannerGroup.alpha = 1f;
        }

        // ──────────────────────── KILL FEED ────────────────────────

        public Sprite GetWeaponSprite(string weaponName)
        {
            if (string.IsNullOrEmpty(weaponName)) return skullSprite;
            string lower = weaponName.ToLower();
            if (lower.Contains("flame")) return flameSprite != null ? flameSprite : defaultWeaponSprite;
            if (lower.Contains("freeze") || lower.Contains("cryo")) return cryoSprite != null ? cryoSprite : defaultWeaponSprite;
            if (lower.Contains("void")) return voidSprite != null ? voidSprite : defaultWeaponSprite;
            if (lower.Contains("bounce") || lower.Contains("trick") || lower.Contains("laser")) return bounceSprite != null ? bounceSprite : defaultWeaponSprite;
            if (lower.Contains("sniper")) return sniperSprite != null ? sniperSprite : defaultWeaponSprite;
            if (lower.Contains("knife") || lower.Contains("sword") || lower.Contains("melee") || lower.Contains("axe")) return meleeSprite != null ? meleeSprite : defaultWeaponSprite;
            if (lower.Contains("pistol") || lower.Contains("sidearm")) return pistolSprite != null ? pistolSprite : defaultWeaponSprite;
            if (lower.Contains("machine") || lower.Contains("rifle") || lower.Contains("gun")) return rifleSprite != null ? rifleSprite : defaultWeaponSprite;
            return skullSprite != null ? skullSprite : defaultWeaponSprite;
        }

        private void OnKillFeedEvent(string killerName, string victimName, string weaponName, ulong killerId, ulong victimId)
        {
            ulong localClientId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 9999;
            bool isLocalKiller = (killerId == localClientId);
            bool isLocalVictim = (victimId == localClientId);

            var activeMode = NetworkedGameManager.Instance != null ? NetworkedGameManager.Instance.ActiveGameMode : GameModeBase.Instance;
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
            if (isLocalKiller)
            {
                ShowEliminationBanner(victimName, weaponName);
            }
            else if (isLocalVictim)
            {
                ShowDeathBanner(killerName, weaponName);
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
