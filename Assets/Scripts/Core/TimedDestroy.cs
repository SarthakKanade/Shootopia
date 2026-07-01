using UnityEngine;

namespace Shootopia
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
