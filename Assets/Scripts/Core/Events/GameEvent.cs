using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shootopia
{
    [CreateAssetMenu(menuName = "Shootopia/Events/Game Event")]
    public class GameEvent : ScriptableObject
    {
        private readonly List<GameEventListener> _listeners = new();
        private event Action _onEventRaised;

        /// <summary>
        /// Broadcast to all registered listeners. Call on the SO asset.
        /// </summary>
        public void Raise()
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                if (_listeners[i] == null)
                {
                    _listeners.RemoveAt(i);
                    continue;
                }
                _listeners[i].OnEventRaised();
            }
            
            _onEventRaised?.Invoke();
        }

        public void Register(GameEventListener l) => _listeners.Add(l);
        public void Deregister(GameEventListener l) => _listeners.Remove(l);

        public void RegisterListener(Action action) => _onEventRaised += action;
        public void UnregisterListener(Action action) => _onEventRaised -= action;
    }
}
