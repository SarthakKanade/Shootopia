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
    /// animated kill feed notifications, match countdown timer, hit markers, and damage flash.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Match Header & Timer")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Transform scoreContainer;
        [SerializeField] private TextMeshProUGUI placementText;

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

        [Header("Kill Feed & System Feed")]
        [SerializeField] private Transform killFeedContainer;
        [SerializeField] private GameObject killFeedEntryPrefab;

        [Header("Combat Feel & Juice")]
        [SerializeField] private CanvasGroup damageFlashGroup;
        [SerializeField] private GameObject hitMarkerRoot;
        [SerializeField] private Image[] hitMarkerTicks;
        [SerializeField] private GameObject killConfirmationRoot;
        [SerializeField] private CanvasGroup killConfirmationGroup;
        [SerializeField] private TextMeshProUGUI killConfirmationText;

        [Header("Combat Audio")]
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private AudioClip killSound;

        private AudioSource _audioSource;
        private Coroutine _damageFlashRoutine;
        private Coroutine _hitMarkerRoutine;
        private Coroutine _killConfirmationRoutine;

        // Player caching
        private PlayerHealth _localHealth;
        private WeaponController _localWeaponController;

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
        }

        private void OnDisable()
        {
            GameModeBase.OnKillFeedReceived -= OnKillFeedEvent;
            GameModeBase.OnSystemMessageReceived -= AddSystemFeedEntry;
            PlayerHealth.OnLocalPlayerHitConfirmed -= OnPlayerHitConfirmed;

            CleanUpLocalPlayerReferences();

            if (_damageFlashRoutine != null) { StopCoroutine(_damageFlashRoutine); _damageFlashRoutine = null; }
            if (_hitMarkerRoutine != null) { StopCoroutine(_hitMarkerRoutine); _hitMarkerRoutine = null; }
            if (_killConfirmationRoutine != null) { StopCoroutine(_killConfirmationRoutine); _killConfirmationRoutine = null; }
        }

        private void Update()
        {
            UpdateLocalPlayerReferences();
            UpdateTimerAndScore();
            UpdateWeaponUI();
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

            if (activeMode != null && activeMode.IsInProgress)
            {
                float timeRemaining = activeMode.TimeRemaining;
                int minutes = Mathf.FloorToInt(timeRemaining / 60f);
                int seconds = Mathf.FloorToInt(timeRemaining % 60f);

                if (timerText != null)
                {
                    timerText.text = $"{minutes:00}:{seconds:00}";
                    timerText.color = (timeRemaining <= 10f && timeRemaining > 0f && Mathf.FloorToInt(timeRemaining * 2f) % 2 == 0)
                        ? new Color(1f, 0.3f, 0.3f)
                        : Color.white;
                }
            }
            else
            {
                if (timerText != null)
                {
                    timerText.text = "00:00";
                    timerText.color = Color.white;
                }
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

        public void ShowKillConfirmation(string victimName)
        {
            if (killSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(killSound, 1.0f);
            }

            if (killConfirmationRoot == null) return;
            if (_killConfirmationRoutine != null) StopCoroutine(_killConfirmationRoutine);
            _killConfirmationRoutine = StartCoroutine(KillConfirmationRoutine(victimName));
        }

        private IEnumerator KillConfirmationRoutine(string victimName)
        {
            if (killConfirmationText != null)
            {
                killConfirmationText.text = $"+1 ELIMINATED: {victimName.ToUpper()}";
            }

            killConfirmationRoot.SetActive(true);
            UIJuice.PunchScale(killConfirmationRoot.GetComponent<RectTransform>(), 0.25f);

            yield return new WaitForSeconds(2.0f);

            if (killConfirmationGroup != null)
            {
                float duration = 0.3f;
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    killConfirmationGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                    yield return null;
                }
            }

            killConfirmationRoot.SetActive(false);
            if (killConfirmationGroup != null) killConfirmationGroup.alpha = 1f;
        }

        // ──────────────────────── KILL FEED ────────────────────────

        private void OnKillFeedEvent(string killerName, string victimName, string weaponName, ulong killerId, ulong victimId)
        {
            AddKillFeedEntry(killerName, victimName, weaponName);
        }

        public void AddKillFeedEntry(string killer, string victim, string weapon = "")
        {
            if (killFeedContainer == null) return;

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
                txt.fontSize = 16;
                txt.color = Color.white;
            }

            var label = entryObj.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                string wpnStr = !string.IsNullOrEmpty(weapon) ? $" [{weapon}] " : " ➔ ";
                label.text = $"<color=#FFE066>{killer}</color>{wpnStr}<color=#FF6666>{victim}</color>";
            }

            StartCoroutine(FadeAndDestroyKillEntry(entryObj, 4.0f));

            // Check if local player was the killer
            string localName = PlayerProfileManager.Instance != null ? PlayerProfileManager.Instance.DisplayName : "";
            if (!string.IsNullOrEmpty(localName) && killer.Equals(localName, System.StringComparison.OrdinalIgnoreCase))
            {
                ShowKillConfirmation(victim);
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
            var cg = entry.GetComponent<CanvasGroup>();
            if (cg == null) cg = entry.AddComponent<CanvasGroup>();

            float fadeDuration = 0.5f;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                yield return null;
            }

            Destroy(entry);
        }
    }
}
