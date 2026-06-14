using UnityEngine;
using VelocityWars;

namespace VelocityWars.Weapons
{
    public class WeaponCrateTrigger : MonoBehaviour
    {
        [SerializeField] private Light _glowLight;

        private WeaponSO _assignedWeapon;
        private ActiveWeapon _targetActiveWeapon;
        private WeaponSpawnNode _ownerNode;

        public void Init(WeaponSO weapon, ActiveWeapon playerWeapon, WeaponSpawnNode owner)
        {
            _assignedWeapon = weapon;
            _targetActiveWeapon = playerWeapon;
            _ownerNode = owner;
            
            if (_glowLight == null)
                _glowLight = GetComponentInChildren<Light>();

            if (_glowLight != null && _assignedWeapon != null)
            {
                _glowLight.color = GetColorForCategory(_assignedWeapon.category);
            }
        }

        private Color GetColorForCategory(WeaponCategory category)
        {
            return category switch
            {
                WeaponCategory.Control => Color.cyan,
                WeaponCategory.Burst => Color.red,
                WeaponCategory.Precision => Color.yellow,
                WeaponCategory.Chaos => new Color(0.5f, 0f, 0.5f), // Purple
                _ => Color.white,
            };
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                if (_targetActiveWeapon != null && _assignedWeapon != null)
                {
                    _targetActiveWeapon.SwitchWeapon(_assignedWeapon);
                }

                if (_ownerNode != null)
                {
                    _ownerNode.OnCratePickedUp();
                }

                Destroy(gameObject);
            }
        }
    }
}
