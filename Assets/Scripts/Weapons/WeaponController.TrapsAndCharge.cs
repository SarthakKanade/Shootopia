using System.Collections;
using UnityEngine;

namespace Shootopia
{
    public partial class WeaponController
    {
        private void HandleTrapFire(WeaponSO data, bool fireHeld, bool fireReleased)
        {
            if (_inTrapCooldown) { if (_isAimingTrap) CancelTrapAim(); return; }

            if (fireHeld && CanFire(data) && _timeSinceLastShot >= data.FireRate)
            {
                if (!_isAimingTrap)
                {
                    StartTrapAimLocal();
                    if (IsSpawned) StartTrapAimRpc();
                }
                if (_trajectoryPredictor != null)
                {
                    GetTrapLaunchParameters(data, out Vector3 launchOrigin, out Vector3 launchVel);
                    _trajectoryPredictor.UpdateTrajectory(launchOrigin, launchVel);
                }
            }
            else if (fireReleased && _isAimingTrap)
            {
                ExecuteTrapThrowLocal();
                if (IsSpawned) ExecuteTrapThrowRpc();

                GetTrapLaunchParameters(data, out Vector3 trapOrigin, out Vector3 trapVel);
                FireProjectile(data, trapOrigin, trapVel);

                if (_trapCooldownRoutine != null) StopCoroutine(_trapCooldownRoutine);
                _trapCooldownRoutine = StartCoroutine(TrapCooldownRoutine());
            }
            else if (!fireHeld && _isAimingTrap) CancelTrapAim();
        }

        private void StartTrapAimLocal()
        {
            _isAimingTrap = true;
            int upperBody = GetUpperBodyLayer();
            SafeSetBool(HashIsThrowAiming, true);
            SafeSetFloat(HashShootSpeed, 1f);
            SafeCrossFade("ThrowBall_Prep", 0.05f, upperBody, 0f);
            if (motor != null && IsOwner) motor.MoveSpeedMultiplier = 0.25f;
        }

        private void HandleChargedFire(WeaponSO data, float dt, bool fireHeld, bool fireReleased)
        {
            if (fireHeld && CanFire(data) && _timeSinceLastShot >= data.FireRate)
            {
                if (!_chargeHandler.IsCharging) StartCharge(data);
                else _chargeHandler.UpdateCharge(dt, data);
            }
            else if (fireReleased && _chargeHandler.IsCharging)
            {
                if (_chargeHandler.IsFullyCharged(data))
                {
                    _aimHandler.TriggerCombatStance();
                    if (AudioManager.Instance != null) AudioManager.Instance.TriggerCombatDucking(0.32f, 1.3f, true);
                    FireProjectile(data);
                    if (animator != null) { animator.SetFloat(HashShootSpeed, 1f); animator.Play("Shoot", 1, 0f); }
                    CancelCharge(exitCombatStance: false);
                }
                else CancelCharge(exitCombatStance: true);
            }
            else if (!fireHeld && _chargeHandler.IsCharging) CancelCharge(exitCombatStance: true);
        }

public void GetTrapLaunchParameters(WeaponSO data, out Vector3 launchOrigin, out Vector3 launchVel)
        {
            float angle = data != null && data.throwAngle > 0f ? data.throwAngle : 20f;
            float force = data != null && data.throwForce > 0f ? data.throwForce : 14f;
            Transform camTrans = Camera.main != null ? Camera.main.transform : transform;
            launchOrigin = transform.position + Vector3.up * 1.5f + camTrans.forward * 0.5f;
            launchVel = (Quaternion.AngleAxis(-angle, camTrans.right) * camTrans.forward) * force;
        }

        private void CancelTrapAim()
        {
            if (!_isAimingTrap) return;
            CancelTrapAimLocal();
            if (IsSpawned) CancelTrapAimRpc();
        }

        private void CancelTrapAimLocal()
        {
            if (!_isAimingTrap) return;
            _isAimingTrap = false;
            SafeSetBool(HashIsThrowAiming, false);
            SafeSetFloat(HashShootSpeed, 1f);
            SafeCrossFade("Empty", 0.15f, GetUpperBodyLayer());
            if (motor != null && IsOwner) motor.MoveSpeedMultiplier = 1.0f;
            if (_trajectoryPredictor != null) _trajectoryPredictor.HideTrajectory();
        }

        private void ExecuteTrapThrowLocal()
        {
            _isAimingTrap = false;
            int upperBody = GetUpperBodyLayer();
            SafeSetBool(HashIsThrowAiming, false);
            SafeSetTrigger(HashThrowRelease);
            SafeCrossFade("ThrowBall_Release", 0.02f, upperBody, 0f);
            if (motor != null && IsOwner) motor.MoveSpeedMultiplier = 1.0f;
            if (_trajectoryPredictor != null) _trajectoryPredictor.HideTrajectory();
            if (_currentVisual != null) _currentVisual.SetActive(false);
            StartCoroutine(ResetAnimationToIdleRoutine());
        }

private void StartCharge(WeaponSO data)
        {
            StartChargeLocal();
            if (IsSpawned) StartChargeRpc();
        }

        private void StartChargeLocal()
        {
            _chargeHandler.StartCharge(Muzzle, CurrentAttach?.gameplayData);
            if (motor != null) motor.IsInCombat = true;
            if (animator != null)
            {
                animator.SetBool(HashIsFiring, true);
                animator.SetFloat(HashShootSpeed, 0f);
                if (!animator.GetCurrentAnimatorStateInfo(1).IsName("Shoot")) animator.Play("Shoot", 1, 0f);
            }
        }

        private void CancelCharge(bool exitCombatStance = true)
        {
            CancelChargeLocal(exitCombatStance);
            if (IsSpawned) CancelChargeRpc(exitCombatStance);
        }

        private void CancelChargeLocal(bool exitCombatStance = true)
        {
            _chargeHandler?.CancelCharge();
            if (_isAimingTrap) CancelTrapAim();
            if (exitCombatStance) _aimHandler?.ExitCombatStance();
            SafeSetBool(HashIsThrowAiming, false);
        }

        private IEnumerator ResetAnimationToIdleRoutine()
        {
            yield return new WaitForSeconds(0.35f);
            if (animator != null)
            {
                animator.SetFloat(HashShootSpeed, 1f);
                SafeCrossFade("Locomotion", 0.15f, 0);
                SafeCrossFade("Empty", 0.15f, GetUpperBodyLayer());
            }
            if (_currentVisual != null && CurrentAttach != null && !string.IsNullOrEmpty(CurrentAttach.name))
            {
                _currentVisual.SetActive(true);
            }
        }
    }
}
