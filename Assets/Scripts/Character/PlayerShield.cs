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

        public bool IsShieldActive => _isShieldActive.Value;
        public float CurrentShieldHp => _currentShieldHp.Value;
        public float MaxShieldHp => maxShieldHp;

        private GameObject _shieldInstance;
        private PlayerInputRouter _input;
        private float _shieldTimer = 0f;

        private void Awake()
        {
            _input = GetComponent<PlayerInputRouter>();
            if (shieldSpherePrefab == null)
            {
                shieldSpherePrefab = Resources.Load<GameObject>("PlayerShieldSphere");
                if (shieldSpherePrefab == null)
                {
#if UNITY_EDITOR
                    shieldSpherePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/PlayerShieldSphere.prefab");
#endif
                }
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

                // Ensure Layer 2 (Ignore Raycast) so Cinemachine camera raycasts never zoom in
                int ignoreRaycastLayer = 2;
                _shieldInstance.layer = ignoreRaycastLayer;
                foreach (Transform child in _shieldInstance.GetComponentsInChildren<Transform>(true))
                {
                    child.gameObject.layer = ignoreRaycastLayer;
                }

                // Add physical damage receiver to forcefield surface
                var receiver = _shieldInstance.GetComponent<ShieldColliderReceiver>();
                if (receiver == null) receiver = _shieldInstance.AddComponent<ShieldColliderReceiver>();
                receiver.Initialize(this);

                var shieldCol = _shieldInstance.GetComponent<SphereCollider>();
                if (shieldCol != null)
                {
                    shieldCol.isTrigger = false; // Rigid solid physical forcefield sphere!

                    // Ignore collision with owner player so owner moves freely inside
                    var playerCols = GetComponentsInChildren<Collider>(true);
                    foreach (var pCol in playerCols)
                    {
                        if (pCol != shieldCol)
                        {
                            Physics.IgnoreCollision(pCol, shieldCol, true);
                        }
                    }

                    var cc = GetComponent<CharacterController>();
                    if (cc != null)
                    {
                        Physics.IgnoreCollision(cc, shieldCol, true);
                    }
                }

                _shieldInstance.SetActive(_isShieldActive.Value);
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
            GrantShieldServerRpc(customShieldHp);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void GrantShieldServerRpc(float customShieldHp = 50f, RpcParams rpcParams = default)
        {
            if (!IsServer) return;
            _currentShieldHp.Value = customShieldHp;
            _shieldTimer = 0f;
            _isShieldActive.Value = true;
            Debug.Log($"[PlayerShield] Custom shield granted for Player {OwnerClientId} ({customShieldHp} HP, 15s duration)");
        }

        private void ActivateShieldInternal()
        {
            _currentShieldHp.Value = maxShieldHp;
            _shieldTimer = 0f;
            _isShieldActive.Value = true;
            Debug.Log($"[PlayerShield] Shield activated for Player {OwnerClientId} ({maxShieldHp} HP, {maxShieldDuration}s duration)");
        }

        private void DeactivateShieldInternal()
        {
            _isShieldActive.Value = false;
            _shieldTimer = 0f;
            Debug.Log($"[PlayerShield] Shield deactivated/shattered for Player {OwnerClientId}");
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
            TakeDamageServerRpc(amount);
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
            if (!_isShieldActive.Value) return incomingDamage;

            return AbsorbDamageInternal(incomingDamage);
        }

        private float AbsorbDamageInternal(float incomingDamage)
        {
            float shieldHp = _currentShieldHp.Value;

            if (IsServer)
            {
                // Shield absorbs incoming damage. Even if hit is a lethal nuke (incomingDamage > shieldHp),
                // the shield absorbs the entire hit, shatters, and lets ZERO overflow damage through to health!
                _currentShieldHp.Value = Mathf.Max(0f, shieldHp - incomingDamage);
                Debug.Log($"[PlayerShield] Server absorbed {incomingDamage} damage for Player {OwnerClientId}. Remaining Shield HP: {_currentShieldHp.Value}");

                if (_currentShieldHp.Value <= 0f)
                {
                    DeactivateShieldInternal();
                }
            }

            // Shield Gate / One-shot protection:
            // Always returns 0 overflow damage while the shield was active so a complete hit (even a nuke) is absorbed!
            return 0f;
        }
    }
}
