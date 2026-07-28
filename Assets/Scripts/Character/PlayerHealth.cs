using System.Collections;
using UnityEngine;
using Unity.Netcode;

namespace Shootopia
{
    /// <summary>
    /// Server-authoritative networked player health manager.
    /// Tracks HP and lives via NetworkVariables, handles death & respawn ClientRpcs,
    /// and raises events for local UI updates.
    /// </summary>
    public class PlayerHealth : NetworkBehaviour, IDamageable
    {
        [SerializeField] private float _maxHp = 100f;
        [SerializeField] private int _totalLives = 3;
        [SerializeField] private FloatGameEvent _onHpChanged;
        [SerializeField] private GameEvent _onPlayerDied;

        public ulong LastAttackerClientId { get; set; }

        [Header("Network State")]
        private readonly NetworkVariable<float> _currentHp = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<float> CurrentHp => _currentHp;
        private readonly NetworkVariable<int> livesRemaining = new NetworkVariable<int>(3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _deathCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<int> DeathCount => _deathCount;

        [Header("Combat Feel / Juice [CombatFeedback]")]
        [SerializeField] private GameObject _deathVFXPrefab;
        [SerializeField] private GameObject _spawnVFXPrefab;
        [SerializeField] private AudioClip _deathSound;
        [SerializeField] private AudioClip _spawnSound;

        public static event System.Action<bool> OnLocalPlayerHitConfirmed;

        private bool _isDead = false;
        private bool _isInvincible = false;
        private Coroutine _invincibilityFlashRoutine;

        private void Awake()
        {
        }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _currentHp.OnValueChanged += OnHpValueChanged;

        if (IsServer)
        {
            _currentHp.Value = _maxHp;
            livesRemaining.Value = _totalLives;
            _deathCount.Value = 0;
        }

        // Initial UI update
        _onHpChanged?.Raise(_currentHp.Value);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        _currentHp.OnValueChanged -= OnHpValueChanged;
    }

    private void OnHpValueChanged(float previousValue, float newValue)
    {
        Debug.Log($"[PlayerHealth] OnHpValueChanged: {previousValue} -> {newValue}");
        _onHpChanged?.Raise(newValue);
    }

