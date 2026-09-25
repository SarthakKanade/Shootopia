using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using Unity.Collections;

namespace Shootopia
{
    /// <summary>
    /// Displays player name and an overhead health bar above other players' heads.
    /// Hidden on the local player so names/healthbars don't clutter their own view.
    /// Dynamically tints name and health bar blue (teammate) or red (opponent).
    /// </summary>
    public class PlayerNameplate : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private Canvas _canvas;

        private PlayerNameDisplay _nameDisplay;
        private PlayerHealth _playerHealth;
        private NetworkObject _networkObject;
        private Transform _camTransform;

        // Health bar UI elements constructed dynamically or wired
        private Slider _healthBarSlider;
        private Image _healthFillImage;
        private Image _healthBgImage;

        private int _lastRelationship = -999; // -1: FFA (Opponent), 0: Teammate (Blue), 1: Opponent (Red)
        private bool _isLocalPlayer = false;

        private readonly Color BlueTeammateColor = new Color(0.2f, 0.65f, 1f, 1f);
        private readonly Color RedOpponentColor = new Color(1f, 0.25f, 0.25f, 1f);
        private readonly Color HealthBgColor = new Color(0.1f, 0.1f, 0.15f, 0.75f);

        private void Start()
        {
            _networkObject = GetComponentInParent<NetworkObject>();
            _playerHealth = GetComponentInParent<PlayerHealth>();
            _nameDisplay = GetComponentInParent<PlayerNameDisplay>();

            // If this is the local player, hide overhead name and health bar on their own screen
            if (_networkObject != null && _networkObject.IsOwner)
            {
                _isLocalPlayer = true;
                if (_canvas != null)
                {
                    _canvas.enabled = false;
                }
                gameObject.SetActive(false);
                return;
            }

            // Create health bar UI if not present
            BuildHealthBarUI();

            if (_nameDisplay != null)
            {
                _nameDisplay.DisplayName.OnValueChanged += OnNameChanged;
                UpdateNameplateText(_nameDisplay.DisplayName.Value);
            }

            if (_playerHealth != null)
            {
                _playerHealth.CurrentHp.OnValueChanged += OnHpChanged;
                UpdateHealthBar(_playerHealth.CurrentHp.Value, _playerHealth.MaxHp);
            }

            var mainCam = Camera.main;
            if (mainCam != null)
            {
                _camTransform = mainCam.transform;
            }

            UpdateTeamAndColors(force: true);
        }

        private void OnDestroy()
        {
            if (_nameDisplay != null)
            {
                _nameDisplay.DisplayName.OnValueChanged -= OnNameChanged;
            }

            if (_playerHealth != null)
            {
                _playerHealth.CurrentHp.OnValueChanged -= OnHpChanged;
            }
        }

        private void OnNameChanged(FixedString64Bytes previousValue, FixedString64Bytes newValue)
        {
            UpdateNameplateText(newValue);
        }

        private void OnHpChanged(float previousHp, float newHp)
        {
            float max = _playerHealth != null ? _playerHealth.MaxHp : 100f;
            UpdateHealthBar(newHp, max);
        }

        private void UpdateNameplateText(FixedString64Bytes name)
        {
            string displayName = name.ToString();
            if (string.IsNullOrEmpty(displayName))
            {
                if (_networkObject != null)
                {
                    displayName = PlayerNameRegistry.GetName(_networkObject.OwnerClientId);
                }
                else
                {
                    displayName = "Player";
                }
            }

            if (_nameText != null)
            {
                _nameText.text = displayName;
            }
        }

        private void UpdateHealthBar(float currentHp, float maxHp)
        {
            if (_healthBarSlider != null)
            {
                maxHp = Mathf.Max(maxHp, 1f);
                _healthBarSlider.value = Mathf.Clamp01(currentHp / maxHp);
            }
        }

