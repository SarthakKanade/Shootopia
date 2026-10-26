using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Shootopia
{
    /// <summary>
    /// Networked Invisibility Cloak Component.
    /// Turns the player invisible to opponents and teammates for a set duration.
    /// - Weapons in hand remain visible as they are.
    /// - Player still receives full damage and can die.
    /// - Local player body appears translucent with a stealth cyan glow for visual feedback.
    /// - Remote clients have the player's body renderers and overhead nameplates hidden.
    /// </summary>
    public class PlayerInvisibility : NetworkBehaviour
    {
        [Header("Invisibility Configuration")]
        [SerializeField] private float defaultDuration = 10f;
        [SerializeField] private Material stealthTranslucentMaterial;

        [Header("Audio")]
        [SerializeField] private AudioClip stealthActivateClip;
        [SerializeField] private AudioClip stealthDeactivateClip;

        [Header("Network State")]
        private readonly NetworkVariable<bool> _isInvisible = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<float> _invisibilityTimeRemaining = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public bool IsInvisible => _isInvisible.Value;
        public float Duration => defaultDuration;
        public float InvisibilityTimeRemaining => IsSpawned ? _invisibilityTimeRemaining.Value : Mathf.Max(0f, _activeDuration - _durationTimer);
        public float InvisibilityTotalDuration => _activeDuration;

        private float _activeDuration = 10f;
        private float _durationTimer = 0f;

        private WeaponController _weaponController;
        private PlayerHealth _playerHealth;
        private PlayerNameplate _nameplate;

        // Cache original materials for local player translucent effect restoration
        private readonly Dictionary<Renderer, Material[]> _originalMaterials = new Dictionary<Renderer, Material[]>();

        private void Awake()
        {
            _weaponController = GetComponent<WeaponController>();
            _playerHealth = GetComponent<PlayerHealth>();
            _nameplate = GetComponentInChildren<PlayerNameplate>(true);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _isInvisible.OnValueChanged += OnInvisibilityStateChanged;

            if (_playerHealth != null)
            {
                _playerHealth.CurrentHp.OnValueChanged += OnHpChanged;
            }

            if (IsServer)
            {
                _isInvisible.Value = false;
                _durationTimer = 0f;
            }

            // Sync visual state if joining or spawned mid-effect
            if (_isInvisible.Value)
            {
                ApplyInvisibilityVisuals(true);
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            _isInvisible.OnValueChanged -= OnInvisibilityStateChanged;

            if (_playerHealth != null)
            {
                _playerHealth.CurrentHp.OnValueChanged -= OnHpChanged;
            }

            RestoreNormalVisuals();
        }

        private void Update()
        {
            if (!IsSpawned)
            {
                if (_durationTimer < _activeDuration && _activeDuration > 0f)
                {
                    _durationTimer += Time.deltaTime;
                    if (_durationTimer >= _activeDuration)
                    {
                        ApplyInvisibilityVisuals(false);
                    }
                }
                return;
            }

            if (!IsServer) return;

            if (_isInvisible.Value)
            {
                if (_playerHealth != null && _playerHealth.IsDead)
                {
                    DeactivateInvisibilityInternal();
                    return;
                }

                _durationTimer += Time.deltaTime;
                _invisibilityTimeRemaining.Value = Mathf.Max(0f, _activeDuration - _durationTimer);
                if (_durationTimer >= _activeDuration)
                {
                    DeactivateInvisibilityInternal();
                }
            }
        }

        private void OnHpChanged(float previousHp, float newHp)
        {
            if (IsServer && newHp <= 0f && _isInvisible.Value)
            {
                DeactivateInvisibilityInternal();
            }
        }

        public void ActivateInvisibility(float duration = 10f)
        {
            if (duration <= 0f) duration = defaultDuration;
            if (IsSpawned)
            {
                ActivateInvisibilityServerRpc(duration);
            }
            else
            {
                _activeDuration = duration > 0f ? duration : defaultDuration;
                _durationTimer = 0f;
                ApplyInvisibilityVisuals(true);
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void ActivateInvisibilityServerRpc(float duration = 10f, RpcParams rpcParams = default)
        {
            if (!IsServer) return;

            _activeDuration = duration > 0f ? duration : defaultDuration;
            _durationTimer = 0f;
            _invisibilityTimeRemaining.Value = _activeDuration;
            _isInvisible.Value = true;
            Debug.Log($"[PlayerInvisibility] Activated for Player {OwnerClientId} (duration: {_activeDuration}s)");
        }

        private void DeactivateInvisibilityInternal()
        {
            if (!IsServer) return;
            _isInvisible.Value = false;
            _durationTimer = 0f;
            _invisibilityTimeRemaining.Value = 0f;
            Debug.Log($"[PlayerInvisibility] Deactivated for Player {OwnerClientId}");
        }

        public void ResetInvisibility()
        {
            if (IsServer)
            {
                DeactivateInvisibilityInternal();
            }
            else
            {
                ApplyInvisibilityVisuals(false);
            }
        }

        private void OnInvisibilityStateChanged(bool previousValue, bool newValue)
        {
            ApplyInvisibilityVisuals(newValue);

            if (newValue && stealthActivateClip != null)
            {
                AudioSource.PlayClipAtPoint(stealthActivateClip, transform.position, 0.8f);
            }
            else if (!newValue && previousValue && stealthDeactivateClip != null)
            {
                AudioSource.PlayClipAtPoint(stealthDeactivateClip, transform.position, 0.8f);
            }
        }

        private void ApplyInvisibilityVisuals(bool invisible)
        {
            if (_nameplate == null)
            {
                _nameplate = GetComponentInChildren<PlayerNameplate>(true);
            }

            var bodyRenderers = GetBodyRenderers();

            if (invisible)
            {
                if (IsOwner)
                {
                    // Local player visual feedback: keep renderers enabled, swap to translucent stealth material
                    foreach (var r in bodyRenderers)
                    {
                        if (r == null) continue;
                        if (!_originalMaterials.ContainsKey(r))
                        {
                            _originalMaterials[r] = r.sharedMaterials;
                        }

                        if (stealthTranslucentMaterial != null)
                        {
                            int matCount = r.sharedMaterials.Length;
                            Material[] stealthMats = new Material[matCount];
                            for (int i = 0; i < matCount; i++)
                            {
                                stealthMats[i] = stealthTranslucentMaterial;
                            }
                            r.sharedMaterials = stealthMats;
                        }
                        r.enabled = true;
                    }
                }
                else
                {
                    // Remote clients: hide player body renderers completely
                    foreach (var r in bodyRenderers)
                    {
                        if (r != null) r.enabled = false;
                    }

                    // Hide overhead nameplate
                    if (_nameplate != null)
                    {
                        _nameplate.SetInvisibilityHidden(true);
                    }
                }
            }
            else
            {
                RestoreNormalVisuals();
            }
        }

        private void RestoreNormalVisuals()
        {
            var bodyRenderers = GetBodyRenderers();

            if (IsOwner)
            {
                foreach (var r in bodyRenderers)
                {
                    if (r == null) continue;
                    if (_originalMaterials.TryGetValue(r, out var origMats) && origMats != null)
                    {
                        r.sharedMaterials = origMats;
                    }
                    r.enabled = true;
                }
                _originalMaterials.Clear();
            }
            else
            {
                foreach (var r in bodyRenderers)
                {
                    if (r != null) r.enabled = true;
                }

                if (_nameplate != null)
                {
                    _nameplate.SetInvisibilityHidden(false);
                }
            }
        }

        /// <summary>
        /// Collects all character body renderers, strictly excluding:
        /// - Weapon socket children (so held weapons remain visible!)
        /// - UI canvases
        /// - Shield forcefield spheres
        /// </summary>
        private List<Renderer> _cachedBodyRenderers;

        private List<Renderer> GetBodyRenderers()
        {
            if (_cachedBodyRenderers != null && _cachedBodyRenderers.Count > 0)
            {
                return _cachedBodyRenderers;
            }

            _cachedBodyRenderers = new List<Renderer>();

            Transform weaponSocket = _weaponController != null ? _weaponController.WeaponSocket : null;
            if (weaponSocket == null)
            {
                var socketObj = transform.Find("WeaponSocket_R") ?? transform.Find("weaponSocket");
                if (socketObj != null) weaponSocket = socketObj;
            }
            var allRenderers = GetComponentsInChildren<Renderer>(true);

            foreach (var r in allRenderers)
            {
                if (r == null) continue;

                // Keep weapons in hand visible at all times
                if (weaponSocket != null && r.transform.IsChildOf(weaponSocket)) continue;

                // Exclude UI Canvas renderers (e.g. overhead health bars)
                if (r.GetComponentInParent<Canvas>() != null) continue;

                // Exclude Shield sphere renderers
                if (r.name.Contains("Shield") || (r.transform.parent != null && r.transform.parent.name.Contains("Shield"))) continue;

                _cachedBodyRenderers.Add(r);
            }

            return _cachedBodyRenderers;
        }

        private void OnDisable()
        {
            RestoreNormalVisuals();
        }
    }
}
