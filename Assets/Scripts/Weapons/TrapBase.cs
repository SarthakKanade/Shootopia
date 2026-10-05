using UnityEngine;

namespace Shootopia
{
    public class TrapBase : MonoBehaviour
    {
        protected int _ownerID;
        protected Camera _cam;

        [SerializeField] private float throwScaleMultiplier = 1.5f;
        private Vector3 _baseScale = Vector3.one;
        private bool _hasBaseScale = false;

        public void Initialise(int ownerID, Camera cam, WeaponSO data, Vector3 launchVelocity = default)
        {
            _ownerID = ownerID;
            _cam = cam;
            IgnoreOwnerCollisions(ownerID);

            if (!_hasBaseScale)
            {
                _baseScale = transform.localScale;
                _hasBaseScale = true;
            }
            float mult = (data != null && data.throwScaleMultiplier > 0f) ? data.throwScaleMultiplier : throwScaleMultiplier;
            transform.localScale = _baseScale * mult;

            OnInitialised(data, launchVelocity);
        }

        protected virtual void OnDisable()
        {
            if (_hasBaseScale)
            {
                transform.localScale = _baseScale;
            }
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
