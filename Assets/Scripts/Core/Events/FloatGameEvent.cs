using System;
using System.Collections.Generic;
using UnityEngine;

namespace VelocityWars.Core
{
    [CreateAssetMenu(menuName = "VelocityWars/Events/Float Event")]
    public class FloatGameEvent : ScriptableObject
    {
        private readonly List<FloatGameEventListener> _listeners = new();
        private event Action<float> _onEventRaised;

        public void Raise(float value)
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                if (_listeners[i] == null)
                {
                    _listeners.RemoveAt(i);
                    continue;
                }
                _listeners[i].OnEventRaised(value);
            }
            
            _onEventRaised?.Invoke(value);
        }

        public void Register(FloatGameEventListener l) => _listeners.Add(l);
        public void Deregister(FloatGameEventListener l) => _listeners.Remove(l);

        public void RegisterListener(Action<float> action) => _onEventRaised += action;
        public void UnregisterListener(Action<float> action) => _onEventRaised -= action;
    }
}
