using UnityEngine;

namespace Shootopia
{
    public abstract class BaseProjectile : MonoBehaviour
    {
        protected WeaponSO _weaponData;
        protected GameObject _owner;

        public virtual void Init(WeaponSO data, GameObject owner)
        {
            _weaponData = data;
            _owner = owner;
        }
    }
}
