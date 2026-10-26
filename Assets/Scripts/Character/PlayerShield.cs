using System.Collections;
using UnityEngine;
using Unity.Netcode;

namespace Shootopia
{
    /// <summary>
    /// Networked Force Field Shield Manager.
    /// Surrounds the player with a rigid semi-transparent blue/purple forcefield sphere that moves with the player.
    /// Absorbs up to 100 HP of incoming damage and lasts up to 15 seconds.
    /// Prevents enemy players and incoming bullets/projectiles from entering the forcefield sphere.
    /// Grants immunity to Void Black Hole pull while active.
    /// Activated via Y Key (testing input) or item loadout.
    /// </summary>
    public class PlayerShield : NetworkBehaviour
    {
        [Header("Shield Configuration")]
        [SerializeField] private float maxShieldHp = 50f;
        [SerializeField] private float maxShieldDuration = 15f;
        [SerializeField] private GameObject shieldSpherePrefab;

        [Header("Audio & Juice")]
        [SerializeField] private AudioClip shieldActivateSound;
        [SerializeField] private AudioClip shieldBreakSound;

        [Header("Networked State")]
        private readonly NetworkVariable<bool> _isShieldActive = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<float> _currentShieldHp = new NetworkVariable<float>(50f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<float> _shieldTimeRemaining = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private bool _localShieldActive = false;
        private float _localShieldHp = 50f;
        private int _lastDamageAbsorptionFrame = -100;
        private float _lastDamageAbsorptionTime = -100f;

        public bool IsShieldActive => IsSpawned ? _isShieldActive.Value : _localShieldActive;
        public float CurrentShieldHp => IsSpawned ? _currentShieldHp.Value : _localShieldHp;
        public float MaxShieldHp => maxShieldHp;
        public float ShieldTimeRemaining => IsSpawned ? _shieldTimeRemaining.Value : Mathf.Max(0f, maxShieldDuration - _shieldTimer);
        public float MaxShieldDuration => maxShieldDuration;

        /// <summary>
        /// True if shield is actively up, OR if the shield absorbed damage / shattered within the current or previous frame (or within 0.35s).
        /// Used to block status effects (Freeze, Burn, Stun) from leaking through when an attack hits or breaks the shield.
        /// </summary>
        public bool WasActiveOrAbsorbedRecently => IsShieldActive || (Time.frameCount - _lastDamageAbsorptionFrame <= 2) || (Time.time - _lastDamageAbsorptionTime <= 0.35f);

        private GameObject _shieldInstance;
        private PlayerInputRouter _input;
        private float _shieldTimer = 0f;

        private void Awake()
        {
            _input = GetComponent<PlayerInputRouter>();
        }

        private void Start()
        {
            if (!IsSpawned)
            {
                SetupShieldInstance();
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _isShieldActive.OnValueChanged += OnShieldStateChanged;

            SetupShieldInstance();

            if (IsServer)
            {
                _isShieldActive.Value = false;
                _currentShieldHp.Value = maxShieldHp;
            }
        }

        private void SetupShieldInstance()
        {
            if (shieldSpherePrefab != null && _shieldInstance == null)
            {
                _shieldInstance = Instantiate(shieldSpherePrefab, transform);
                _shieldInstance.transform.localPosition = Vector3.up * 1.0f;
                _shieldInstance.transform.localRotation = Quaternion.identity;

                // Set Layer to Player (3) and tag Player so Cinemachine avoids colliding with it,
                // while weapon raycasts and melee spherecasts can detect and damage the shield.
                int playerLayer = LayerMask.NameToLayer("Player");
                if (playerLayer < 0) playerLayer = 3;
                _shieldInstance.layer = playerLayer;
                _shieldInstance.tag = "Player";
                foreach (Transform child in _shieldInstance.GetComponentsInChildren<Transform>(true))
                {
                    child.gameObject.layer = playerLayer;
                    child.gameObject.tag = "Player";
                }

                // Add physical damage receiver to forcefield surface
                var receiver = _shieldInstance.GetComponent<ShieldColliderReceiver>();
                if (receiver == null) receiver = _shieldInstance.AddComponent<ShieldColliderReceiver>();
                receiver.Initialize(this);

                var shieldCol = _shieldInstance.GetComponent<SphereCollider>();
                if (shieldCol != null)
                {
                    shieldCol.isTrigger = false; // Rigid solid physical forcefield sphere for projectiles/raycasts
                    IgnoreCharacterControllers(shieldCol);
                }

                _shieldInstance.SetActive(_isShieldActive.Value);
            }
        }

        private void IgnoreCharacterControllers(Collider shieldCol)
        {
            if (shieldCol == null) return;

            // 1. Ignore all colliders (including CharacterController) on owner
            var ownerCols = GetComponentsInChildren<Collider>(true);
            foreach (var col in ownerCols)
            {
                if (col != null && col != shieldCol)
                {
                    Physics.IgnoreCollision(col, shieldCol, true);
                }
            }

            // 2. Ignore CharacterControllers of all active players so shield never pushes opponents away
            foreach (var player in PlayerHealth.ActivePlayers)
            {
                if (player == null) continue;
                var cc = player.GetComponent<CharacterController>();
                if (cc != null && cc != shieldCol)
                {
                    Physics.IgnoreCollision(cc, shieldCol, true);
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            _isShieldActive.OnValueChanged -= OnShieldStateChanged;
            if (_shieldInstance != null)
            {
                Destroy(_shieldInstance);
            }
        }

        private void Update()
        {
            // Local owner Y key testing trigger
            if (IsOwner && _input != null && _input.Intent.ToggleShield)
            {
                ToggleShieldServerRpc();
            }

            // Duration timer tracking on server
            if (IsServer && _isShieldActive.Value)
            {
                _shieldTimer += Time.deltaTime;
                _shieldTimeRemaining.Value = Mathf.Max(0f, maxShieldDuration - _shieldTimer);
                if (_shieldTimer >= maxShieldDuration || _currentShieldHp.Value <= 0f)
                {
                    DeactivateShieldInternal();
                }
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void ToggleShieldServerRpc(RpcParams rpcParams = default)
        {
            if (!IsServer) return;
            if (_isShieldActive.Value)
            {
                DeactivateShieldInternal();
            }
            else
            {
                ActivateShieldInternal();
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void ActivateShieldServerRpc(RpcParams rpcParams = default)
        {
            if (!IsServer) return;
            ActivateShieldInternal();
        }

        public void GrantShield(float customShieldHp = 50f)
        {
            if (IsSpawned)
            {
                GrantShieldServerRpc(customShieldHp);
            }
            else
            {
                _localShieldHp = customShieldHp;
                _shieldTimer = 0f;
                _localShieldActive = true;
                if (_shieldInstance == null) SetupShieldInstance();
                if (_shieldInstance != null) _shieldInstance.SetActive(true);
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void GrantShieldServerRpc(float customShieldHp = 50f, RpcParams rpcParams = default)
        {
            if (!IsServer) return;
            _currentShieldHp.Value = customShieldHp;
            _shieldTimer = 0f;
            _shieldTimeRemaining.Value = maxShieldDuration;
            _isShieldActive.Value = true;
            GetComponent<PlayerStatusEffects>()?.ClearAllEffects();
            Debug.Log($"[PlayerShield] Custom shield granted for Player {OwnerClientId} ({customShieldHp} HP, 15s duration)");
        }

        private void ActivateShieldInternal()
        {
            _currentShieldHp.Value = maxShieldHp;
            _shieldTimer = 0f;
            _shieldTimeRemaining.Value = maxShieldDuration;
            _isShieldActive.Value = true;
            GetComponent<PlayerStatusEffects>()?.ClearAllEffects();
            Debug.Log($"[PlayerShield] Shield activated for Player {OwnerClientId} ({maxShieldHp} HP, {maxShieldDuration}s duration)");
        }

        private void DeactivateShieldInternal()
        {
            _lastDamageAbsorptionFrame = Time.frameCount;
            _lastDamageAbsorptionTime = Time.time;
            _isShieldActive.Value = false;
            _shieldTimer = 0f;
            _shieldTimeRemaining.Value = 0f;
            Debug.Log($"[PlayerShield] Shield deactivated/shattered for Player {OwnerClientId}");
        }

        public void ResetShield()
        {
            if (IsServer)
            {
                DeactivateShieldInternal();
            }
            else
            {
                _localShieldActive = false;
                if (_shieldInstance != null)
                {
                    _shieldInstance.SetActive(false);
                }
            }
        }

        private void OnShieldStateChanged(bool previousValue, bool newValue)
        {
            if (_shieldInstance == null) SetupShieldInstance();

            if (_shieldInstance != null)
            {
                _shieldInstance.SetActive(newValue);
            }

            if (newValue)
            {
                if (_shieldInstance != null)
                {
                    IgnoreCharacterControllers(_shieldInstance.GetComponent<SphereCollider>());
                }
                if (shieldActivateSound != null) AudioSource.PlayClipAtPoint(shieldActivateSound, transform.position, 0.8f);
            }
            else if (previousValue && !newValue)
            {
                if (shieldBreakSound != null) AudioSource.PlayClipAtPoint(shieldBreakSound, transform.position, 0.9f);
            }
        }

        /// <summary>
        /// Public entry point for damage hits on the shield collider.
        /// Sends a ServerRpc so damage absorption and durability reduction are calculated authoritatively on the server.
        /// </summary>
        public void TakeDamage(float amount)
        {
            if (IsSpawned)
            {
                TakeDamageServerRpc(amount);
            }
            else if (_localShieldActive)
            {
                AbsorbDamageInternal(amount);
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void TakeDamageServerRpc(float amount, RpcParams rpcParams = default)
        {
            if (!IsServer || !_isShieldActive.Value) return;

            AbsorbDamageInternal(amount);
        }

        /// <summary>
        /// Called on the server during PlayerHealth.TakeDamage or TakeDamageServerRpc.
        /// Absorbs incoming damage up to remaining shield HP and returns overflow damage.
        /// </summary>
        public float AbsorbDamage(float incomingDamage)
        {
            if (!IsShieldActive) return incomingDamage;

            return AbsorbDamageInternal(incomingDamage);
        }

        private float AbsorbDamageInternal(float incomingDamage)
        {
            _lastDamageAbsorptionFrame = Time.frameCount;
            _lastDamageAbsorptionTime = Time.time;

            float shieldHp = CurrentShieldHp;
            float newShieldHp = Mathf.Max(0f, shieldHp - incomingDamage);

            if (IsSpawned)
            {
                if (IsServer)
                {
                    _currentShieldHp.Value = newShieldHp;
                    Debug.Log($"[PlayerShield] Server absorbed {incomingDamage} damage for Player {OwnerClientId}. Remaining Shield HP: {_currentShieldHp.Value}");

                    if (_currentShieldHp.Value <= 0f)
                    {
                        DeactivateShieldInternal();
                    }
                }
            }
            else
            {
                _localShieldHp = newShieldHp;
                Debug.Log($"[PlayerShield] Offline absorbed {incomingDamage} damage. Remaining Shield HP: {_localShieldHp}");
                if (_localShieldHp <= 0f)
                {
                    _localShieldActive = false;
                    if (_shieldInstance != null) _shieldInstance.SetActive(false);
                }
            }

            // Shield Gate / One-shot protection:
            // Always returns 0 overflow damage while the shield was active so a complete hit (even a nuke) is absorbed!
            return 0f;
        }
    }
}