        private void BuildHealthBarUI()
        {
            if (_canvas == null) return;

            // Check if already created
            var existingSlider = _canvas.GetComponentInChildren<Slider>(true);
            if (existingSlider != null)
            {
                _healthBarSlider = existingSlider;
                var fill = _healthBarSlider.fillRect != null ? _healthBarSlider.fillRect.GetComponent<Image>() : null;
                if (fill != null) _healthFillImage = fill;
                return;
            }

            // Adjust name text position slightly higher to accommodate health bar below it
            if (_nameText != null)
            {
                var nameRect = _nameText.GetComponent<RectTransform>();
                if (nameRect != null)
                {
                    nameRect.anchoredPosition = new Vector2(0f, 12f);
                    nameRect.sizeDelta = new Vector2(250f, 30f);
                    _nameText.fontSize = 20;
                }
            }

            // Create HealthBar root
            GameObject healthBarGo = new GameObject("OverheadHealthBar", typeof(RectTransform));
            healthBarGo.transform.SetParent(_canvas.transform, false);
            var hbRect = healthBarGo.GetComponent<RectTransform>();
            hbRect.anchorMin = new Vector2(0.5f, 0.5f);
            hbRect.anchorMax = new Vector2(0.5f, 0.5f);
            hbRect.pivot = new Vector2(0.5f, 0.5f);
            hbRect.anchoredPosition = new Vector2(0f, -8f);
            hbRect.sizeDelta = new Vector2(100f, 10f);

            // Background
            GameObject bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(healthBarGo.transform, false);
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            _healthBgImage = bgGo.GetComponent<Image>();
            _healthBgImage.color = HealthBgColor;

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(healthBarGo.transform, false);
            var faRect = fillArea.GetComponent<RectTransform>();
            faRect.anchorMin = Vector2.zero;
            faRect.anchorMax = Vector2.one;
            faRect.sizeDelta = Vector2.zero;

            // Fill
            GameObject fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(fillArea.transform, false);
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            _healthFillImage = fillGo.GetComponent<Image>();
            _healthFillImage.color = RedOpponentColor;

            // Slider component
            _healthBarSlider = healthBarGo.AddComponent<Slider>();
            _healthBarSlider.minValue = 0f;
            _healthBarSlider.maxValue = 1f;
            _healthBarSlider.value = 1f;
            _healthBarSlider.interactable = false;
            _healthBarSlider.targetGraphic = _healthFillImage;
            _healthBarSlider.fillRect = fillRect;

            var transition = _healthBarSlider.colors;
            _healthBarSlider.transition = Selectable.Transition.None;
        }

        private void Update()
        {
            if (_isLocalPlayer) return;

            // Check if local ownership state resolved late
            if (_networkObject != null && _networkObject.IsOwner)
            {
                _isLocalPlayer = true;
                if (_canvas != null) _canvas.enabled = false;
                gameObject.SetActive(false);
                return;
            }

            UpdateTeamAndColors(force: false);
        }

        private void UpdateTeamAndColors(bool force)
        {
            if (_isLocalPlayer) return;

            int relationship = DetermineRelationship();
            if (force || relationship != _lastRelationship)
            {
                _lastRelationship = relationship;

                // 0: Teammate (Blue), 1: Opponent (Red)
                Color themeColor = (relationship == 0) ? BlueTeammateColor : RedOpponentColor;
                SetNameplateColor(themeColor);
            }
        }

        /// <summary>
        /// Returns 0 for teammate, 1 for opponent.
        /// In TDM: compares target's team with local player's team.
        /// In FFA / other modes: all other players are opponents (1).
        /// </summary>
        private int DetermineRelationship()
        {
            if (GameModeBase.Instance is TDMMode tdm && _networkObject != null)
            {
                ulong localClientId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 9999;
                int localTeam = tdm.GetPlayerTeam(localClientId);
                int targetTeam = tdm.GetPlayerTeam(_networkObject.OwnerClientId);

                if (localTeam != -1 && targetTeam != -1)
                {
                    return (localTeam == targetTeam) ? 0 : 1;
                }

                // If local team not yet assigned, fallback to target's raw team (0=blue, 1=red)
                if (targetTeam != -1)
                {
                    return targetTeam;
                }
            }

            // In FFA or unassigned mode, all others are opponents (Red)
            return 1;
        }

        private void LateUpdate()
        {
            if (_isLocalPlayer) return;

            if (_camTransform == null)
            {
                var mainCam = Camera.main;
                if (mainCam != null)
                {
                    _camTransform = mainCam.transform;
                }
            }

            if (_camTransform != null && _canvas != null)
            {
                // Billboard: face camera directly
                _canvas.transform.LookAt(_canvas.transform.position + _camTransform.forward);
            }
        }

        public void SetNameplateColor(Color c)
        {
            if (_nameText != null)
            {
                _nameText.color = c;
            }

            if (_healthFillImage != null)
            {
                _healthFillImage.color = c;
            }
        }
    }
}
