using UnityEngine;

namespace Shootopia
{
    public partial class WeaponController
    {
        public void CycleWeapon()
        {
            _aimHandler.ExitCombatStance();
            if (loadout == null || loadout.Length == 0) return;
            int next = (_currentIndex + 1) % loadout.Length;
            if (loadout[next] != null)
            {
                _currentIndex = next;
                EquipNetworkedWeapon(loadout[next].name);
            }
        }

        public void EquipByAttachSO(WeaponAttachSO attach)
        {
            if (attach != null) EquipNetworkedWeapon(attach.name);
        }

        public void ReplaceCurrentWeapon(WeaponAttachSO attach)
        {
            if (attach != null)
            {
                RegisterAttach(attach);
                EquipNetworkedWeapon(attach.name);
            }
        }

        public void Equip(int index)
        {
            if (loadout != null && index >= 0 && index < loadout.Length && loadout[index] != null)
            {
                _currentIndex = index;
                EquipNetworkedWeapon(loadout[index].name);
            }
            else
            {
                ExpireWeapon();
            }
        }

        public void EquipAttachByNameLocal(string attachName, bool forceFresh = false)
        {
            if (string.IsNullOrEmpty(attachName))
            {
                ResetWeaponState();
                return;
            }

            var attach = GetAttachByName(attachName);
            if (attach == null) return;
            if (!forceFresh && CurrentAttach == attach && _currentVisual != null) return;

            ResetWeaponState();

            // Defense / Consumable item instant usage
            if (attach.gameplayData != null)
            {
                var cType = attach.gameplayData.consumableType;
                if (cType == ConsumableType.Health || attach.gameplayData.name.Contains("Health") || attach.name.Contains("Health"))
                {
                    var hp = GetComponent<PlayerHealth>() ?? GetComponentInParent<PlayerHealth>();
                    if (hp != null) hp.Heal(hp.GetMaxHp());
                    ExpireWeapon();
                    return;
                }

                if (cType == ConsumableType.Shield || attach.gameplayData.name.Contains("Shield") || attach.name.Contains("Shield"))
                {
                    var sh = GetComponent<PlayerShield>() ?? GetComponentInParent<PlayerShield>();
                    if (sh != null) sh.GrantShield(50f);
                    ExpireWeapon();
                    return;
                }

                if (cType == ConsumableType.Invisibility || attach.gameplayData.name.Contains("Invis") || attach.name.Contains("Invis") || attach.name.Contains("Cloak"))
                {
                    var invis = GetComponent<PlayerInvisibility>() ?? GetComponentInParent<PlayerInvisibility>();
                    if (invis != null) invis.ActivateInvisibility(10f);
                    ExpireWeapon();
                    return;
                }
            }

            CurrentAttach = attach;
            if (loadout != null)
            {
                for (int i = 0; i < loadout.Length; i++)
                {
                    if (loadout[i] != null && loadout[i].name == attachName)
                    {
                        _currentIndex = i;
                        break;
                    }
                }
            }

            var data = attach.gameplayData;
            if (data != null)
            {
                InitDurability(data);
                if (data.audio?.drawClip != null && AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFXAtPosition(data.audio.drawClip, transform.position);
            }

            if (attach.visualPrefab != null && weaponSocket != null)
            {
                _currentVisual = Instantiate(attach.visualPrefab, weaponSocket);
                _currentVisual.transform.localPosition = attach.equipLocalPos;
                _currentVisual.transform.localRotation = Quaternion.Euler(attach.equipLocalEuler);
                _currentVisual.transform.localScale = Vector3.one;

                foreach (var rb in _currentVisual.GetComponentsInChildren<Rigidbody>(true)) rb.isKinematic = true;
                foreach (var col in _currentVisual.GetComponentsInChildren<Collider>(true)) col.enabled = false;
                foreach (var tr in _currentVisual.GetComponentsInChildren<TrapBase>(true)) tr.enabled = false;
                foreach (var lt in _currentVisual.GetComponentsInChildren<Light>(true)) lt.enabled = false;

                Muzzle = FindChild(_currentVisual.transform, attach.muzzleChildName);
                GripR = FindChild(_currentVisual.transform, attach.gripRChildName);
                GripL = FindChild(_currentVisual.transform, attach.gripLChildName);
                _muzzleFlash = _currentVisual.GetComponentInChildren<ParticleSystem>(true);

                if (attach.snapRightGripToHand && GripR != null)
                {
                    Vector3 gripInSocket = weaponSocket.InverseTransformPoint(GripR.position);
                    _currentVisual.transform.localPosition -= gripInSocket;
                }
            }

            if (animator != null)
            {
                float stance = attach.stanceId;
                if (attach.stanceId == 3 || (data != null && (data.category == WeaponCategory.Melee || data.name.Contains("Knife")))) stance = 3f;
                else if (attach.stanceId == 2 || (data != null && (data.isTrap || data.category == WeaponCategory.Trap))) stance = 2f;
                else if (data != null) stance = data.handedness == WeaponHandedness.OneHanded ? 1f : 0f;
                animator.SetFloat(HashStance, stance);
            }
        }
    }
}

