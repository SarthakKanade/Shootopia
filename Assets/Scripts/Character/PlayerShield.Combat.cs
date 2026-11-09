using UnityEngine;
using Unity.Netcode;

namespace Shootopia
{
    public partial class PlayerShield
    {
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
