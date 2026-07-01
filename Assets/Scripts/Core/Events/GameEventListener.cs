using UnityEngine;
using UnityEngine.Events;

namespace Shootopia
{
    /// <summary>
    /// Add to any GameObject. Assign a GameEvent SO and a UnityEvent response.
    /// The response fires whenever the SO's Raise() is called — from anywhere.
    /// </summary>
    public class GameEventListener : MonoBehaviour
    {
        [SerializeField] private GameEvent _event;
        [SerializeField] private UnityEvent _response;

        private void OnEnable()  => _event?.Register(this);
        private void OnDisable() => _event?.Deregister(this);
        public void OnEventRaised() => _response?.Invoke();
    }
}
