using UnityEngine;

namespace VelocityWars
{
    public class TrapBase : MonoBehaviour
    {
        protected int _ownerID;
        protected Camera _cam;

        public void Initialise(int ownerID, Camera cam, WeaponSO data)
        {
            _ownerID = ownerID;
            _cam = cam;
            OnInitialised(data);
        }

        protected virtual void OnInitialised(WeaponSO data) { }
        public int OwnerID => _ownerID;
    }
}
