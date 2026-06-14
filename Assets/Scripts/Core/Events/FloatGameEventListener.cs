using UnityEngine;
using UnityEngine.Events;

namespace VelocityWars.Core
{
    [System.Serializable]
    public class FloatEvent : UnityEvent<float> { }

    public class FloatGameEventListener : MonoBehaviour
    {
        [SerializeField] private FloatGameEvent _event;
        [SerializeField] private FloatEvent _response;

        private void OnEnable()
        {
            if (_event != null)
            {
                _event.Register(this);
            }
        }

        private void OnDisable()
        {
            if (_event != null)
            {
                _event.Deregister(this);
            }
        }

        public void OnEventRaised(float value)
        {
            _response?.Invoke(value);
        }
    }
}
