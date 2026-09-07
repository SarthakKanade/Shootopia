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
        private WeaponController _localWeapon;

        private void OnEnable()
        {
            DeviceProfileManager.OnTouchSensitivityChanged += HandleSensitivityChanged;
            baseSensitivity = DeviceProfileManager.TouchSensitivityX;
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

        private void EnsureLocalWeapon()
        {
            if (_localWeapon == null)
            {
                var networkManager = Unity.Netcode.NetworkManager.Singleton;
                if (networkManager != null && networkManager.SpawnManager != null)
                {
                    var localObj = networkManager.SpawnManager.GetLocalPlayerObject();
                    if (localObj != null)
                    {
                        _localWeapon = localObj.GetComponent<WeaponController>();
                    }
                }
            }
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

            bool shouldInvert = invertPitch || (PlayerProfileManager.Instance != null && PlayerProfileManager.Instance.InvertY);
            if (shouldInvert) delta.y = -delta.y;

            EnsureLocalWeapon();
            bool isScoped = _localWeapon != null && _localWeapon.IsADSActive;
            float adsMult = isScoped ? DeviceProfileManager.TouchSensitivityADS : 1.0f;

            float sensX = DeviceProfileManager.TouchSensitivityX;
            float sensY = DeviceProfileManager.TouchSensitivityY;

            _accumulatedLookDelta.x += delta.x * (sensX * adsMult);
            _accumulatedLookDelta.y += delta.y * (sensY * adsMult);
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
