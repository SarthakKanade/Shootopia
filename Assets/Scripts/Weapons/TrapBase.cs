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
            if (myCols == null || myCols.Length == 0 || ownerID < 0) return;

            GameObject ownerGo = null;
            if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                var nm = Unity.Netcode.NetworkManager.Singleton;
                if (nm.IsServer || (ulong)ownerID == nm.LocalClientId)
                {
                    var ownerNetObj = nm.SpawnManager?.GetPlayerNetworkObject((ulong)ownerID);
                    if (ownerNetObj != null) ownerGo = ownerNetObj.gameObject;
                }
            }

            if (ownerGo == null)
            {
                for (int i = 0; i < PlayerHealth.ActivePlayers.Count; i++)
                {
                    var p = PlayerHealth.ActivePlayers[i];
                    if (p != null && (int)p.OwnerClientId == ownerID)
                    {
                        ownerGo = p.gameObject;
                        break;
                    }
                }
            }

            if (ownerGo != null)
            {
                var oCols = ownerGo.GetComponentsInChildren<Collider>(true);
                foreach (var mc in myCols)
                {
                    if (mc == null || mc is CharacterController) continue;
                    foreach (var oc in oCols)
                    {
                        if (oc != null && mc != oc && !(oc is CharacterController))
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
