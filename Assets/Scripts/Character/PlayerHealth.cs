using System;
using System.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Shootopia
{
    /// <summary>
    /// Server-authoritative networked player health manager.
    /// Manages HP, lives, damage validation, death sequence, and respawning.
    /// </summary>
    public partial class PlayerHealth : NetworkBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHp = 100f;
        [SerializeField] private int totalLives = 3;
        [SerializeField] private FloatGameEvent onHpChanged;
        [SerializeField] private GameEvent onPlayerDied;

        [Header("Combat Feel / Juice")]
        [FormerlySerializedAs("_playerAudioConfig")]
        [SerializeField] private PlayerAudioSO playerAudioConfig;
        [SerializeField] private GameObject spawnVFXPrefab;
        [FormerlySerializedAs("_deathSound")]
        [SerializeField] private AudioClip deathSound;
        [FormerlySerializedAs("_spawnSound")]
        [SerializeField] private AudioClip spawnSound;

        // Network State
        private readonly NetworkVariable<float> _currentHp = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _livesRemaining = new NetworkVariable<int>(3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _deathCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<float> CurrentHp => _currentHp;
        public NetworkVariable<int> DeathCount => _deathCount;
        public float MaxHp => maxHp;
        public ulong LastAttackerClientId { get; set; }

        public static event Action<bool> OnLocalPlayerHitConfirmed;
        public static event Action<Vector3, float> OnLocalPlayerDamagedWithPosition;
        public static event Action<float> OnLocalPlayerDied;
        public static event Action OnLocalPlayerRespawned;

        [Header("Spawn Protection")]
        [SerializeField] private float spawnInvincibilityDuration = 5.0f;

        private readonly NetworkVariable<bool> _isInvincibleNet = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
        private bool _isDead = false;
        private bool _localInvincible = false;
        private Coroutine _serverInvincibilityRoutine;
        private Coroutine _localInvincibilityRoutine;
        private Coroutine _invincibilityFlashRoutine;
        private Coroutine _hideModelRoutine;
        private GameObject _invincibilityAuraInstance;
        private Material _invincibilitySkinMaterial;
        private PlayerStatusEffects _statusEffects;

        public bool IsDead => _isDead;
        public bool IsInvincible => IsSpawned ? _isInvincibleNet.Value : _localInvincible;
        public NetworkVariable<bool> IsInvincibleNet => _isInvincibleNet;
        public bool IsFrozen => _statusEffects != null && _statusEffects.IsFrozen;
        public bool IsBurning => _statusEffects != null && _statusEffects.IsBurning;
        public float SpawnInvincibilityDuration => spawnInvincibilityDuration;

        public static readonly System.Collections.Generic.List<PlayerHealth> ActivePlayers = new System.Collections.Generic.List<PlayerHealth>();

        private void OnEnable()
        {
            if (!ActivePlayers.Contains(this))
            {
                ActivePlayers.Add(this);
            }
        }

        private void OnDisable()
        {
            ActivePlayers.Remove(this);
        }

        private void Awake()
        {
            _statusEffects = GetComponent<PlayerStatusEffects>() ?? gameObject.AddComponent<PlayerStatusEffects>();
            if (playerAudioConfig == null)
            {
                var motor = GetComponent<CharacterMotor>();
                if (motor != null && motor.AudioConfig != null) playerAudioConfig = motor.AudioConfig;
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _currentHp.OnValueChanged += OnHpValueChanged;
            _isInvincibleNet.OnValueChanged += OnInvincibleNetChanged;
            SetPlayerVisibility(true);

            if (_isInvincibleNet.Value)
            {
                ShowInvincibilityAura(true);
            }

            if (IsServer)
            {
                _currentHp.Value = maxHp;
                _livesRemaining.Value = totalLives;
                _deathCount.Value = 0;

                // Spawn positioning check
                if (transform.position.y <= 1.0f && SpawnPointManager.Instance != null)
                {
                    SpawnPointType spawnType = GetSpawnTypeForPlayer();
                    Transform sp = SpawnPointManager.Instance.GetSpawnPoint(spawnType);
                    if (sp != null) Teleport(sp.position);
                }

                bool isMatchActive = (GameModeBase.Instance != null && (GameModeBase.Instance.MatchActive.Value || GameModeBase.Instance.IsInProgress))
                                     || (NetworkedGameManager.Instance != null && 
                                         (NetworkedGameManager.Instance.MatchStarted.Value || 
                                          (NetworkedGameManager.Instance.ActiveGameMode != null && (NetworkedGameManager.Instance.ActiveGameMode.MatchActive.Value || NetworkedGameManager.Instance.ActiveGameMode.IsInProgress))));
                if (isMatchActive)
                {
                    StartInvincibilityServer(spawnInvincibilityDuration);
                }
            }

            onHpChanged?.Raise(_currentHp.Value);
        }

        private void Start()
        {
            // Pure offline / non-networked initial spawn invincibility (5 seconds)
            if (!IsSpawned || (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening))
            {
                StartInvincibilityLocal(spawnInvincibilityDuration);
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            _currentHp.OnValueChanged -= OnHpValueChanged;
            _isInvincibleNet.OnValueChanged -= OnInvincibleNetChanged;
            if (_serverInvincibilityRoutine != null) { StopCoroutine(_serverInvincibilityRoutine); _serverInvincibilityRoutine = null; }
            if (_localInvincibilityRoutine != null) { StopCoroutine(_localInvincibilityRoutine); _localInvincibilityRoutine = null; }
            if (_invincibilityAuraInstance != null)
            {
                Destroy(_invincibilityAuraInstance);
                _invincibilityAuraInstance = null;
            }
            if (_invincibilitySkinMaterial != null)
            {
                Destroy(_invincibilitySkinMaterial);
                _invincibilitySkinMaterial = null;
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (_invincibilityAuraInstance != null)
            {
                Destroy(_invincibilityAuraInstance);
                _invincibilityAuraInstance = null;
            }
            if (_invincibilitySkinMaterial != null)
            {
                Destroy(_invincibilitySkinMaterial);
                _invincibilitySkinMaterial = null;
            }
        }

        private void OnHpValueChanged(float previousValue, float newValue)
        {
            onHpChanged?.Raise(newValue);

            if (newValue > 0f && _isDead)
            {
                _isDead = false;
            }

            if (newValue < previousValue && AudioManager.Instance != null)
            {
                var cfg = playerAudioConfig;
                if (cfg == null)
                {
                    var motor = GetComponent<CharacterMotor>();
                    if (motor != null) cfg = motor.AudioConfig;
                }
                AudioClip hitClip = cfg != null ? cfg.hitTakenClip : null;
                if (hitClip != null)
                {
                    bool isLocal = IsOwner || !IsSpawned;
                    if (isLocal)
                    {
                        AudioManager.Instance.PlaySFX2D(hitClip, 1f, isCombatPriority: true);
                    }
                    else
                    {
                        AudioManager.Instance.PlaySFXAtPosition(hitClip, transform.position, 0.8f, isCombatPriority: false);
                    }
                }
            }
        }


    }
}
