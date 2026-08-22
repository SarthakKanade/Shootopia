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
        [SerializeField] private PlayerAudioSO _playerAudioConfig;
        [SerializeField] private GameObject _deathVFXPrefab;
        [SerializeField] private GameObject _spawnVFXPrefab;
        [SerializeField] private AudioClip _deathSound;
        [SerializeField] private AudioClip _spawnSound;

        public static event System.Action<bool> OnLocalPlayerHitConfirmed;

        private bool _isDead = false;
        public bool IsDead => _isDead;
        private bool _isInvincible = false;
        private Coroutine _invincibilityFlashRoutine;
        private Coroutine _deathVFXRoutine;

        private void Awake()
        {
        }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _currentHp.OnValueChanged += OnHpValueChanged;
        SetPlayerVisibility(true);

        if (IsServer)
        {
            _currentHp.Value = _maxHp;
            livesRemaining.Value = _totalLives;
            _deathCount.Value = 0;

        if (transform.position.y <= 1.0f && SpawnPointManager.Instance != null)
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
                if (IsServer)
                {
                    Teleport(sp.position);
                }
                else if (IsOwner)
                {
                    var cc = GetComponent<CharacterController>();
                    if (cc != null) cc.enabled = false;
                    transform.position = sp.position;
                    transform.rotation = sp.rotation;
                    if (cc != null) cc.enabled = true;

                    var motor = GetComponent<CharacterMotor>();
                    if (motor != null)
                    {
                        motor.VerticalVelocity = 0f;
                        motor.IsInLava = false;
                    }
                }
            }
        }
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

        if (newValue > 0f && _isDead)
        {
            _isDead = false;
            SetPlayerVisibility(true);
        }

        if (newValue < previousValue && IsOwner && AudioManager.Instance != null)
        {
            AudioClip hitClip = (_playerAudioConfig != null && _playerAudioConfig.hitTakenClip != null) ? _playerAudioConfig.hitTakenClip : null;
            if (hitClip != null)
            {
                AudioManager.Instance.PlaySFX2D(hitClip);
            }
        }
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

        // Ignore damage when match is not active (Warmup Lobby)
        if (NetworkedGameManager.Instance != null && NetworkedGameManager.Instance.ActiveGameMode != null && !NetworkedGameManager.Instance.ActiveGameMode.MatchActive.Value)
        {
            return;
        }
        if (GameModeBase.Instance != null && !GameModeBase.Instance.MatchActive.Value)
        {
            return;
        }

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
        Vector3 spawnPos = new Vector3(120f, 16f, 2.5f);
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

        // Refill health
        _currentHp.Value = _maxHp;

        // Server-side repositioning
        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        transform.position = spawnPos;
        if (cc != null) cc.enabled = true;

        var motor = GetComponent<CharacterMotor>();
        if (motor != null)
        {
            motor.VerticalVelocity = 0f;
            motor.IsInLava = false;
        }

        var input = GetComponent<PlayerInputRouter>();
        if (input != null) input.DisableControls = false;

        RemoveBurnEffect();

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

        if (AudioManager.Instance != null && _playerAudioConfig != null && _playerAudioConfig.hitMarkerClip != null)
        {
            AudioManager.Instance.PlaySFX2D(_playerAudioConfig.hitMarkerClip);
        }
    }

    [ClientRpc]
    private void DeathClientRpc(DamageType type)
    {
        Debug.Log($"[PlayerHealth] DeathClientRpc received for Player {OwnerClientId}");

        _onPlayerDied?.Raise();

        // Play death sound on dying player's client only (not broadcast to all — avoids audio spam in FFA)
        if (IsOwner && AudioManager.Instance != null)
        {
            AudioClip deathClip = (_playerAudioConfig != null && _playerAudioConfig.deathClip != null) ? _playerAudioConfig.deathClip : _deathSound;
            if (deathClip != null)
            {
                AudioManager.Instance.PlaySFX2D(deathClip);
            }
        }
        else if (_deathSound != null && AudioManager.Instance == null)
        {
            AudioSource.PlayClipAtPoint(_deathSound, transform.position, 1f);
        }

        // Trigger death animation on Animator
        Animator anim = GetComponentInChildren<Animator>(true);
        if (anim != null)
        {
            if (anim.layerCount > 1)
            {
                anim.SetLayerWeight(1, 0f); // Mute upper body layer so it doesn't override full-body death animation
            }
            anim.SetTrigger("Die");
        }

        // Disable player controls locally while keeping renderers visible for the death animation
        SetPlayerControlsEnabled(false);

        // Hide model after 1 second of playing death animation (no death VFX)
        if (_deathVFXRoutine != null) StopCoroutine(_deathVFXRoutine);
        _deathVFXRoutine = StartCoroutine(HideModelAfterDeathRoutine());
    }

    private IEnumerator HideModelAfterDeathRoutine()
    {
        // Wait 1 second after death animation starts
        yield return new WaitForSeconds(1.0f);

        // Directly hide character renderers — NO VFX
        SetPlayerVisibility(false);
        _deathVFXRoutine = null;
    }

    [ClientRpc]
    private void RespawnClientRpc(Vector3 spawnPosition)
    {
        Debug.Log($"[PlayerHealth] RespawnClientRpc received for Player {OwnerClientId}");

        if (_deathVFXRoutine != null)
        {
            StopCoroutine(_deathVFXRoutine);
            _deathVFXRoutine = null;
        }

        // Reposition character safely by temporarily disabling the character controller
        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        transform.position = spawnPosition;
        if (cc != null) cc.enabled = true;

        // Reset animator state back to locomotion
        Animator anim = GetComponentInChildren<Animator>(true);
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

        // Restore controls and visibility
        SetPlayerControlsEnabled(true);
        SetPlayerVisibility(true);
        RemoveBurnEffect();

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

    private void SetPlayerControlsEnabled(bool enabled)
    {
        // Disable/enable character motor movement FSM
        var motor = GetComponent<CharacterMotor>();
        if (motor != null)
        {
            motor.enabled = enabled;
        }

        // Disable/enable player input routing
        var input = GetComponent<PlayerInputRouter>();
        if (input != null)
        {
            input.enabled = enabled;
        }
    }

    private void SetPlayerVisibility(bool visible)
    {
        if (!visible && _invincibilityFlashRoutine != null)
        {
            StopCoroutine(_invincibilityFlashRoutine);
            _invincibilityFlashRoutine = null;
        }

        // Hide/show renderers
        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            r.enabled = visible;
        }

        SetPlayerControlsEnabled(visible);
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
        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        transform.position = position;
        if (cc != null) cc.enabled = true;

        TeleportClientRpc(position);
    }

    [ClientRpc]
    private void TeleportClientRpc(Vector3 position)
    {
        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        transform.position = position;
        if (cc != null) cc.enabled = true;

        var input = GetComponent<PlayerInputRouter>();
        if (input != null) input.DisableControls = false;

        var motor = GetComponent<CharacterMotor>();
        if (motor != null)
        {
            motor.VerticalVelocity = 0f;
            motor.IsInLava = false;
        }
        RemoveBurnEffect();
    }

    public void ResetStateForMatch(Vector3 position)
    {
        if (!IsServer) return;
        _isDead = false;
        _currentHp.Value = _maxHp;

        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        transform.position = position;
        if (cc != null) cc.enabled = true;

        var motor = GetComponent<CharacterMotor>();
        if (motor != null)
        {
            motor.VerticalVelocity = 0f;
            motor.IsInLava = false;
        }

        var input = GetComponent<PlayerInputRouter>();
        if (input != null) input.DisableControls = false;

        RemoveBurnEffect();

        ResetPlayerClientRpc(position);
    }

    [ClientRpc]
    private void ResetPlayerClientRpc(Vector3 position)
    {
        _isDead = false;

        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        transform.position = position;
        if (cc != null) cc.enabled = true;

        SetPlayerControlsEnabled(true);
        SetPlayerVisibility(true);
        RemoveBurnEffect();

        var input = GetComponent<PlayerInputRouter>();
        if (input != null) input.DisableControls = false;

        var motor = GetComponent<CharacterMotor>();
        if (motor != null)
        {
            motor.VerticalVelocity = 0f;
            motor.IsInLava = false;
        }

        Animator anim = GetComponentInChildren<Animator>(true);
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

    private Coroutine _freezeRoutine;
    private bool _isFrozen = false;
    public bool IsFrozen => _isFrozen;

    /// <summary>
    /// Applies freeze or slow status effect.
    /// If isDirectHit is true: 100% complete freeze (MoveSpeedMultiplier = 0) for 5 seconds.
    /// If isDirectHit is false: Applies proportional speed slow based on distance to POC.
    /// </summary>
    public void ApplyFreezeEffect(float duration, float slowPercent, bool isDirectHit)
    {
        ApplyFreezeRpc(duration, slowPercent, isDirectHit);
    }

    [Rpc(SendTo.Everyone)]
    private void ApplyFreezeRpc(float duration, float slowPercent, bool isDirectHit)
    {
        if (_freezeRoutine != null)
        {
            StopCoroutine(_freezeRoutine);
        }
        _freezeRoutine = StartCoroutine(FreezeRoutine(duration, slowPercent, isDirectHit));
    }

    private static MaterialPropertyBlock _statusMPB;
    private static MaterialPropertyBlock StatusMPB
    {
        get
        {
            if (_statusMPB == null) _statusMPB = new MaterialPropertyBlock();
            return _statusMPB;
        }
    }

    private IEnumerator FreezeRoutine(float duration, float slowPercent, bool isDirectHit)
    {
        CharacterMotor motor = GetComponent<CharacterMotor>();
        Animator anim = GetComponentInChildren<Animator>();

        _isFrozen = isDirectHit;

        if (motor != null)
        {
            if (isDirectHit)
            {
                // 100% complete freeze
                motor.MoveSpeedMultiplier = 0f;
            }
            else
            {
                // Proportional slow multiplier (e.g., 80% slow -> MoveSpeedMultiplier = 0.20f)
                float mult = Mathf.Clamp01(1f - slowPercent);
                motor.MoveSpeedMultiplier = mult;
            }
        }

        if (anim != null)
        {
            if (isDirectHit)
            {
                anim.speed = 0f; // Freeze animation pose completely in place
            }
            else
            {
                float mult = Mathf.Clamp01(1f - slowPercent);
                anim.speed = Mathf.Clamp(mult, 0.15f, 1f); // Slow-motion animation matching movement speed!
            }
        }

        // Apply Light Translucent Bluish Frost Ice Visual Indicator via MaterialPropertyBlock
        var renderers = GetComponentsInChildren<Renderer>(true);
        Color iceTint = new Color(0.55f, 0.85f, 1.0f, 0.65f); // Light translucent ice blue
        Color iceEmission = new Color(0.18f, 0.55f, 0.90f) * 1.5f; // Cool frost glow

        MaterialPropertyBlock mpb = StatusMPB;
        mpb.Clear();
        mpb.SetColor("_BaseColor", iceTint);
        mpb.SetColor("_Color", iceTint);
        mpb.SetColor("_EmissionColor", iceEmission);

        foreach (var r in renderers)
        {
            if (r == null || r is ParticleSystemRenderer) continue;
            r.SetPropertyBlock(mpb);
        }

        yield return new WaitForSeconds(duration);

        // Instant restoration of original material properties without memory allocations
        foreach (var r in renderers)
        {
            if (r != null && !(r is ParticleSystemRenderer))
            {
                r.SetPropertyBlock(null);
            }
        }

        if (motor != null)
        {
            motor.MoveSpeedMultiplier = 1f;
        }
        if (anim != null)
        {
            anim.speed = 1f;
        }

        _isFrozen = false;
        _freezeRoutine = null;
    }

    private Coroutine _burnRoutine;
    private bool _isBurning = false;
    public bool IsBurning => _isBurning;

    /// <summary>
    /// Applies 5-second burning status effect with passive Damage-Over-Time (DoT) and fiery visual tint.
    /// </summary>
    public void ApplyBurnEffect(float duration = 5.0f, float dps = 5.0f, WeaponSO weaponUsed = null)
    {
        ApplyBurnRpc(duration, dps);
    }

    /// <summary>
    /// Immediately stops burning status effect and restores original materials.
    /// </summary>
    public void RemoveBurnEffect()
    {
        RemoveBurnRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void RemoveBurnRpc()
    {
        if (_burnRoutine != null)
        {
            StopCoroutine(_burnRoutine);
            _burnRoutine = null;
        }
        _isBurning = false;

        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r != null && !(r is ParticleSystemRenderer))
            {
                r.SetPropertyBlock(null);
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void ApplyBurnRpc(float duration, float dps)
    {
        if (_burnRoutine != null)
        {
            StopCoroutine(_burnRoutine);
        }
        _burnRoutine = StartCoroutine(BurnRoutine(duration, dps));
    }

    private IEnumerator BurnRoutine(float duration, float dps)
    {
        _isBurning = true;

        // Apply Fiery Orange/Red Burn Visual Indicator via MaterialPropertyBlock
        var renderers = GetComponentsInChildren<Renderer>(true);
        Color burnTint = new Color(1.0f, 0.35f, 0.05f, 0.9f); // Fiery orange/red
        Color burnEmission = new Color(2.0f, 0.5f, 0.05f) * 1.5f; // Fiery emission glow

        MaterialPropertyBlock mpb = StatusMPB;
        mpb.Clear();
        mpb.SetColor("_BaseColor", burnTint);
        mpb.SetColor("_Color", burnTint);
        mpb.SetColor("_EmissionColor", burnEmission);

        foreach (var r in renderers)
        {
            if (r == null || r is ParticleSystemRenderer) continue;
            r.SetPropertyBlock(mpb);
        }

        float elapsed = 0f;
        float tickInterval = 0.5f;
        float damagePerTick = dps * tickInterval;

        while (elapsed < duration)
        {
            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;

            if (_isDead) break;

            // Apply passive burn damage on server
            if (IsServer)
            {
                TakeDamageServerRpc(damagePerTick, DamageType.Environment, "FireBurn", LastAttackerClientId);
            }
        }

        // Instant restoration of original material properties
        foreach (var r in renderers)
        {
            if (r != null && !(r is ParticleSystemRenderer))
            {
                r.SetPropertyBlock(null);
            }
        }

        _isBurning = false;
        _burnRoutine = null;
    }
}
}
