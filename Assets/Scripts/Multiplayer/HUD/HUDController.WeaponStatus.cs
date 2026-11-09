using UnityEngine;

namespace Shootopia
{
    public partial class HUDController
    {
private void UpdateWeaponUI()
        {
            if (_localWeaponController == null || _localWeaponController.CurrentAttach == null)
            {
                if (weaponIcon != null)
                {
                    weaponIcon.sprite = pistolSprite != null ? pistolSprite : defaultWeaponSprite;
                    weaponIcon.color = Color.white;
                    weaponIcon.gameObject.SetActive(true);
                }
                if (weaponCategoryText != null) weaponCategoryText.text = "PISTOL";
                if (weaponNameText != null) weaponNameText.text = "SIDEARM";
                if (ammoText != null) ammoText.text = "∞";
                if (cooldownBarRoot != null) cooldownBarRoot.SetActive(false);
                return;
            }

            var attach = _localWeaponController.CurrentAttach;
            var weaponData = attach.gameplayData;

            string rawName = "";
            if (weaponData != null && !string.IsNullOrEmpty(weaponData.weaponName))
            {
                rawName = weaponData.weaponName;
            }
            else if (attach != null)
            {
                rawName = attach.name;
            }

            if (weaponIcon != null)
            {
                Sprite icon = (weaponData != null && weaponData.icon != null) ? weaponData.icon : GetWeaponSprite(rawName);
                if (icon != null)
                {
                    weaponIcon.sprite = icon;
                    weaponIcon.color = Color.white;
                    weaponIcon.gameObject.SetActive(true);
                }
            }

            if (weaponCategoryText != null)
            {
                weaponCategoryText.text = weaponData != null ? weaponData.category.ToString().ToUpper() : "WEAPON";
            }

            if (weaponNameText != null)
            {
                weaponNameText.text = !string.IsNullOrEmpty(rawName) ? rawName.ToUpper() : "WEAPON";
            }

            if (ammoText != null)
            {
                if (weaponData != null && weaponData.durabilityType == DurabilityType.Ammo)
                {
                    ammoText.text = $"{_localWeaponController.CurrentAmmo} / {weaponData.MagazineSize}";
                }
                else if (weaponData != null && weaponData.durabilityType == DurabilityType.Uses)
                {
                    ammoText.text = $"{_localWeaponController.CurrentUses} USES";
                }
                else if (weaponData != null && weaponData.durabilityType == DurabilityType.TimeBased)
                {
                    ammoText.text = $"{_localWeaponController.TimeRemaining:0.0}s";
                }
                else
                {
                    ammoText.text = "∞";
                }
            }
        }
    }
}
