using System.Collections;
using UnityEngine;
using Unity.Netcode;

namespace Shootopia
{
    public partial class PlayerStatusEffects
    {
        #region Burn Status Effect

        /// <summary>
        /// Applies burning status effect with passive Damage-Over-Time (DoT) and fiery visual tint.
        /// </summary>
        public void ApplyBurn(float duration = 5.0f, float dps = 5.0f, WeaponSO weaponUsed = null)
        {
            if (_health != null && _health.IsInvincible) return;

            var shield = GetComponent<PlayerShield>();
            if (shield != null && (shield.IsShieldActive || shield.WasActiveOrAbsorbedRecently))
            {
                Debug.Log($"[PlayerStatusEffects] Shield blocked burn on {gameObject.name}");
                return;
            }

            ApplyBurnRpc(duration, dps);
        }

        public void RemoveBurn()
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
            ClearMaterialPropertyBlock();
        }

        [Rpc(SendTo.Everyone)]
        private void ApplyBurnRpc(float duration, float dps)
        {
            if (_health != null && _health.IsInvincible) return;

            var shield = GetComponent<PlayerShield>();
            if (shield != null && (shield.IsShieldActive || shield.WasActiveOrAbsorbedRecently))
            {
                return;
            }

            if (_burnRoutine != null) StopCoroutine(_burnRoutine);
            _burnRoutine = StartCoroutine(BurnRoutine(duration, dps));
        }

        private IEnumerator BurnRoutine(float duration, float dps)
        {
            _isBurning = true;

            // Fiery orange/red burn tint via MaterialPropertyBlock
            Color burnTint = new Color(1.0f, 0.35f, 0.05f, 0.9f);
            Color burnEmission = new Color(2.0f, 0.5f, 0.05f) * 1.5f;

            ApplyMaterialPropertyBlock(burnTint, burnEmission);

            float elapsed = 0f;
            float tickInterval = 0.5f;
            float damagePerTick = dps * tickInterval;

            while (elapsed < duration)
            {
                yield return new WaitForSeconds(tickInterval);
                elapsed += tickInterval;

                if (_health != null && _health.IsDead) break;

                if (IsServer && _health != null && damagePerTick > 0f)
                {
                    ulong burnAttacker = _health.LastAttackerClientId;
                    _health.TakeDamage(damagePerTick, DamageType.Projectile, null, burnAttacker);
                }
            }

            ClearMaterialPropertyBlock();
            _isBurning = false;
            _burnRoutine = null;
        }

        #endregion
    }
}

