using System.Collections;
using UnityEngine;
using Unity.Netcode;

namespace Shootopia
{
    /// <summary>
    /// Manages player status effects (Frost Freeze & Fiery Burn DoT)
    /// across the network using MaterialPropertyBlocks and NetworkBehaviours.
    /// </summary>
    public partial class PlayerStatusEffects : NetworkBehaviour
    {
        private CharacterMotor _motor;
        private Animator _animator;
        private PlayerHealth _health;

        private Coroutine _freezeRoutine;
        private Coroutine _burnRoutine;
        private bool _isFrozen = false;
        private bool _isBurning = false;

        public bool IsFrozen => _isFrozen;
        public bool IsBurning => _isBurning;

        private static MaterialPropertyBlock _statusMPB;
        private static MaterialPropertyBlock StatusMPB => _statusMPB ?? (_statusMPB = new MaterialPropertyBlock());

        private void Awake()
        {
            _motor = GetComponent<CharacterMotor>();
            _animator = GetComponentInChildren<Animator>(true);
            _health = GetComponent<PlayerHealth>();
        }

        #region Freeze Status Effect

        /// <summary>
        /// Applies freeze or slow status effect.
        /// isDirectHit = true: 100% complete freeze (MoveSpeedMultiplier = 0) and frozen animation pose.
        /// isDirectHit = false: Proportional speed slow based on distance to POC.
        /// </summary>
        public void ApplyFreeze(float duration, float slowPercent, bool isDirectHit)
        {
            var shield = GetComponent<PlayerShield>();
            if (shield != null && (shield.IsShieldActive || shield.WasActiveOrAbsorbedRecently))
            {
                Debug.Log($"[PlayerStatusEffects] Shield blocked freeze on {gameObject.name}");
                return;
            }

            ApplyFreezeRpc(duration, slowPercent, isDirectHit);
        }

        [Rpc(SendTo.Everyone)]
        private void ApplyFreezeRpc(float duration, float slowPercent, bool isDirectHit)
        {
            var shield = GetComponent<PlayerShield>();
            if (shield != null && (shield.IsShieldActive || shield.WasActiveOrAbsorbedRecently))
            {
                return;
            }

            if (_freezeRoutine != null) StopCoroutine(_freezeRoutine);
            _freezeRoutine = StartCoroutine(FreezeRoutine(duration, slowPercent, isDirectHit));
        }

        private IEnumerator FreezeRoutine(float duration, float slowPercent, bool isDirectHit)
        {
            _isFrozen = isDirectHit;

            if (_motor != null)
            {
                _motor.MoveSpeedMultiplier = isDirectHit ? 0f : Mathf.Clamp01(1f - slowPercent);
            }

            if (_animator != null)
            {
                _animator.speed = isDirectHit ? 0f : Mathf.Clamp(1f - slowPercent, 0.15f, 1f);
            }

            // Light translucent frost ice tint via MaterialPropertyBlock
            Color iceTint = new Color(0.55f, 0.85f, 1.0f, 0.65f);
            Color iceEmission = new Color(0.18f, 0.55f, 0.90f) * 1.5f;

            ApplyMaterialPropertyBlock(iceTint, iceEmission);

            yield return new WaitForSeconds(duration);

            ClearMaterialPropertyBlock();

            if (_motor != null) _motor.MoveSpeedMultiplier = 1f;
            if (_animator != null) _animator.speed = 1f;

            _isFrozen = false;
            _freezeRoutine = null;
        }

        #endregion

        public void ClearAllEffects()
        {
            if (IsSpawned)
            {
                ClearAllEffectsRpc();
            }
            else
            {
                ClearAllEffectsInternal();
            }
        }

        [Rpc(SendTo.Everyone)]
        private void ClearAllEffectsRpc()
        {
            ClearAllEffectsInternal();
        }

        private void ClearAllEffectsInternal()
        {
            if (_freezeRoutine != null)
            {
                StopCoroutine(_freezeRoutine);
                _freezeRoutine = null;
            }
            if (_burnRoutine != null)
            {
                StopCoroutine(_burnRoutine);
                _burnRoutine = null;
            }
            _isFrozen = false;
            _isBurning = false;
            ClearMaterialPropertyBlock();
            if (_motor != null) _motor.MoveSpeedMultiplier = 1f;
            if (_animator != null) _animator.speed = 1f;
        }

        #region Helper Methods

        private void ApplyMaterialPropertyBlock(Color baseColor, Color emissionColor)
        {
            MaterialPropertyBlock mpb = StatusMPB;
            mpb.Clear();
            mpb.SetColor("_BaseColor", baseColor);
            mpb.SetColor("_Color", baseColor);
            mpb.SetColor("_EmissionColor", emissionColor);

            var renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r != null && !(r is ParticleSystemRenderer))
                {
                    r.SetPropertyBlock(mpb);
                }
            }
        }

        public void ClearMaterialPropertyBlock()
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r != null && !(r is ParticleSystemRenderer))
                {
                    r.SetPropertyBlock(null);
                }
            }
        }

        #endregion
    }
}
