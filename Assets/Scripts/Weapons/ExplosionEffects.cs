using UnityEngine;

namespace Shootopia
{
    public class ExplosionEffects : MonoBehaviour
    {
        [SerializeField] private Light _light;
        [SerializeField] private float _lightDuration = 0.1f;

        void Awake()
        {
            if (_light == null)
            {
                _light = GetComponentInChildren<Light>();
            }
        }

        void Start()
        {
            if (_light != null)
            {
                Destroy(_light.gameObject, _lightDuration);
            }
        }
    }
}
