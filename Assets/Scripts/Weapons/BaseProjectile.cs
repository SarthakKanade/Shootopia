using UnityEngine;
using Unity.Netcode;

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

        protected bool IsOwnerProjectile()
        {
            if (_owner == null) return false;
            var netObj = _owner.GetComponent<NetworkObject>();
            return netObj != null && netObj.IsOwner;
        }
    }
}
