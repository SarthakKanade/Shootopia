using UnityEngine;

namespace Shootopia
{
    public class TrapBase : MonoBehaviour
    {
        protected int _ownerID;
        protected Camera _cam;

        public void Initialise(int ownerID, Camera cam, WeaponSO data, Vector3 launchVelocity = default)
        {
            _ownerID = ownerID;
            _cam = cam;
            IgnoreOwnerCollisions(ownerID);
            OnInitialised(data, launchVelocity);
        }

        private void IgnoreOwnerCollisions(int ownerID)
        {
            var myCols = GetComponentsInChildren<Collider>(true);
            if (myCols == null || myCols.Length == 0) return;

            if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.SpawnManager != null && ownerID >= 0)
            {
                var ownerNetObj = Unity.Netcode.NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject((ulong)ownerID);
                if (ownerNetObj != null)
                {
                    var oCols = ownerNetObj.GetComponentsInChildren<Collider>(true);
                    foreach (var mc in myCols)
                    {
                        foreach (var oc in oCols)
                        {
                            Physics.IgnoreCollision(mc, oc, true);
                        }
                    }
                }
            }
        }

        protected virtual void OnInitialised(WeaponSO data, Vector3 launchVelocity)
        {
            if (launchVelocity.sqrMagnitude > 0.01f)
            {
                var rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = launchVelocity;
                }
            }
        }
        public int OwnerID => _ownerID;
    }
}
