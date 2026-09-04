using UnityEngine;
using UnityEngine.EventSystems;

namespace Shootopia
{
    /// <summary>
    /// Touch drag surface for mobile camera look and aim control.
    /// Captures swipe deltas from the right portion of the screen,
    /// tracking specific pointer IDs to prevent cross-touch interference.
    /// </summary>
    public class TouchLookZone : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("Sensitivity Settings")]
        [SerializeField] private float baseSensitivity = 0.20f;
        [SerializeField] private bool invertPitch = false;

        private int _activePointerId = -1;
        private Vector2 _lastPointerPosition;
        private Vector2 _accumulatedLookDelta = Vector2.zero;

        private void OnEnable()
        {
            DeviceProfileManager.OnTouchSensitivityChanged += HandleSensitivityChanged;
            baseSensitivity = DeviceProfileManager.TouchSensitivity;
        }

        private void OnDisable()
        {
            DeviceProfileManager.OnTouchSensitivityChanged -= HandleSensitivityChanged;
            ResetLookZone();
        }

        private void HandleSensitivityChanged(float newSensitivity)
        {
            baseSensitivity = newSensitivity;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_activePointerId != -1) return;

            _activePointerId = eventData.pointerId;
            _lastPointerPosition = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _activePointerId) return;

            Vector2 delta = eventData.position - _lastPointerPosition;
            _lastPointerPosition = eventData.position;

            if (invertPitch) delta.y = -delta.y;

            // Invert Y delta if needed: PlayerAimRig subtracts look.y * pitchSensitivity from pitch,
            // which expects upward mouse/touch delta to look up.
            _accumulatedLookDelta += delta * baseSensitivity;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _activePointerId) return;
            ResetLookZone();
        }

        /// <summary>
        /// Reads and clears the accumulated look delta for the current frame.
        /// </summary>
        public Vector2 ConsumeLookDelta()
        {
            Vector2 delta = _accumulatedLookDelta;
            _accumulatedLookDelta = Vector2.zero;
            return delta;
        }

        private void ResetLookZone()
        {
            _activePointerId = -1;
            _accumulatedLookDelta = Vector2.zero;
        }
    }
}
