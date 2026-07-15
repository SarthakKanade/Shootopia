using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Netcode;

namespace Shootopia
{
    /// <summary>
    /// Controls the gameplay HUD using UI Toolkit.
    /// Manages player health bar, equipped weapon/ammo display, kill feed notifications, match countdown timer, and scoreboard.
    /// Prefixes all logs with [HUDController].
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("UI Reference")]
        [SerializeField] private UIDocument uiDocument;

        [Header("Combat Feel / Juice")]
        [SerializeField] private AudioClip _hitSound;
        [SerializeField] private AudioClip _killSound;

        private VisualElement _root;
        
        private Label _timerText;
        private VisualElement _scoreContainer;
        private VisualElement _killFeedContainer;
        private Label _placementText;
        
        private VisualElement _healthFill;
        private Label _healthText;
        
        private Label _weaponCategory;
        private Label _weaponName;
        private Label _ammoText;
        private VisualElement _cooldownBar;
        private VisualElement _cooldownFill;

        private VisualElement _damageFlash;
        private VisualElement _hitMarker;
        private readonly List<VisualElement> _hitMarkerTicks = new List<VisualElement>();
        private Label _killConfirmation;
        private AudioSource _audioSource;

        private Coroutine _damageFlashRoutine;
        private Coroutine _hitMarkerRoutine;
        private Coroutine _killConfirmationRoutine;

        // Player caching
        private PlayerHealth _localHealth;
        private WeaponController _localWeaponController;

        private void OnEnable()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

            if (uiDocument == null)
            {
                Debug.LogError("[HUDController] No UIDocument found!");
                return;
            }

            _root = uiDocument.rootVisualElement;
            if (_root == null) return;

            // Bind UI Elements
            _timerText = _root.Q<Label>("TimerText");
            _scoreContainer = _root.Q<VisualElement>("ScoreContainer");
            _killFeedContainer = _root.Q<VisualElement>("KillFeed");
            _placementText = _root.Q<Label>("PlacementText");
            
            _healthFill = _root.Q<VisualElement>("HealthFill");
            _healthText = _root.Q<Label>("HealthText");
            
            _weaponCategory = _root.Q<Label>("WeaponCategory");
            _weaponName = _root.Q<Label>("WeaponName");
            _ammoText = _root.Q<Label>("AmmoText");
            _cooldownBar = _root.Q<VisualElement>("CooldownBar");
            _cooldownFill = _root.Q<VisualElement>("CooldownFill");

            // Bind Combat Feel Elements
            _damageFlash = _root.Q<VisualElement>("DamageFlash");
            _hitMarker = _root.Q<VisualElement>("HitMarker");
            _killConfirmation = _root.Q<Label>("KillConfirmation");

            _hitMarkerTicks.Clear();
            if (_hitMarker != null)
            {
                _hitMarkerTicks.Add(_root.Q<VisualElement>("HitMarkerT"));
                _hitMarkerTicks.Add(_root.Q<VisualElement>("HitMarkerB"));
                _hitMarkerTicks.Add(_root.Q<VisualElement>("HitMarkerL"));
                _hitMarkerTicks.Add(_root.Q<VisualElement>("HitMarkerR"));
            }

            // Setup local audio source for 2D feedback sound
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
            _audioSource.spatialBlend = 0f; // Force 2D sound

            // Subscribe to static event for kill feeds
            GameModeBase.OnKillFeedReceived += AddKillFeedEntry;
            PlayerHealth.OnLocalPlayerHitConfirmed += ShowHitMarker;
            
            Debug.Log("[HUDController] Initialized successfully.");
        }

        private void OnDisable()
        {
            GameModeBase.OnKillFeedReceived -= AddKillFeedEntry;
            PlayerHealth.OnLocalPlayerHitConfirmed -= ShowHitMarker;
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
            float percentage = Mathf.Clamp01(newValue / maxHp) * 100f;

            if (_healthFill != null)
            {
                _healthFill.style.width = new Length(percentage, LengthUnit.Percent);
            }
            if (_healthText != null)
            {
                _healthText.text = $"{Mathf.RoundToInt(newValue)} / {Mathf.RoundToInt(maxHp)}";
            }

            // Trigger screen flash when taking damage
            if (newValue < previousValue && previousValue > 0f)
            {
                float damageTaken = previousValue - newValue;
                TriggerDamageFlash(damageTaken);
            }
        }

        private void UpdateTimerAndScore()
        {
            // Update match countdown timer
            if (GameModeBase.Instance != null && GameModeBase.Instance.IsInProgress)
            {
                float timeRemaining = GameModeBase.Instance.TimeRemaining;
                int minutes = Mathf.FloorToInt(timeRemaining / 60f);
                int seconds = Mathf.FloorToInt(timeRemaining % 60f);

                if (_timerText != null)
                {
                    _timerText.text = $"{minutes:00}:{seconds:00}";

                    // Simple low time warning pulsing
                    if (timeRemaining <= 10f && timeRemaining > 0f)
                    {
                        _timerText.style.color = (Mathf.FloorToInt(timeRemaining * 2f) % 2 == 0) ? Color.red : Color.white;
                    }
                    else
                    {
                        _timerText.style.color = Color.white;
                    }
                }
            }
            else
            {
                if (_timerText != null)
                {
                    _timerText.text = "00:00";
                    _timerText.style.color = Color.white;
                }
            }

            // Update mode-agnostic score list
            if (_scoreContainer != null)
            {
                _scoreContainer.Clear();
                if (GameModeBase.Instance is IScoreProvider scoreProvider)
                {
                    var scoreData = scoreProvider.GetScoreDisplayData();
                    if (scoreData.Entries != null)
                    {
                        for (int i = 0; i < scoreData.Entries.Count; i++)
                        {
                            var entry = scoreData.Entries[i];

                            var entryLabel = new Label($"{entry.Label}: {entry.Value}");
                            entryLabel.style.fontSize = 13f;
                            entryLabel.style.color = Color.white;
                            entryLabel.style.marginRight = 12f;
                            entryLabel.style.marginLeft = 12f;
                            entryLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

                            // Dynamic coloring based on label
                            if (entry.Label.Equals("You", System.StringComparison.OrdinalIgnoreCase))
                            {
                                entryLabel.style.color = new Color(0.18f, 0.8f, 0.44f); // light green
                            }
                            else if (entry.Label.StartsWith("Leader", System.StringComparison.OrdinalIgnoreCase))
                            {
                                entryLabel.style.color = new Color(0.95f, 0.77f, 0.06f); // golden yellow
                            }
                            else if (entry.Label.Equals("Blue", System.StringComparison.OrdinalIgnoreCase))
                            {
                                entryLabel.style.color = new Color(0.2f, 0.6f, 1f); // Blue
                            }
                            else if (entry.Label.Equals("Red", System.StringComparison.OrdinalIgnoreCase))
                            {
                                entryLabel.style.color = new Color(1f, 0.3f, 0.3f); // Red
                            }

                            _scoreContainer.Add(entryLabel);

                            if (i < scoreData.Entries.Count - 1)
                            {
                                var divider = new Label("|");
                                divider.style.fontSize = 13f;
                                divider.style.color = new Color(0.4f, 0.4f, 0.4f);
                                _scoreContainer.Add(divider);
                            }
                        }
                    }
                }
            }

            // Update FFA Placement Indicator
            if (GameModeBase.Instance is FFAMode ffa)
            {
                if (NetworkManager.Singleton != null)
                {
                    ulong localId = NetworkManager.Singleton.LocalClientId;
                    int rank = ffa.GetPlayerRank(localId);
                    string suffix = "th";
                    if (rank == 1) suffix = "st";
                    else if (rank == 2) suffix = "nd";
                    else if (rank == 3) suffix = "rd";

                    if (_placementText != null)
                    {
                        _placementText.text = $"YOU: {rank}{suffix} PLACE";
                        _placementText.style.display = DisplayStyle.Flex;
                    }
                }
            }
            else
            {
                if (_placementText != null)
                {
                    _placementText.style.display = DisplayStyle.None;
                }
            }
        }

        private void UpdateWeaponUI()
        {
            if (_localWeaponController == null || !_localWeaponController.IsArmed || _localWeaponController.CurrentAttach == null)
            {
                if (_weaponName != null) _weaponName.text = "FISTS";
                if (_weaponCategory != null) _weaponCategory.text = "DEFAULT";
                if (_ammoText != null) _ammoText.text = "∞";
                if (_cooldownBar != null) _cooldownBar.style.display = DisplayStyle.None;
                return;
            }

            var attach = _localWeaponController.CurrentAttach;
            var data = attach.gameplayData;
            if (data == null) return;

            if (_weaponName != null) _weaponName.text = data.weaponName.ToUpper();
            if (_weaponCategory != null) _weaponCategory.text = data.category.ToString().ToUpper();

            bool isProjectile = data.projectilePrefab != null;

            if (_cooldownBar != null && _cooldownFill != null)
            {
                if (isProjectile)
                {
                    if (data.durabilityType == DurabilityType.Ammo)
                    {
                        if (_ammoText != null)
                        {
                            _ammoText.text = $"{_localWeaponController.CurrentAmmo} / {data.MagazineSize}";
                        }
                        _cooldownBar.style.display = DisplayStyle.None;
                    }
                    else if (data.durabilityType == DurabilityType.Uses)
                    {
                        if (_ammoText != null)
                        {
                            _ammoText.text = $"{_localWeaponController.CurrentUses} / {data.maxUses}";
                        }
                        _cooldownBar.style.display = DisplayStyle.None;
                    }
                    else if (data.durabilityType == DurabilityType.TimeBased)
                    {
                        float remaining = _localWeaponController.TimeRemaining;
                        if (_ammoText != null)
                        {
                            _ammoText.text = $"{remaining:F1}s";
                        }
                        _cooldownBar.style.display = DisplayStyle.Flex;
                        float pct = Mathf.Clamp01(remaining / data.weaponDuration) * 100f;
                        _cooldownFill.style.width = new Length(pct, LengthUnit.Percent);
                    }
                    else
                    {
                        // Fallback cooldown / ready status based on fire rate
                        _cooldownBar.style.display = DisplayStyle.Flex;
                        float pct = Mathf.Clamp01(_localWeaponController.TimeSinceLastShot / data.FireRate) * 100f;
                        _cooldownFill.style.width = new Length(pct, LengthUnit.Percent);
                        if (_ammoText != null)
                        {
                            _ammoText.text = pct >= 100f ? "READY" : "COOLDOWN";
                        }
                    }
                }
                else
                {
                    // Hitscan weapons
                    _cooldownBar.style.display = DisplayStyle.None;

                    if (data.durabilityType == DurabilityType.Ammo)
                    {
                        if (_ammoText != null)
                        {
                            _ammoText.text = $"{_localWeaponController.CurrentAmmo} / {data.MagazineSize}";
                        }
                    }
                    else if (data.durabilityType == DurabilityType.Uses)
                    {
                        if (_ammoText != null)
                        {
                            _ammoText.text = $"{_localWeaponController.CurrentUses} / {data.maxUses}";
                        }
                    }
                    else if (data.durabilityType == DurabilityType.TimeBased)
                    {
                        float remaining = _localWeaponController.TimeRemaining;
                        if (_ammoText != null)
                        {
                            _ammoText.text = $"{remaining:F1}s";
                        }
                        _cooldownBar.style.display = DisplayStyle.Flex;
                        float pct = Mathf.Clamp01(remaining / data.weaponDuration) * 100f;
                        _cooldownFill.style.width = new Length(pct, LengthUnit.Percent);
                    }
                }
            }
        }

        private void AddKillFeedEntry(string killerName, string victimName, string weaponName, ulong killerClientId, ulong victimClientId)
        {
            if (_killFeedContainer == null) return;

            var entry = new VisualElement();
            entry.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.85f);
            entry.style.borderTopLeftRadius = 4f;
            entry.style.borderTopRightRadius = 4f;
            entry.style.borderBottomLeftRadius = 4f;
            entry.style.borderBottomRightRadius = 4f;
            entry.style.borderLeftWidth = 3f;
            entry.style.borderLeftColor = new Color(0.9f, 0.3f, 0.23f);
            entry.style.paddingTop = 4f;
            entry.style.paddingBottom = 4f;
            entry.style.paddingLeft = 10f;
            entry.style.paddingRight = 10f;
            entry.style.marginBottom = 6f;
            entry.style.flexDirection = FlexDirection.Row;

            var textLabel = new Label($"{killerName} killed {victimName} with {weaponName}");
            textLabel.style.color = Color.white;
            textLabel.style.fontSize = 12f;
            entry.Add(textLabel);

            _killFeedContainer.Add(entry);

            // Cap count
            while (_killFeedContainer.childCount > 5)
            {
                _killFeedContainer.RemoveAt(0);
            }

            // Life timer
            StartCoroutine(FadeOutAndRemoveEntry(entry));

            // Check if local player is the killer
            if (NetworkManager.Singleton != null && killerClientId == NetworkManager.Singleton.LocalClientId && killerClientId != victimClientId)
            {
                ShowKillConfirmation(victimName);
            }
        }

        private IEnumerator FadeOutAndRemoveEntry(VisualElement entry)
        {
            yield return new WaitForSeconds(3.5f);

            float duration = 0.5f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (entry == null || entry.parent == null) yield break;
                elapsed += Time.deltaTime;
                entry.style.opacity = Mathf.Lerp(1f, 0f, elapsed / duration);
                yield return null;
            }

            if (entry != null && entry.parent != null)
            {
                _killFeedContainer.Remove(entry);
            }
        }

        // --- Combat Feel Animations [CombatFeedback] ---

        private void TriggerDamageFlash(float damage)
        {
            if (_damageFlash == null) return;
            if (_damageFlashRoutine != null) StopCoroutine(_damageFlashRoutine);
            _damageFlashRoutine = StartCoroutine(DamageFlashSequence(damage));
        }

        private IEnumerator DamageFlashSequence(float damage)
        {
            float maxHp = _localHealth != null ? _localHealth.GetMaxHp() : 100f;
            float ratio = Mathf.Clamp01(damage / maxHp);
            
            // Intensity proportional to damage taken (min 0.3, max 0.8)
            float startAlpha = Mathf.Lerp(0.3f, 0.8f, ratio);
            float duration = Mathf.Lerp(0.2f, 0.5f, ratio);
            
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
                Color c = new Color(0.9f, 0.3f, 0.23f, alpha);
                _damageFlash.style.borderLeftColor = c;
                _damageFlash.style.borderRightColor = c;
                _damageFlash.style.borderTopColor = c;
                _damageFlash.style.borderBottomColor = c;
                _damageFlash.style.backgroundColor = new Color(0.9f, 0.3f, 0.23f, alpha * 0.25f);
                yield return null;
            }
            
            Color transparent = new Color(0.9f, 0.3f, 0.23f, 0f);
            _damageFlash.style.borderLeftColor = transparent;
            _damageFlash.style.borderRightColor = transparent;
            _damageFlash.style.borderTopColor = transparent;
            _damageFlash.style.borderBottomColor = transparent;
            _damageFlash.style.backgroundColor = transparent;
        }

        private void ShowHitMarker(bool isKill)
        {
            if (_hitMarker == null) return;
            if (_hitMarkerRoutine != null) StopCoroutine(_hitMarkerRoutine);
            _hitMarkerRoutine = StartCoroutine(HitMarkerSequence(isKill));
        }

        private IEnumerator HitMarkerSequence(bool isKill)
        {
            Color tickColor = isKill ? new Color(0.92f, 0.15f, 0.15f) : Color.white;
            foreach (var tick in _hitMarkerTicks)
            {
                if (tick != null) tick.style.backgroundColor = tickColor;
            }

            // Play hit/kill sound
            AudioClip sound = isKill ? _killSound : _hitSound;
            if (_audioSource != null && sound != null)
            {
                _audioSource.PlayOneShot(sound);
            }

            float duration = 0.2f;
            float elapsed = 0f;
            _hitMarker.style.opacity = 1f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                _hitMarker.style.opacity = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }

            _hitMarker.style.opacity = 0f;
        }

        private void ShowKillConfirmation(string victimName)
        {
            if (_killConfirmation == null) return;
            if (_killConfirmationRoutine != null) StopCoroutine(_killConfirmationRoutine);
            _killConfirmationRoutine = StartCoroutine(KillConfirmationSequence(victimName));
        }

        private IEnumerator KillConfirmationSequence(string victimName)
        {
            _killConfirmation.text = $"+1 Kill: {victimName.ToUpper()}";
            _killConfirmation.style.opacity = 1f;
            
            float duration = 1.5f;
            float elapsed = 0f;
            
            // Reset position & size
            _killConfirmation.style.top = new Length(60, LengthUnit.Percent);
            _killConfirmation.style.fontSize = 26f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Drift up
                float topPct = Mathf.Lerp(60f, 54f, t);
                _killConfirmation.style.top = new Length(topPct, LengthUnit.Percent);
                
                // Font size shrink slightly
                _killConfirmation.style.fontSize = Mathf.Lerp(26f, 22f, t);

                if (t > 0.6f)
                {
                    float fadeT = (t - 0.6f) / 0.4f;
                    _killConfirmation.style.opacity = Mathf.Lerp(1f, 0f, fadeT);
                }

                yield return null;
            }

            _killConfirmation.style.opacity = 0f;
        }
    }
}
