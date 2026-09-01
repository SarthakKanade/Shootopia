using System;
using System.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace Shootopia
{
    /// <summary>
    /// Server-authoritative networked player health manager.
    /// Manages HP, lives, damage validation, death sequence, and respawning.
    /// </summary>
    public class PlayerHealth : NetworkBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHp = 100f;
        [SerializeField] private int totalLives = 3;
        [SerializeField] private FloatGameEvent onHpChanged;
        [SerializeField] private GameEvent onPlayerDied;

        [Header("Combat Feel / Juice")]
        [SerializeField] private PlayerAudioSO playerAudioConfig;
        [SerializeField] private GameObject spawnVFXPrefab;
        [SerializeField] private AudioClip deathSound;
        [SerializeField] private AudioClip spawnSound;

        // Network State
        private readonly NetworkVariable<float> _currentHp = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _livesRemaining = new NetworkVariable<int>(3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _deathCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<float> CurrentHp => _currentHp;
        public NetworkVariable<int> DeathCount => _deathCount;
        public ulong LastAttackerClientId { get; set; }

        public static event Action<bool> OnLocalPlayerHitConfirmed;

        private bool _isDead = false;
        private bool _isInvincible = false;
        private Coroutine _invincibilityFlashRoutine;
        private Coroutine _hideModelRoutine;
        private PlayerStatusEffects _statusEffects;

        public bool IsDead => _isDead;
        public bool IsFrozen => _statusEffects != null && _statusEffects.IsFrozen;
        public bool IsBurning => _statusEffects != null && _statusEffects.IsBurning;

        private void Awake()
        {
            _statusEffects = GetComponent<PlayerStatusEffects>() ?? gameObject.AddComponent<PlayerStatusEffects>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _currentHp.OnValueChanged += OnHpValueChanged;
            SetPlayerVisibility(true);

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
            }

            onHpChanged?.Raise(_currentHp.Value);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            _currentHp.OnValueChanged -= OnHpValueChanged;
        }

        private void OnHpValueChanged(float previousValue, float newValue)
        {
            onHpChanged?.Raise(newValue);

            if (newValue > 0f && _isDead)
            {
                _isDead = false;
                SetPlayerVisibility(true);
            }

            if (newValue < previousValue && IsOwner && AudioManager.Instance != null)
            {
                AudioClip hitClip = playerAudioConfig != null ? playerAudioConfig.hitTakenClip : null;
                if (hitClip != null) AudioManager.Instance.PlaySFX2D(hitClip);
            }
        }

        #region Damage & Health Flow

        public void TakeDamage(float amount, DamageType type = DamageType.Projectile, WeaponSO weaponUsed = null)
        {
            string weaponName = weaponUsed != null ? (!string.IsNullOrEmpty(weaponUsed.weaponName) ? weaponUsed.weaponName : weaponUsed.name)
                              : (type == DamageType.Melee ? "Melee" : "Unknown");

            TakeDamageServerRpc(amount, type, weaponName, LastAttackerClientId);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void TakeDamageServerRpc(float amount, DamageType type, string weaponName, ulong attackerClientId)
        {
            if (!IsServer || _isDead || _isInvincible || _currentHp.Value <= 0f) return;

            // Inactive match or waiting room damage gating
            if (SceneManager.GetActiveScene().name == "WaitingRoom") return;
            if (GameModeBase.Instance != null && !GameModeBase.Instance.MatchActive.Value) return;

            // TDM Friendly Fire prevention
            if (GameModeBase.Instance is TDMMode tdm)
            {
                int victimTeam = tdm.GetPlayerTeam(OwnerClientId);
                int attackerTeam = tdm.GetPlayerTeam(attackerClientId);
                if (attackerClientId != OwnerClientId && victimTeam != -1 && attackerTeam != -1 && victimTeam == attackerTeam)
                {
                    return;
                }
            }

            // Shield Absorption
            float damageToApply = amount;
            var shield = GetComponent<PlayerShield>();
            if (shield != null && shield.IsShieldActive)
            {
                damageToApply = shield.AbsorbDamage(damageToApply);
                if (damageToApply <= 0f) return;
            }

            float previousHp = _currentHp.Value;
            _currentHp.Value = Mathf.Max(_currentHp.Value - damageToApply, 0f);
            float actualDamage = previousHp - _currentHp.Value;

            if (actualDamage > 0f)
            {
                bool isKill = _currentHp.Value <= 0f;

                // Notify attacker
                if (attackerClientId != OwnerClientId && NetworkManager.Singleton.ConnectedClients.ContainsKey(attackerClientId))
                {
                    var clientParams = new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams { TargetClientIds = new[] { attackerClientId } }
                    };
                    NotifyHitConfirmedClientRpc(isKill, clientParams);
                }

                if (isKill)
                {
                    _isDead = true;
                    HandleDeath(attackerClientId, type, weaponName);
                }
            }
        }

        private void HandleDeath(ulong killerClientId, DamageType type, string weaponName)
        {
            if (!IsServer) return;

            _isDead = true;
            _deathCount.Value++;
            _livesRemaining.Value = Mathf.Max(_livesRemaining.Value - 1, 0);

            if (killerClientId != OwnerClientId)
            {
                NetworkedGameManager.ReportKill(killerClientId, OwnerClientId, weaponName);
            }

            DeathClientRpc(type);
            StartCoroutine(RespawnSequence());
        }

        private IEnumerator RespawnSequence()
        {
            yield return new WaitForSeconds(3f);

            _isDead = false;
            StartCoroutine(InvincibilityRoutine());

            Vector3 spawnPos = new Vector3(120f, 16f, 2.5f);
            if (SpawnPointManager.Instance != null)
            {
                Transform sp = SpawnPointManager.Instance.GetSpawnPoint(GetSpawnTypeForPlayer());
                if (sp != null) spawnPos = sp.position;
            }

            _currentHp.Value = maxHp;

            // Server-side repositioning
            SetCharacterPosition(spawnPos);
            ResetMovementState();

            RemoveBurnEffect();
            RespawnClientRpc(spawnPos);
        }

        private IEnumerator InvincibilityRoutine()
        {
            _isInvincible = true;
            yield return new WaitForSeconds(1.5f);
            _isInvincible = false;
        }

        [ClientRpc]
        private void NotifyHitConfirmedClientRpc(bool isKill, ClientRpcParams clientRpcParams = default)
        {
            OnLocalPlayerHitConfirmed?.Invoke(isKill);

            if (AudioManager.Instance != null && playerAudioConfig != null && playerAudioConfig.hitMarkerClip != null)
            {
                AudioManager.Instance.PlaySFX2D(playerAudioConfig.hitMarkerClip);
            }
        }

        [ClientRpc]
        private void DeathClientRpc(DamageType type)
        {
            onPlayerDied?.Raise();

            if (IsOwner && AudioManager.Instance != null)
            {
                AudioClip deathClip = (playerAudioConfig != null && playerAudioConfig.deathClip != null) ? playerAudioConfig.deathClip : deathSound;
                if (deathClip != null) AudioManager.Instance.PlaySFX2D(deathClip);
            }

            var anim = GetComponentInChildren<Animator>(true);
            if (anim != null)
            {
                if (anim.layerCount > 1) anim.SetLayerWeight(1, 0f);
                anim.SetTrigger("Die");
            }

            SetPlayerControlsEnabled(false);

            if (_hideModelRoutine != null) StopCoroutine(_hideModelRoutine);
            _hideModelRoutine = StartCoroutine(HideModelAfterDeathRoutine());
        }

        private IEnumerator HideModelAfterDeathRoutine()
        {
            yield return new WaitForSeconds(1.0f);
            SetPlayerVisibility(false);
            _hideModelRoutine = null;
        }

        [ClientRpc]
        private void RespawnClientRpc(Vector3 spawnPosition)
        {
            if (_hideModelRoutine != null)
            {
                StopCoroutine(_hideModelRoutine);
                _hideModelRoutine = null;
            }

            SetCharacterPosition(spawnPosition);
            ResetLocomotionAnimator();

            SetPlayerControlsEnabled(true);
            SetPlayerVisibility(true);
            RemoveBurnEffect();

            if (spawnVFXPrefab != null) Instantiate(spawnVFXPrefab, transform.position, Quaternion.identity);
            if (spawnSound != null) AudioSource.PlayClipAtPoint(spawnSound, transform.position, 1f);

            if (_invincibilityFlashRoutine != null) StopCoroutine(_invincibilityFlashRoutine);
            _invincibilityFlashRoutine = StartCoroutine(InvincibilityFlashRoutine());
        }

        private IEnumerator InvincibilityFlashRoutine()
        {
            float duration = 1.5f;
            float interval = 0.1f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (_isDead) yield break;
                bool visible = (Mathf.FloorToInt(elapsed / interval) % 2) == 0;
                ToggleRenderers(visible);
                yield return new WaitForSeconds(interval);
                elapsed += interval;
            }

            if (!_isDead) ToggleRenderers(true);
            _invincibilityFlashRoutine = null;
        }

        #endregion

        #region Healing & State Manipulation

        public void Heal(float amount) => HealServerRpc(amount);

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void HealServerRpc(float amount)
        {
            if (!IsServer || _currentHp.Value <= 0f) return;
            _currentHp.Value = Mathf.Min(_currentHp.Value + amount, maxHp);
        }

        public float GetCurrentHp() => _currentHp.Value;
        public float GetMaxHp() => maxHp;

        public void Teleport(Vector3 position)
        {
            if (!IsServer) return;
            SetCharacterPosition(position);
            TeleportClientRpc(position);
        }

        [ClientRpc]
        private void TeleportClientRpc(Vector3 position)
        {
            SetCharacterPosition(position);
            ResetMovementState();
            RemoveBurnEffect();
        }

        public void ResetStateForMatch(Vector3 position)
        {
            if (!IsServer) return;
            _isDead = false;
            _currentHp.Value = maxHp;

            SetCharacterPosition(position);
            ResetMovementState();
            RemoveBurnEffect();

            ResetPlayerClientRpc(position);
        }

        [ClientRpc]
        private void ResetPlayerClientRpc(Vector3 position)
        {
            _isDead = false;
            SetCharacterPosition(position);
            SetPlayerControlsEnabled(true);
            SetPlayerVisibility(true);
            ResetMovementState();
            ResetLocomotionAnimator();
            RemoveBurnEffect();
        }

        #endregion

        #region Status Effects Delegation

        public void ApplyFreezeEffect(float duration, float slowPercent, bool isDirectHit)
        {
            if (_statusEffects == null) _statusEffects = GetComponent<PlayerStatusEffects>() ?? gameObject.AddComponent<PlayerStatusEffects>();
            _statusEffects.ApplyFreeze(duration, slowPercent, isDirectHit);
        }

        public void ApplyBurnEffect(float duration = 5.0f, float dps = 5.0f, WeaponSO weaponUsed = null)
        {
            if (_statusEffects == null) _statusEffects = GetComponent<PlayerStatusEffects>() ?? gameObject.AddComponent<PlayerStatusEffects>();
            _statusEffects.ApplyBurn(duration, dps, weaponUsed);
        }

        public void RemoveBurnEffect()
        {
            if (_statusEffects != null) _statusEffects.RemoveBurn();
        }

        #endregion

        #region Helper Methods

        private SpawnPointType GetSpawnTypeForPlayer()
        {
            if (GameModeBase.Instance is TDMMode tdm)
            {
                int team = tdm.GetPlayerTeam(OwnerClientId);
                if (team == 0) return SpawnPointType.TeamBlue;
                if (team == 1) return SpawnPointType.TeamRed;
            }
            return SpawnPointType.FFA;
        }

        private void SetCharacterPosition(Vector3 position)
        {
            var cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            transform.position = position;
            if (cc != null) cc.enabled = true;
        }

        private void ResetMovementState()
        {
            var motor = GetComponent<CharacterMotor>();
            if (motor != null)
            {
                motor.VerticalVelocity = 0f;
                motor.IsInLava = false;
            }

            var inputRouter = GetComponent<PlayerInputRouter>();
            if (inputRouter != null) inputRouter.DisableControls = false;
        }

        private void ResetLocomotionAnimator()
        {
            var anim = GetComponentInChildren<Animator>(true);
            if (anim != null)
            {
                anim.ResetTrigger("Die");
                anim.CrossFade("Locomotion", 0.1f, 0);
                if (anim.layerCount > 1)
                {
                    anim.CrossFade("Empty", 0.1f, 1);
                    anim.SetLayerWeight(1, 1f);
                }
            }
        }

        private void SetPlayerControlsEnabled(bool enabled)
        {
            var motor = GetComponent<CharacterMotor>();
            if (motor != null) motor.enabled = enabled;

            var inputRouter = GetComponent<PlayerInputRouter>();
            if (inputRouter != null) inputRouter.enabled = enabled;
        }

        private void SetPlayerVisibility(bool visible)
        {
            if (!visible && _invincibilityFlashRoutine != null)
            {
                StopCoroutine(_invincibilityFlashRoutine);
                _invincibilityFlashRoutine = null;
            }

            ToggleRenderers(visible);
            SetPlayerControlsEnabled(visible);
        }

        private void ToggleRenderers(bool visible)
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r != null) r.enabled = visible;
            }
        }

        #endregion
    }
}
