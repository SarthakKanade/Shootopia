using System.Collections;
using UnityEngine;

namespace Shootopia
{
    public partial class WeaponController
    {
        private void DecrementDurability(WeaponSO data)
        {
            if (data == null) return;
            switch (data.durabilityType)
            {
                case DurabilityType.Ammo:
                    _currentAmmo = Mathf.Max(0, _currentAmmo - 1);
                    onDurabilityChanged?.Raise(_currentAmmo);
                    if (_currentAmmo <= 0) ExpireWeapon();
                    break;
                case DurabilityType.Uses:
                    _currentUses = Mathf.Max(0, _currentUses - 1);
                    onDurabilityChanged?.Raise(_currentUses);
                    if (_currentUses <= 0) ExpireWeapon();
                    break;
            }
        }

        private void ExpireWeapon()
        {
            if (IsServer)
            {
                _equippedAttachName.Value = "";
                ForceEquipWeaponRpc("");
            }
            else
            {
                EquipAttachByNameLocal("", forceFresh: true);
            }
            onWeaponExpired?.Raise();
            onDurabilityChanged?.Raise(0f);
        }

        private void InitDurability(WeaponSO data)
        {
            if (_timeRoutine != null) { StopCoroutine(_timeRoutine); _timeRoutine = null; }
            float max = 1f;
            switch (data.durabilityType)
            {
                case DurabilityType.Ammo:
                    _currentAmmo = data.MagazineSize; max = data.MagazineSize;
                    onDurabilityChanged?.Raise(_currentAmmo);
                    break;
                case DurabilityType.Uses:
                    _currentUses = data.maxUses; max = data.maxUses;
                    onDurabilityChanged?.Raise(_currentUses);
                    break;
                case DurabilityType.TimeBased:
                    _timeRemaining = data.weaponDuration; max = data.weaponDuration;
                    _timeRoutine = StartCoroutine(TimeBasedRoutine());
                    break;
            }
            onMaxDurabilityChanged?.Raise(max);
        }

        private IEnumerator TimeBasedRoutine()
        {
            while (_timeRemaining > 0f)
            {
                _timeRemaining -= Time.deltaTime;
                onDurabilityChanged?.Raise(_timeRemaining);
                yield return null;
            }
            ExpireWeapon();
        }

        private void ResetWeaponState()
        {
            CancelTrapAim();
            if (_trapCooldownRoutine != null) { StopCoroutine(_trapCooldownRoutine); _trapCooldownRoutine = null; }
            _inTrapCooldown = false;
            if (_timeRoutine != null) { StopCoroutine(_timeRoutine); _timeRoutine = null; }

            CancelCharge();
            _isAimingBounce = false;
            if (_aimHandler != null)
            {
                _aimHandler.DisableBouncePreview();
                _aimHandler.ExitCombatStance();
            }
            if (_adsHandler != null && _adsHandler.IsADSActive) _adsHandler.ExitADS(instant: true);
            if (MobileControlsCanvas.Instance != null) MobileControlsCanvas.Instance.ResetAltFireToggle();

            if (_currentVisual != null) Destroy(_currentVisual);
            Muzzle = GripR = GripL = null;
            CurrentAttach = null;
            _muzzleFlash = null;
            _currentIndex = -1;

            if (animator != null) animator.SetFloat(HashStance, 2f);
        }

        private IEnumerator TrapCooldownRoutine()
        {
            _inTrapCooldown = true;
            yield return new WaitForSeconds(1.0f);
            _inTrapCooldown = false;

            if (CurrentAttach?.gameplayData != null)
            {
                if (CurrentDurabilityValue > 0)
                {
                    if (_currentVisual != null) _currentVisual.SetActive(true);
                }
                else
                {
                    ExpireWeapon();
                }
            }
        }
    }
}
