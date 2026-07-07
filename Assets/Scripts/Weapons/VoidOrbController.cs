using UnityEngine;
using Shootopia;

namespace Shootopia
{
    public class VoidOrbController : BaseProjectile
    {
        [SerializeField] private GameObject voidZonePrefab;
        private Rigidbody _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        public override void Init(WeaponSO data, GameObject owner)
        {
            base.Init(data, owner);
            if (_rb != null)
            {
                // Set initial velocity based on forward direction
                _rb.linearVelocity = transform.forward * data.projectileSpeed;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Avoid spawning on owner or other triggers
            if (other.isTrigger) return;
            if (_owner != null && other.gameObject == _owner) return;
            
            SpawnVoidZone();
        }

        private void SpawnVoidZone()
        {
            if (voidZonePrefab != null)
            {
                var zone = Instantiate(voidZonePrefab, transform.position, Quaternion.identity);
                var zoneScript = zone.GetComponent<VoidZone>();
                if (zoneScript != null && _weaponData != null)
                {
                    ulong ownerId = Unity.Netcode.NetworkManager.ServerClientId;
                    if (_owner != null)
                    {
                        var netObj = _owner.GetComponent<Unity.Netcode.NetworkObject>();
                        if (netObj != null) ownerId = netObj.OwnerClientId;
                    }
                    zoneScript.Initialize(_weaponData, ownerId);
                }
            }
            Destroy(gameObject);
        }
    }
}
