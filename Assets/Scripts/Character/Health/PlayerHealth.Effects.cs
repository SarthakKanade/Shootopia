using UnityEngine;

namespace Shootopia
{
    public partial class PlayerHealth
    {

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



        private void SetCharacterPosition(Vector3 position)
        {
            var cc = GetComponent<CharacterController>();
            bool wasEnabled = cc != null && cc.enabled && IsOwner;
            if (cc != null) cc.enabled = false;
            transform.position = position;
            if (wasEnabled && cc != null) cc.enabled = true;
        }

        private void SetCollidersEnabled(bool enabled)
        {
            var cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = enabled && IsOwner;

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

    }
}
