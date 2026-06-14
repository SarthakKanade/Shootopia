using UnityEngine;

namespace VelocityWars
{
    public abstract class BaseTrap : MonoBehaviour
    {
        protected GameObject _owner;

        public virtual void SetOwner(GameObject owner)
        {
            _owner = owner;
        }
    }
}
