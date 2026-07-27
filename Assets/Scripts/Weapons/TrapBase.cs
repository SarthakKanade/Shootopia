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
            OnInitialised(data, launchVelocity);
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
