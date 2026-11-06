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
    public class PlayerHealth : NetworkBehaviour, IDamageable
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

        private bool _isDead = false;
        private bool _isInvincible = false;
        private Coroutine _invincibilityRoutine;
        private Coroutine _invincibilityFlashRoutine;
        private Coroutine _hideModelRoutine;
        private GameObject _invincibilityAuraInstance;
        private Material _invincibilitySkinMaterial;
        private PlayerStatusEffects _statusEffects;

        public bool IsDead => _isDead;
        public bool IsInvincible => _isInvincible;
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

                // Only grant spawn invincibility on spawn if match is already active (e.g. late join during active match) or offline sandbox
                bool isMatchActive = (GameModeBase.Instance != null && (GameModeBase.Instance.MatchActive.Value || GameModeBase.Instance.IsInProgress))
                                     || (NetworkedGameManager.Instance != null && 
                                         (NetworkedGameManager.Instance.MatchStarted.Value || 
                                          (NetworkedGameManager.Instance.ActiveGameMode != null && (NetworkedGameManager.Instance.ActiveGameMode.MatchActive.Value || NetworkedGameManager.Instance.ActiveGameMode.IsInProgress))))
                                     || (NetworkedGameManager.Instance == null);
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

        #region Damage & Health Flow

        public void TakeDamage(float amount, DamageType type = DamageType.Projectile, WeaponSO weaponUsed = null)
        {
            TakeDamage(amount, type, weaponUsed, ulong.MaxValue);
        }

        public void TakeDamage(float amount, DamageType type, WeaponSO weaponUsed, ulong attackerClientId)
        {
            bool isLethalAbyss = (type == DamageType.Environment && amount >= 9000f);
            if (_isDead || (_isInvincible && !isLethalAbyss)) return;

            string weaponName = weaponUsed != null ? (!string.IsNullOrEmpty(weaponUsed.weaponName) ? weaponUsed.weaponName : weaponUsed.name)
                              : (type == DamageType.Melee ? "Melee" : (type == DamageType.Environment ? "Lava" : "Unknown"));

            ulong resolvedAttacker = (attackerClientId != ulong.MaxValue) 
                ? attackerClientId 
                : (type == DamageType.Environment || NetworkManager.Singleton == null || !NetworkManager.Singleton.ConnectedClients.ContainsKey(LastAttackerClientId) ? OwnerClientId : LastAttackerClientId);

            if (IsSpawned)
            {
                TakeDamageServerRpc(amount, type, weaponName, resolvedAttacker);
            }
            else
            {
                // Offline / sandbox fallback
                float damageToApply = amount;
                var shield = GetComponent<PlayerShield>();
                if (shield != null && shield.IsShieldActive)
                {
                    damageToApply = shield.AbsorbDamage(damageToApply);
                    if (damageToApply <= 0f) return;
                }

                float previousHp = _currentHp.Value;
                _currentHp.Value = Mathf.Max(_currentHp.Value - damageToApply, 0f);
                float actual = previousHp - _currentHp.Value;
                Vector3 fallbackPos = transform.position - transform.forward * 10f;
                OnLocalPlayerDamagedWithPosition?.Invoke(fallbackPos, actual);
                HUDController.Instance?.TriggerDamageFlash(actual);
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void TakeDamageServerRpc(float amount, DamageType type, string weaponName, ulong attackerClientId)
        {
            bool isLethalAbyss = (type == DamageType.Environment && amount >= 9000f);
            if (!IsServer || _isDead || (_isInvincible && !isLethalAbyss) || _currentHp.Value <= 0f) return;

            // Inactive match or waiting room damage gating
            if (SceneManager.GetActiveScene().name == "WaitingRoom") return;
            
            var activeMode = NetworkedGameManager.Instance != null && NetworkedGameManager.Instance.ActiveGameMode != null 
                ? NetworkedGameManager.Instance.ActiveGameMode 
                : GameModeBase.Instance;

            bool isSelfOrEnvironment = (type == DamageType.Environment || attackerClientId == OwnerClientId);
            bool isMatchActive = (activeMode != null && activeMode.MatchActive.Value) || 
                                 (NetworkedGameManager.Instance != null && NetworkedGameManager.Instance.MatchStarted.Value);
            if (!isSelfOrEnvironment && !isMatchActive) return;

            // TDM Friendly Fire prevention
            if (activeMode is TDMMode tdm)
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

                // Notify victim of attacker position for 360 threat indicator & damage flash
                Vector3 attackerPos = transform.position + transform.forward * 8f;
                if (NetworkManager.Singleton != null && 
                    NetworkManager.Singleton.ConnectedClients.TryGetValue(attackerClientId, out var client) && 
                    client.PlayerObject != null)
                {
                    attackerPos = client.PlayerObject.transform.position;
                }

                var victimParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
                };
                NotifyDamageTakenClientRpc(attackerPos, actualDamage, victimParams);

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

            ulong reportedKiller = (type == DamageType.Environment || killerClientId == ulong.MaxValue || killerClientId == OwnerClientId) ? OwnerClientId : killerClientId;
            string reportedWeapon = (type == DamageType.Environment && (string.IsNullOrEmpty(weaponName) || weaponName == "Unknown")) ? "Lava" : weaponName;
            NetworkedGameManager.ReportKill(reportedKiller, OwnerClientId, reportedWeapon);

            Vector3 spawnPos = new Vector3(120f, 16f, 2.5f);
            if (SpawnPointManager.Instance != null)
            {
                Transform sp = SpawnPointManager.Instance.GetSpawnPoint(GetSpawnTypeForPlayer());
                if (sp != null) spawnPos = sp.position;
            }

            DeathClientRpc(type, spawnPos);
            StartCoroutine(RespawnSequence(spawnPos));
        }

        private IEnumerator RespawnSequence(Vector3 spawnPos)
        {
            // Wait 1.0 second while death animation plays
            yield return new WaitForSeconds(1.0f);

            // Move server representation to spawn point while invisible
            SetCharacterPosition(spawnPos);
            Physics.SyncTransforms();

            // Wait remaining 2.0 seconds (total 3.0s delay) while client transform & buffers settle at spawnPos
            yield return new WaitForSeconds(2.0f);

            StartInvincibilityServer(spawnInvincibilityDuration);

            SetCharacterPosition(spawnPos);
            Physics.SyncTransforms();
            var netTransform = GetComponent<Unity.Netcode.Components.NetworkTransform>();
            if (netTransform != null && netTransform.CanCommitToTransform)
            {
                try { netTransform.Teleport(spawnPos, transform.rotation, transform.localScale); } catch { }
            }
            ResetMovementState();

            _currentHp.Value = maxHp;
            _isDead = false;

            // Ensure player respawns unarmed
            var weaponCtrl = GetComponent<WeaponController>();
            if (weaponCtrl != null) weaponCtrl.UnequipAll();

            RemoveBurnEffect();
            RespawnClientRpc(spawnPos);
        }

        public void StartInvincibilityServer(float duration = 5.0f)
        {
            if (_invincibilityRoutine != null) StopCoroutine(_invincibilityRoutine);
            _invincibilityRoutine = StartCoroutine(InvincibilityRoutine(duration));
            TriggerSpawnInvincibilityClientRpc(duration);
        }

        private IEnumerator InvincibilityRoutine(float duration)
        {
            _isInvincible = true;
            yield return new WaitForSeconds(duration);
            _isInvincible = false;
            _invincibilityRoutine = null;
        }

        [ClientRpc]
        private void TriggerSpawnInvincibilityClientRpc(float duration)
        {
            StartInvincibilityLocal(duration);
        }

        public void StartInvincibilityLocal(float duration)
        {
            _isInvincible = true;
            if (_invincibilityRoutine != null) StopCoroutine(_invincibilityRoutine);
            _invincibilityRoutine = StartCoroutine(LocalInvincibilityRoutine(duration));
        }

        private IEnumerator LocalInvincibilityRoutine(float duration)
        {
            _isInvincible = true;
            ShowInvincibilityAura(true);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (_isDead)
                {
                    ShowInvincibilityAura(false);
                    _isInvincible = false;
                    _invincibilityRoutine = null;
                    yield break;
                }

                float remaining = duration - elapsed;
                // In the last 1.2 seconds, blink the skin layer to signal it's ending
                if (remaining < 1.2f && _invincibilityAuraInstance != null)
                {
                    bool blinkVisible = (Mathf.FloorToInt(remaining * 8f) % 2) == 0;
                    _invincibilityAuraInstance.SetActive(blinkVisible);
                }
                else if (_invincibilitySkinMaterial != null)
                {
                    // Gentle, translucent breathing energy sheen across the skin layer
                    float pulse = 0.70f + Mathf.Sin(elapsed * 5.0f) * 0.20f;
                    _invincibilitySkinMaterial.SetColor("_EmissionColor", new Color(1.0f, 0.72f, 0.20f) * pulse);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            ShowInvincibilityAura(false);
            _isInvincible = false;
            _invincibilityRoutine = null;
        }

        private Material GetOrCreateInvincibilityMaterial()
        {
            if (_invincibilitySkinMaterial == null)
            {
                Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Unlit");
                _invincibilitySkinMaterial = new Material(s);
                _invincibilitySkinMaterial.name = "InvincibilitySkinMat_Runtime";
                _invincibilitySkinMaterial.SetFloat("_Surface", 1.0f); // Transparent
                _invincibilitySkinMaterial.SetFloat("_Blend", 0.0f);   // 0 = Alpha blend for translucent see-through
                _invincibilitySkinMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _invincibilitySkinMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _invincibilitySkinMaterial.SetInt("_ZWrite", 0);
                _invincibilitySkinMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                _invincibilitySkinMaterial.EnableKeyword("_EMISSION");
                _invincibilitySkinMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 100;
                // Soft translucent amber-gold sheen (highly see-through, letting character clothes/textures show clearly)
                _invincibilitySkinMaterial.SetColor("_BaseColor", new Color(1.0f, 0.82f, 0.25f, 0.20f));
                _invincibilitySkinMaterial.SetColor("_EmissionColor", new Color(1.0f, 0.72f, 0.20f) * 0.75f);
            }
            return _invincibilitySkinMaterial;
        }

        private void ShowInvincibilityAura(bool show)
        {
            if (show)
            {
                if (_invincibilityAuraInstance == null || _invincibilityAuraInstance.transform.childCount == 0)
                {
                    if (_invincibilityAuraInstance != null)
                    {
                        Destroy(_invincibilityAuraInstance);
                    }

                    // Body-hugging translucent skin overlay across the player's active model itself (COD / PUBG spawn protection style)
                    _invincibilityAuraInstance = new GameObject("SpawnInvincibilitySkinLayer");
                    _invincibilityAuraInstance.transform.SetParent(transform, false);
                    _invincibilityAuraInstance.transform.localPosition = Vector3.zero;
                    _invincibilityAuraInstance.transform.localRotation = Quaternion.identity;
                    _invincibilityAuraInstance.transform.localScale = Vector3.one;

                    var mat = GetOrCreateInvincibilityMaterial();

                    // 1. SkinnedMeshRenderers (body, head, limbs) - strictly matching only the currently ACTIVE model (Vanguard or Valkyrie)
                    var smrs = GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    foreach (var smr in smrs)
                    {
                        if (smr == null || smr.transform.IsChildOf(_invincibilityAuraInstance.transform)) continue;
                        if (!smr.gameObject.activeInHierarchy || !smr.enabled) continue; // Skip inactive character variants

                        var overlayObj = new GameObject($"{smr.gameObject.name}_SkinOverlay");
                        overlayObj.transform.SetParent(_invincibilityAuraInstance.transform, false);

                        var overlaySmr = overlayObj.AddComponent<SkinnedMeshRenderer>();
                        overlaySmr.sharedMesh = smr.sharedMesh;
                        overlaySmr.bones = smr.bones;
                        overlaySmr.rootBone = smr.rootBone;
                        overlaySmr.material = mat;
                        overlaySmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        overlaySmr.receiveShadows = false;
                    }

                    // 2. MeshRenderers (helmets, attachments, props if any)
                    var mrs = GetComponentsInChildren<MeshRenderer>(true);
                    foreach (var mr in mrs)
                    {
                        if (mr == null || mr.transform.IsChildOf(_invincibilityAuraInstance.transform)) continue;
                        if (!mr.gameObject.activeInHierarchy || !mr.enabled) continue;
                        var mf = mr.GetComponent<MeshFilter>();
                        if (mf == null || mf.sharedMesh == null) continue;

                        var overlayObj = new GameObject($"{mr.gameObject.name}_SkinOverlay");
                        overlayObj.transform.SetParent(mr.transform, false);
                        overlayObj.transform.localPosition = Vector3.zero;
                        overlayObj.transform.localRotation = Quaternion.identity;
                        overlayObj.transform.localScale = Vector3.one;

                        var overlayFilter = overlayObj.AddComponent<MeshFilter>();
                        overlayFilter.sharedMesh = mf.sharedMesh;

                        var overlayRend = overlayObj.AddComponent<MeshRenderer>();
                        overlayRend.material = mat;
                        overlayRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        overlayRend.receiveShadows = false;
                    }
                }

                _invincibilityAuraInstance.SetActive(true);
            }
            else
            {
                if (_invincibilityAuraInstance != null)
                {
                    _invincibilityAuraInstance.SetActive(false);
                }
            }
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
        private void NotifyDamageTakenClientRpc(Vector3 attackerWorldPos, float damageAmount, ClientRpcParams clientRpcParams = default)
        {
            if (!IsOwner) return;

            OnLocalPlayerDamagedWithPosition?.Invoke(attackerWorldPos, damageAmount);
            HUDController.Instance?.TriggerDamageFlash(damageAmount);
            CameraShakeManager.Instance?.ShakeDamage(damageAmount);
            MobileHapticsManager.TriggerDamageTaken();
        }

        [ClientRpc]
        private void DeathClientRpc(DamageType type, Vector3 spawnPos)
        {
            _isDead = true;
            _isInvincible = false;
            ShowInvincibilityAura(false);
            onPlayerDied?.Raise();

            // Immediately hide overhead nameplate so it never lingers on the death spot
            var nameplate = GetComponentInChildren<PlayerNameplate>(true);
            if (nameplate != null) nameplate.SetDeathHidden(true);

            if (IsOwner)
            {
                OnLocalPlayerDied?.Invoke(3.0f);
                if (AudioManager.Instance != null)
                {
                    AudioClip deathClip = (playerAudioConfig != null && playerAudioConfig.deathClip != null) ? playerAudioConfig.deathClip : deathSound;
                    if (deathClip != null) AudioManager.Instance.PlaySFX2D(deathClip, 1f, isCombatPriority: true);
                }
            }

            var anim = GetComponentInChildren<Animator>(true);
            if (anim != null)
            {
                if (anim.layerCount > 1) anim.SetLayerWeight(1, 0f);
                anim.SetTrigger("Die");
            }

            SetPlayerControlsEnabled(false);
            SetCollidersEnabled(false);

            var weaponCtrl = GetComponent<WeaponController>();
            if (weaponCtrl != null) weaponCtrl.OnPlayerDied();

            if (_hideModelRoutine != null) StopCoroutine(_hideModelRoutine);
            _hideModelRoutine = StartCoroutine(HideModelAndTeleportRoutine(spawnPos));
        }

        private IEnumerator HideModelAndTeleportRoutine(Vector3 spawnPos)
        {
            yield return new WaitForSeconds(1.0f);
            SetPlayerVisibility(false);

            // Teleport to spawn position while invisible so network transform & interpolation buffers settle ahead of time
            Vector3 delta = spawnPos - transform.position;
            SetCharacterPosition(spawnPos);
            Physics.SyncTransforms();

            var netTransform = GetComponent<Unity.Netcode.Components.NetworkTransform>();
            if (netTransform != null && netTransform.CanCommitToTransform)
            {
                try { netTransform.Teleport(spawnPos, transform.rotation, transform.localScale); } catch { }
            }

            if (IsOwner)
            {
                var cinemachineCam = FindAnyObjectByType<Unity.Cinemachine.CinemachineCamera>();
                if (cinemachineCam != null)
                {
                    cinemachineCam.OnTargetObjectWarped(transform, delta);
                }
            }

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

            _isDead = false;

            StartInvincibilityLocal(spawnInvincibilityDuration);

            SetCharacterPosition(spawnPosition);
            Physics.SyncTransforms();

            var netTransform = GetComponent<Unity.Netcode.Components.NetworkTransform>();
            if (netTransform != null && netTransform.CanCommitToTransform)
            {
                try { netTransform.Teleport(spawnPosition, transform.rotation, transform.localScale); } catch { }
            }

            ResetLocomotionAnimator();
            ResetMovementState();

            SetCollidersEnabled(true);
            SetPlayerVisibility(true);
            RemoveBurnEffect();

            var weaponCtrl = GetComponent<WeaponController>();
            if (weaponCtrl != null) weaponCtrl.UnequipAllLocal();

            if (spawnVFXPrefab != null) Instantiate(spawnVFXPrefab, spawnPosition, Quaternion.identity);
            if (spawnSound != null) AudioSource.PlayClipAtPoint(spawnSound, spawnPosition, 1f);

            if (IsOwner)
            {
                SetPlayerControlsEnabled(true);
                OnLocalPlayerRespawned?.Invoke();
            }
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
            Physics.SyncTransforms();
            var netTransform = GetComponent<Unity.Netcode.Components.NetworkTransform>();
            if (netTransform != null && netTransform.CanCommitToTransform)
            {
                try { netTransform.Teleport(position, transform.rotation, transform.localScale); } catch { }
            }
            TeleportClientRpc(position);
        }

        [ClientRpc]
        private void TeleportClientRpc(Vector3 position)
        {
            SetCharacterPosition(position);
            Physics.SyncTransforms();
            var netTransform = GetComponent<Unity.Netcode.Components.NetworkTransform>();
            if (netTransform != null && netTransform.CanCommitToTransform)
            {
                try { netTransform.Teleport(position, transform.rotation, transform.localScale); } catch { }
            }
            ResetMovementState();
            RemoveBurnEffect();
        }

        public void ResetStateForMatch(Vector3 position, Quaternion? rotation = null)
        {
            if (!IsServer) return;
            _isDead = false;
            _deathCount.Value = 0;

            SetCharacterPosition(position);
            if (rotation.HasValue) transform.rotation = rotation.Value;
            Physics.SyncTransforms();
            var netTransform = GetComponent<Unity.Netcode.Components.NetworkTransform>();
            if (netTransform != null && netTransform.CanCommitToTransform)
            {
                try { netTransform.Teleport(position, transform.rotation, transform.localScale); } catch { }
            }
            ResetMovementState();
            RemoveBurnEffect();

            _currentHp.Value = maxHp;

            // Ensure player starts match unarmed
            var weaponCtrl = GetComponent<WeaponController>();
            if (weaponCtrl != null) weaponCtrl.UnequipAll();

            // Clear shields and invisibility carried from warmup
            var shield = GetComponent<PlayerShield>();
            if (shield != null) shield.ResetShield();

            var invis = GetComponent<PlayerInvisibility>();
            if (invis != null) invis.ResetInvisibility();

            bool isMatchActive = (GameModeBase.Instance != null && (GameModeBase.Instance.MatchActive.Value || GameModeBase.Instance.IsInProgress))
                                 || (NetworkedGameManager.Instance != null && 
                                     (NetworkedGameManager.Instance.MatchStarted.Value || 
                                      (NetworkedGameManager.Instance.ActiveGameMode != null && (NetworkedGameManager.Instance.ActiveGameMode.MatchActive.Value || NetworkedGameManager.Instance.ActiveGameMode.IsInProgress))))
                                 || (NetworkedGameManager.Instance == null);

            if (isMatchActive)
            {
                StartInvincibilityServer(spawnInvincibilityDuration);
            }
            else
            {
                if (_invincibilityRoutine != null)
                {
                    StopCoroutine(_invincibilityRoutine);
                    _invincibilityRoutine = null;
                }
                _isInvincible = false;
                ShowInvincibilityAura(false);
            }

            ResetPlayerClientRpc(position, rotation.HasValue ? rotation.Value : transform.rotation, isMatchActive);
        }

        [ClientRpc]
        private void ResetPlayerClientRpc(Vector3 position, Quaternion rotation, bool grantInvincibility = false)
        {
            _isDead = false;
            SetCharacterPosition(position);
            transform.rotation = rotation;
            Physics.SyncTransforms();
            var netTransform = GetComponent<Unity.Netcode.Components.NetworkTransform>();
            if (netTransform != null && netTransform.CanCommitToTransform)
            {
                try { netTransform.Teleport(position, rotation, transform.localScale); } catch { }
            }

            SetPlayerControlsEnabled(true);
            SetPlayerVisibility(true);
            ResetMovementState();
            ResetLocomotionAnimator();
            RemoveBurnEffect();

            // Ensure player starts match unarmed on client
            var weaponCtrl = GetComponent<WeaponController>();
            if (weaponCtrl != null) weaponCtrl.UnequipAllLocal();

            var shield = GetComponent<PlayerShield>();
            if (shield != null && !IsServer) shield.ResetShield();

            var invis = GetComponent<PlayerInvisibility>();
            if (invis != null && !IsServer) invis.ResetInvisibility();

            if (grantInvincibility)
            {
                StartInvincibilityLocal(spawnInvincibilityDuration);
            }
            else
            {
                if (_invincibilityRoutine != null)
                {
                    StopCoroutine(_invincibilityRoutine);
                    _invincibilityRoutine = null;
                }
                _isInvincible = false;
                ShowInvincibilityAura(false);
            }
        }

        #endregion

        #region Status Effects Delegation

        public void ApplyFreezeEffect(float duration, float slowPercent, bool isDirectHit)
        {
            var shield = GetComponent<PlayerShield>();
            if (shield != null && (shield.IsShieldActive || shield.WasActiveOrAbsorbedRecently))
            {
                Debug.Log($"[PlayerHealth] Freeze effect blocked by active or recently-absorbed shield on {gameObject.name}");
                return;
            }

            if (_statusEffects == null) _statusEffects = GetComponent<PlayerStatusEffects>() ?? gameObject.AddComponent<PlayerStatusEffects>();
            _statusEffects.ApplyFreeze(duration, slowPercent, isDirectHit);
        }

        public void ApplyBurnEffect(float duration = 5.0f, float dps = 5.0f, WeaponSO weaponUsed = null)
        {
            var shield = GetComponent<PlayerShield>();
            if (shield != null && (shield.IsShieldActive || shield.WasActiveOrAbsorbedRecently))
            {
                Debug.Log($"[PlayerHealth] Burn effect blocked by active or recently-absorbed shield on {gameObject.name}");
                return;
            }

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
            var activeMode = NetworkedGameManager.Instance != null && NetworkedGameManager.Instance.ActiveGameMode != null 
                ? NetworkedGameManager.Instance.ActiveGameMode 
                : GameModeBase.Instance;

            if (activeMode is TDMMode tdm)
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
            bool wasEnabled = cc != null && cc.enabled;
            if (cc != null) cc.enabled = false;
            transform.position = position;
            if (wasEnabled && cc != null) cc.enabled = true;
        }

        private void SetCollidersEnabled(bool enabled)
        {
            var cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = enabled;

            var cols = GetComponentsInChildren<Collider>(true);
            foreach (var c in cols)
            {
                if (c != null && !(c is CharacterController))
                {
                    c.enabled = enabled;
                }
            }
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
            if (anim != null && anim.runtimeAnimatorController != null && anim.isActiveAndEnabled)
            {
                anim.ResetTrigger("Die");
                int locomotionHash = Animator.StringToHash("Locomotion");
                if (anim.layerCount > 0 && anim.HasState(0, locomotionHash))
                {
                    anim.CrossFade("Locomotion", 0.1f, 0);
                }
                if (anim.layerCount > 1)
                {
                    int emptyHash = Animator.StringToHash("Empty");
                    if (anim.HasState(1, emptyHash))
                    {
                        anim.CrossFade("Empty", 0.1f, 1);
                    }
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

            if (!enabled)
            {
                var weaponCtrl = GetComponent<WeaponController>();
                if (weaponCtrl != null) weaponCtrl.OnPlayerDied();
            }
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

            var nameplate = GetComponentInChildren<PlayerNameplate>(true);
            if (nameplate != null)
            {
                nameplate.SetDeathHidden(!visible);
            }
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
