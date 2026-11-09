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
    public partial class PlayerNameplate : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private Slider _healthBarSlider;
        [SerializeField] private Image _healthFillImage;
        [SerializeField] private Image _healthBgImage;

        private PlayerNameDisplay _nameDisplay;
        private PlayerHealth _playerHealth;
        private NetworkObject _networkObject;
        private Transform _camTransform;

        private int _lastRelationship = -999; // -1: FFA (Opponent), 0: Teammate (Blue), 1: Opponent (Red)
        private bool _isLocalPlayer = false;
        private bool _isInvisibleHidden = false;
        private bool _isDeadHidden = false;

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
                _playerHealth.IsInvincibleNet.OnValueChanged += OnInvincibleNetChanged;
                UpdateHealthBar(_playerHealth.CurrentHp.Value, _playerHealth.MaxHp);

                if (_playerHealth.IsDead || _playerHealth.CurrentHp.Value <= 0f)
                {
                    SetDeathHidden(true);
                }
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
                _playerHealth.IsInvincibleNet.OnValueChanged -= OnInvincibleNetChanged;
            }
        }

        private void OnInvincibleNetChanged(bool previous, bool current)
        {
            FixedString64Bytes name = _nameDisplay != null ? _nameDisplay.DisplayName.Value : "";
            UpdateNameplateText(name);
            if (current && _healthFillImage != null)
            {
                _healthFillImage.color = new Color(1f, 0.85f, 0.2f, 1f); // Golden protected bar
            }
            else
            {
                UpdateTeamAndColors(force: true);
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

            if (newHp <= 0f)
            {
                SetDeathHidden(true);
            }
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
                if (_playerHealth != null && _playerHealth.IsInvincible)
                {
                    _nameText.text = $"{displayName} <color=#FFD700>[PROTECTED]</color>";
                }
                else
                {
                    _nameText.text = displayName;
                }
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

            bool isDead = (_playerHealth != null && _playerHealth.IsDead) || _isDeadHidden;
            if (isDead || _isInvisibleHidden)
            {
                if (_canvas != null && _canvas.enabled) _canvas.enabled = false;
                return;
            }

            if (_canvas != null && !_canvas.enabled)
            {
                _canvas.enabled = true;
            }

            UpdateTeamAndColors(force: false);
        }
    }
}
