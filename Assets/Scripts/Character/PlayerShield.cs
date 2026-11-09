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
    public partial class PlayerShield : NetworkBehaviour
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

    }
}
