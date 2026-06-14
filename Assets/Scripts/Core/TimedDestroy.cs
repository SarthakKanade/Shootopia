using UnityEngine;

namespace VelocityWars.Core
{
    public class TimedDestroy : MonoBehaviour
    {
        public float delay = 1.0f;

        void Start()
        {
            Destroy(gameObject, delay);
        }
    }
}
