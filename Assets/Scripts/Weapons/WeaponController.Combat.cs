using System.Collections;
using UnityEngine;

namespace Shootopia
{
    public partial class WeaponController
    {
        private void HandleFire(float dt)
        {
            bool fireHeld = input.Intent.Fire;
            bool firePressed = fireHeld && !_lastFire;
            bool fireReleased = !fireHeld && _lastFire;
            _lastFire = fireHeld;

            if (!IsArmed)
            {
                if (firePressed && _timeSinceLastShot >= 0.4f)
                {
                    _timeSinceLastShot = 0f;
                    if (animator != null)
                    {
                        animator.SetFloat(HashStance, 2.0f);
                        animator.Play("Shoot", 1, 0f);
                        StartCoroutine(TriggerMeleeAnimation(0.60f));
                    }
                    StartCoroutine(DelayedMeleeHitRoutine(0.45f, 25f, null));
                }
                return;
            }

            var data = CurrentAttach?.gameplayData;
            if (data == null) return;

            if (CurrentAttach.stanceId == 3 || data.category == WeaponCategory.Melee || data.name.Contains("Knife"))
            {
                if (firePressed && CanFire(data) && _timeSinceLastShot >= data.FireRate)
                {
                    _timeSinceLastShot = 0f;
                    if (animator != null)
                    {
                        animator.SetFloat(HashStance, 3f);
                        animator.Play("Shoot", 1, 0f);
                        StartCoroutine(TriggerMeleeAnimation(0.55f));
                    }
                    StartCoroutine(DelayedMeleeHitRoutine(0.20f, data.Damage > 0 ? data.Damage : 65f, data));
                    DecrementDurability(data);
                }
                return;
            }

            if (data.isTrap || data.category == WeaponCategory.Trap) { HandleTrapFire(data, fireHeld, fireReleased); return; }
            if (data.isCharged) { HandleChargedFire(data, dt, fireHeld, fireReleased); return; }
            if (CurrentAttach.name.Contains("Flame")) { HandleFlameFire(data, fireHeld, firePressed); return; }

            if (data.maxBounces > 0 || (CurrentAttach != null && CurrentAttach.name.Contains("TrickShot")))
            {
                HandleBounceFire(data, fireHeld, fireReleased);
                return;
            }

            if (data.requiresADSToFire || (data.hasADS && CurrentAttach != null && CurrentAttach.name.Contains("Sniper")))
            {
                HandleSniperFire(data, fireHeld, firePressed, fireReleased, input.Intent.AltFire);
                return;
            }

            // Standard Ranged (Auto / Semi-Auto)
            bool isAuto = data.isAutomatic || data.isAutoFire || (data.weaponName != null && (data.weaponName.Contains("Shooter") || data.weaponName.Contains("Pistol")));
            bool wantFire = isAuto ? fireHeld : firePressed;

            if (wantFire && CanFire(data) && _timeSinceLastShot >= data.FireRate)
            {
                _aimHandler.TriggerCombatStance();
                FireProjectile(data);
                if (animator != null) animator.Play("Shoot", 1, 0f);
            }
            else if (firePressed && !CanFire(data) && _timeSinceLastShot >= 0.20f)
            {
                _timeSinceLastShot = 0f;
                if (data.audio?.emptyClickClip != null && AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFXAtPosition(data.audio.emptyClickClip, transform.position, 0.6f, false);
            }

            if (isAuto && !fireHeld && !_aimHandler.IsInCombatStance && animator != null)
                animator.SetBool(HashIsFiring, false);
        }

        private void HandleBounceFire(WeaponSO data, bool fireHeld, bool fireReleased)
        {
            if (fireHeld && CanFire(data))
            {
                if (!_isAimingBounce)
                {
                    _isAimingBounce = true;
                    if (motor != null) motor.IsInCombat = true;
                    if (animator != null) { animator.SetBool(HashIsFiring, true); animator.SetFloat(HashShootSpeed, 0f); animator.Play("Shoot", 1, 0f); }
                }
            }
            else if (fireReleased && _isAimingBounce)
            {
                _isAimingBounce = false;
                if (_timeSinceLastShot >= data.FireRate)
                {
                    _aimHandler.TriggerCombatStance();
                    FireProjectile(data);
                    if (animator != null) { animator.SetFloat(HashShootSpeed, 1f); animator.Play("Shoot", 1, 0f); }
                }
                else _aimHandler.ExitCombatStance();
                _aimHandler.DisableBouncePreview();
            }
            else if (!fireHeld && _isAimingBounce)
            {
                _isAimingBounce = false;
                _aimHandler.DisableBouncePreview();
                _aimHandler.ExitCombatStance();
            }
        }

        private void HandleSniperFire(WeaponSO data, bool fireHeld, bool firePressed, bool fireReleased, bool altFireHeld)
        {
            if (altFireHeld)
            {
                _aimHandler.TriggerCombatStance();
                if (firePressed && CanFire(data) && _timeSinceLastShot >= data.FireRate)
                {
                    FireProjectile(data);
                    if (animator != null) animator.Play("Shoot", 1, 0f);
                }
            }
            else
            {
                if (fireHeld && CanFire(data))
                {
                    _aimHandler.TriggerCombatStance();
                    if (animator != null) { animator.SetBool(HashIsFiring, true); animator.SetFloat(HashShootSpeed, 0f); }
                }
                else if (fireReleased)
                {
                    if (CanFire(data) && _timeSinceLastShot >= data.FireRate)
                    {
                        _aimHandler.TriggerCombatStance();
                        FireProjectile(data);
                        if (animator != null) { animator.SetFloat(HashShootSpeed, 1f); animator.Play("Shoot", 1, 0f); }
                    }
                    if (animator != null) { animator.SetBool(HashIsFiring, false); animator.SetFloat(HashShootSpeed, 1f); }
                }
            }
        }

        private void HandleFlameFire(WeaponSO data, bool fireHeld, bool firePressed)
        {
            if (fireHeld && CanFire(data))
            {
                _aimHandler.TriggerCombatStance();
                if (animator != null && firePressed) { animator.Play("Shoot", 1, 0f); animator.SetFloat(HashShootSpeed, 0f); }
                if (_timeSinceLastShot >= data.FireRate) FireProjectile(data);
            }
            else if (!_aimHandler.IsInCombatStance && animator != null)
            {
                animator.SetBool(HashIsFiring, false);
                animator.SetFloat(HashShootSpeed, 1f);
            }
        }

        private bool CanFire(WeaponSO data)
        {
            if (data == null) return false;
            return data.durabilityType switch
            {
                DurabilityType.Ammo => _currentAmmo > 0,
                DurabilityType.Uses => _currentUses > 0,
                DurabilityType.TimeBased => _timeRemaining > 0f,
                _ => true
            };
        }

        private void FireProjectile(WeaponSO data, Vector3? customOrigin = null, Vector3? customVel = null)
        {
            _timeSinceLastShot = 0f;

            Vector3 origin = customOrigin ?? (Muzzle != null ? Muzzle.position : transform.position + Vector3.up * 1.2f);
            if (!customOrigin.HasValue && origin.y < transform.position.y + 0.9f) origin.y = transform.position.y + 0.9f;

            Camera mainCam = Camera.main;
            Vector3 camForward = mainCam != null ? mainCam.transform.forward : (Aim != null ? Aim.CameraPlanarForward : transform.forward);
            Vector3 aimPoint = Aim != null ? Aim.AimPoint : origin + camForward * 100f;
            Vector3 dir = customVel.HasValue ? customVel.Value.normalized : (aimPoint - origin).normalized;

            if (!customVel.HasValue && Vector3.Dot(dir, camForward) <= 0.15f) dir = camForward;
            if (dir.sqrMagnitude < 0.0001f) dir = Muzzle != null ? Muzzle.forward : transform.forward;

            Vector3 launchVel = customVel ?? (dir * (data.throwForce > 0f ? data.throwForce : 14f));
            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

            FireProjectileRpc(_currentIndex, origin, rot, launchVel);

            if (data.audio?.fireClip != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFXAtPosition(data.audio.fireClip, origin);

            if (IsOwner)
            {
                FireWeaponServerRpc(origin, dir, data.name, Time.time);
                CameraShakeManager.Instance?.ShakeWeaponFire(data);
                MobileHapticsManager.TriggerShotFired();
            }

            if (data.projectilePrefab != null)
            {
                var proj = GlobalPoolManager.Instance != null ? GlobalPoolManager.Instance.Spawn(data.projectilePrefab, origin, rot) : Instantiate(data.projectilePrefab, origin, rot);
                proj.GetComponent<BaseProjectile>()?.Init(data, gameObject);
                IgnoreOwnerCollisions(proj);
                proj.GetComponent<TrapBase>()?.Initialise((int)OwnerClientId, Camera.main, data, launchVel);
            }
            else if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, hitRange, hitMask, QueryTriggerInteraction.Ignore))
            {
                GameObject hitVFX = data.HitVFXPrefab ?? fallbackHitscanImpactVFX;
                if (hitVFX != null)
                {
                    Instantiate(hitVFX, hit.point, Quaternion.LookRotation(hit.normal));
                }
                if (hitscanImpactSound != null) AudioSource.PlayClipAtPoint(hitscanImpactSound, hit.point, 0.6f);
            }

            if (_muzzleFlash != null) _muzzleFlash.Play();
            DecrementDurability(data);
        }

        public void OnPlayerDied()
        {
            CancelTrapAim();
            if (_trajectoryPredictor != null) _trajectoryPredictor.HideTrajectory();
            if (_isAimingBounce) { _isAimingBounce = false; _aimHandler?.DisableBouncePreview(); }
            if (_chargeHandler != null && _chargeHandler.IsCharging) CancelCharge();
            if (_adsHandler != null && _adsHandler.IsADSActive) _adsHandler.ExitADS(instant: true);
            if (_aimHandler != null && _aimHandler.IsInCombatStance) _aimHandler.ExitCombatStance();
            _lastFire = false;
        }

    }
}
