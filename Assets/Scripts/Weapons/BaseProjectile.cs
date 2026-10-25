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

        protected void DealDamage(IDamageable target, float amount, DamageType type, Collider hitCollider = null)
        {
            if (target == null) return;

            if (IsOwnerProjectile())
            {
                ulong attackerId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : NetworkManager.ServerClientId;
                if (_owner != null)
                {
                    var netObj = _owner.GetComponent<NetworkObject>();
                    if (netObj != null) attackerId = netObj.OwnerClientId;
                }

                PlayerHealth targetHealth = null;
                if (hitCollider != null)
                {
                    targetHealth = hitCollider.GetComponentInParent<PlayerHealth>() ?? hitCollider.GetComponent<PlayerHealth>();
                }
                if (targetHealth == null && target is Component comp)
                {
                    targetHealth = comp.GetComponentInParent<PlayerHealth>() ?? comp.GetComponent<PlayerHealth>();
                }

                if (targetHealth != null)
                {
                    targetHealth.LastAttackerClientId = attackerId;
                    targetHealth.TakeDamage(amount, type, _weaponData, attackerId);
                    return;
                }
            }

            target.TakeDamage(amount, type, _weaponData);
        }
    }
}
