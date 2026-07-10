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

        private VisualElement _root;
        
        // UI Elements
        private Label _timerText;
        private VisualElement _scoreContainer;
        private VisualElement _killFeedContainer;
        
        private VisualElement _healthFill;
        private Label _healthText;
        
        private Label _weaponCategory;
        private Label _weaponName;
        private Label _ammoText;
        private VisualElement _cooldownBar;
        private VisualElement _cooldownFill;

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
            
            _healthFill = _root.Q<VisualElement>("HealthFill");
            _healthText = _root.Q<Label>("HealthText");
            
            _weaponCategory = _root.Q<Label>("WeaponCategory");
            _weaponName = _root.Q<Label>("WeaponName");
            _ammoText = _root.Q<Label>("AmmoText");
            _cooldownBar = _root.Q<VisualElement>("CooldownBar");
            _cooldownFill = _root.Q<VisualElement>("CooldownFill");

            // Subscribe to static event for kill feeds
            GameModeBase.OnKillFeedReceived += AddKillFeedEntry;
            
            Debug.Log("[HUDController] Initialized successfully.");
        }

        private void OnDisable()
        {
            GameModeBase.OnKillFeedReceived -= AddKillFeedEntry;
            CleanUpLocalPlayerReferences();
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

        private void AddKillFeedEntry(string killerName, string victimName, string weaponName)
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
    }
}
