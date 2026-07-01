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

        [Header("Network State")]
        private readonly NetworkVariable<float> _currentHp = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> livesRemaining = new NetworkVariable<int>(3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

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
        // Call the Server Rpc so clients cannot cheat and apply arbitrary health modifications
        TakeDamageServerRpc(amount, type, weaponUsed != null ? weaponUsed.name : "Unknown");
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void TakeDamageServerRpc(float amount, DamageType type, string weaponName, RpcParams rpcParams = default)
    {
        if (!IsServer) return;

        if (_currentHp.Value <= 0f) return;

        _currentHp.Value = Mathf.Max(_currentHp.Value - amount, 0f);
        ulong killerClientId = rpcParams.Receive.SenderClientId;
        Debug.Log($"[PlayerHealth] Player {OwnerClientId} took {amount} damage from Client {killerClientId} (Type: {type}, Weapon: {weaponName}). Remaining HP: {_currentHp.Value}");

        if (_currentHp.Value <= 0f)
        {
            HandleDeath(killerClientId, type, weaponName);
        }
    }

    private void HandleDeath(ulong killerClientId, DamageType type, string weaponName)
    {
        if (!IsServer) return;

        // Decrement lives remaining
        livesRemaining.Value = Mathf.Max(livesRemaining.Value - 1, 0);
        Debug.Log($"[PlayerHealth] Player {OwnerClientId} died. Remaining lives: {livesRemaining.Value}");

        // Notify NetworkedGameManager of the kill feed event
        NetworkedGameManager.ReportKill(killerClientId, OwnerClientId, weaponName);

        // Notify all clients to trigger death VFX / UI
        DeathClientRpc(type);

        if (livesRemaining.Value > 0)
        {
            StartCoroutine(RespawnSequence());
        }
        else
        {
            Debug.Log($"[PlayerHealth] Player {OwnerClientId} has no lives remaining.");
        }
    }

    private IEnumerator RespawnSequence()
    {
        yield return new WaitForSeconds(3f);

        // Reposition player (Client index determines initial spawn coordinate offset)
        Vector3 spawnPos = new Vector3(OwnerClientId * 2f, 0.5f, 0f);

        // Refill health
        _currentHp.Value = _maxHp;

        // Re-enable and reposition on all clients
        RespawnClientRpc(spawnPos);
    }

    [ClientRpc]
    private void DeathClientRpc(DamageType type)
    {
        Debug.Log($"[PlayerHealth] DeathClientRpc received for Player {OwnerClientId}");

        _onPlayerDied?.Raise();

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
    }

    private void SetPlayerVisibility(bool visible)
    {
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
}
}