    /// <summary>
    /// IDamageable interface implementation. Delegates damage calculation to the server.
    /// </summary>
    public void TakeDamage(float amount, DamageType type = DamageType.Projectile, WeaponSO weaponUsed = null)
    {
        string weaponName = "Unknown";
        if (weaponUsed != null)
        {
            weaponName = !string.IsNullOrEmpty(weaponUsed.weaponName) ? weaponUsed.weaponName : weaponUsed.name;
        }
        else if (type == DamageType.Melee)
        {
            weaponName = "Melee";
        }

        // Call the Server Rpc so clients cannot cheat and apply arbitrary health modifications
        TakeDamageServerRpc(amount, type, weaponName, LastAttackerClientId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void TakeDamageServerRpc(float amount, DamageType type, string weaponName, ulong attackerClientId, RpcParams rpcParams = default)
    {
        if (!IsServer) return;

        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "WaitingRoom")
        {
            return;
        }

        if (_isDead)
        {
            Debug.Log("[PlayerHealth] Ignored damage - player already dead: " + OwnerClientId);
            return;
        }

        if (_isInvincible)
        {
            Debug.Log("[PlayerHealth] Ignored damage - respawn invincibility active: " + OwnerClientId);
            return;
        }

        if (_currentHp.Value <= 0f) return;

        // Friendly fire prevention for TDM
        if (GameModeBase.Instance is TDMMode tdm)
        {
            int victimTeam = tdm.GetPlayerTeam(OwnerClientId);
            int attackerTeam = tdm.GetPlayerTeam(attackerClientId);
            if (attackerClientId != OwnerClientId && victimTeam != -1 && attackerTeam != -1 && victimTeam == attackerTeam)
            {
                Debug.Log($"[PlayerHealth] Friendly fire prevented: Player {attackerClientId} tried to damage teammate {OwnerClientId} (Team {victimTeam})");
                return;
            }
        }

        float damageToApply = amount;
        var shield = GetComponent<PlayerShield>();
        if (shield != null && shield.IsShieldActive)
        {
            damageToApply = shield.AbsorbDamage(damageToApply);
            if (damageToApply <= 0f)
            {
                // Damage fully absorbed by shield
                return;
            }
        }

        float previousHp = _currentHp.Value;
        _currentHp.Value = Mathf.Max(_currentHp.Value - damageToApply, 0f);
        float actualDamage = previousHp - _currentHp.Value;
        Debug.Log($"[PlayerHealth] Player {OwnerClientId} took {amount} damage from Attacker {attackerClientId} (Type: {type}, Weapon: {weaponName}). Remaining HP: {_currentHp.Value}");

        if (actualDamage > 0f)
        {
            bool isKill = _currentHp.Value <= 0f;

            // Notify attacker client
            if (attackerClientId != OwnerClientId && NetworkManager.Singleton.ConnectedClients.ContainsKey(attackerClientId))
            {
                ClientRpcParams clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { attackerClientId }
                    }
                };
                NotifyHitConfirmedClientRpc(isKill, clientRpcParams);
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
        Debug.Log("[PlayerHealth] Death count updated: " + DeathCount.Value);

        // Decrement lives remaining
        livesRemaining.Value = Mathf.Max(livesRemaining.Value - 1, 0);
        Debug.Log($"[PlayerHealth] Player {OwnerClientId} died. Remaining lives: {livesRemaining.Value}");

        // Notify NetworkedGameManager of the kill feed event (skip self-kills)
        if (killerClientId != OwnerClientId)
            NetworkedGameManager.ReportKill(killerClientId, OwnerClientId, weaponName);
        else
            Debug.Log($"[PlayerHealth] Player {OwnerClientId} self-inflicted death (suppressed from kill feed)");

        // Notify all clients to trigger death VFX / UI
        DeathClientRpc(type);

        // Always respawn the player (removed death spawn limit cap)
        StartCoroutine(RespawnSequence());
    }

    private IEnumerator RespawnSequence()
    {
        yield return new WaitForSeconds(3f);

        _isDead = false;

        // Start server-side invincibility window
        StartCoroutine(InvincibilityRoutine());

        // Get dynamic spawn point from SpawnPointManager
        Vector3 spawnPos = Vector3.zero;
        if (SpawnPointManager.Instance != null)
        {
            SpawnPointType spawnType = SpawnPointType.FFA;
            if (GameModeBase.Instance is TDMMode tdm)
            {
                int team = tdm.GetPlayerTeam(OwnerClientId);
                if (team == 0) spawnType = SpawnPointType.TeamBlue;
                else if (team == 1) spawnType = SpawnPointType.TeamRed;
            }

            Transform sp = SpawnPointManager.Instance.GetSpawnPoint(spawnType);
            if (sp != null)
            {
                spawnPos = sp.position;
            }
        }
        else
        {
            spawnPos = new Vector3(OwnerClientId * 2f, 0.5f, 0f);
        }

        // Refill health
        _currentHp.Value = _maxHp;

        // Re-enable and reposition on all clients
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
        Debug.Log($"[CombatFeedback] NotifyHitConfirmedClientRpc received. IsKill: {isKill}");
        OnLocalPlayerHitConfirmed?.Invoke(isKill);
    }

    [ClientRpc]
    private void DeathClientRpc(DamageType type)
    {
        Debug.Log($"[PlayerHealth] DeathClientRpc received for Player {OwnerClientId}");

        _onPlayerDied?.Raise();

        // Spawn death VFX and play sound
        if (_deathVFXPrefab != null)
        {
            Instantiate(_deathVFXPrefab, transform.position, Quaternion.identity);
        }
        if (_deathSound != null)
        {
            AudioSource.PlayClipAtPoint(_deathSound, transform.position, 1f);
        }

        // Hide mesh and disable control locally
        SetPlayerVisibility(false);
    }

    [ClientRpc]
    private void RespawnClientRpc(Vector3 spawnPosition)
    {
        Debug.Log($"[PlayerHealth] RespawnClientRpc received for Player {OwnerClientId}");

        // Reposition character safely by temporarily disabling the character controller
        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        transform.position = spawnPosition;
        if (cc != null) cc.enabled = true;

        // Restore visibility and control
        SetPlayerVisibility(true);

        // Spawn respawn VFX and play sound
        if (_spawnVFXPrefab != null)
        {
            Instantiate(_spawnVFXPrefab, transform.position, Quaternion.identity);
        }
        if (_spawnSound != null)
        {
            AudioSource.PlayClipAtPoint(_spawnSound, transform.position, 1f);
        }

        // Trigger visual invincibility flashing for exactly 1.5 seconds (matching invincibility timing from 4.2)
        if (_invincibilityFlashRoutine != null) StopCoroutine(_invincibilityFlashRoutine);
        _invincibilityFlashRoutine = StartCoroutine(InvincibilityFlashRoutine());
    }

    private IEnumerator InvincibilityFlashRoutine()
    {
        float duration = 1.5f; // Reuse invincibility duration from 4.2
        float interval = 0.1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (_isDead) yield break;

            var renderers = GetComponentsInChildren<Renderer>(true);
            bool visible = (Mathf.FloorToInt(elapsed / interval) % 2) == 0;
            foreach (var r in renderers)
            {
                if (r != null) r.enabled = visible;
            }

            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }

        // Final restore
        if (!_isDead)
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r != null) r.enabled = true;
            }
        }
        _invincibilityFlashRoutine = null;
    }

    private void SetPlayerVisibility(bool visible)
    {
        if (!visible && _invincibilityFlashRoutine != null)
        {
            StopCoroutine(_invincibilityFlashRoutine);
            _invincibilityFlashRoutine = null;
        }

        // Hide renderers
        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            r.enabled = visible;
        }

        // Disable character motor movement FSM
        var motor = GetComponent<CharacterMotor>();
        if (motor != null)
        {
            motor.enabled = visible;
        }

        // Disable player input routing
        var input = GetComponent<PlayerInputRouter>();
        if (input != null)
        {
            input.enabled = visible;
        }
    }

    /// <summary>
    /// Restores health up to max HP on the server.
    /// </summary>
    public void Heal(float amount)
    {
        HealServerRpc(amount);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void HealServerRpc(float amount)
    {
        if (!IsServer) return;
        if (_currentHp.Value <= 0f) return;

        _currentHp.Value = Mathf.Min(_currentHp.Value + amount, _maxHp);
    }

    public float GetCurrentHp() => _currentHp.Value;
    public float GetMaxHp() => _maxHp;

    public void Teleport(Vector3 position)
    {
        if (!IsServer) return;
        TeleportClientRpc(position);
    }

    [ClientRpc]
    private void TeleportClientRpc(Vector3 position)
    {
        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        transform.position = position;
        if (cc != null) cc.enabled = true;
    }
}
}
