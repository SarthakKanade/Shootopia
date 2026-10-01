using UnityEngine;
using UnityEngine.EventSystems;

namespace Shootopia
{
    /// <summary>
    /// High-performance, zero-allocation virtual movement joystick for mobile touch screens.
    /// Supports both dynamic floating (spawns under thumb anywhere in zone) and static positioning.
    /// Multi-touch isolated via pointerId tracking.
    /// </summary>
    public class TouchVirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public enum JoystickBehavior
        {
            Fixed = 0,
            Dynamic = 1
        }

        [Header("Components")]
        [SerializeField] private RectTransform containerRect;
        [SerializeField] private RectTransform baseRect;
        [SerializeField] private RectTransform handleRect;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Configuration")]
        [SerializeField] private JoystickBehavior behavior = JoystickBehavior.Fixed;
        [SerializeField] private float handleRange = 85f;
        [SerializeField] private float deadZone = 6f;
        [SerializeField] private bool hideWhenIdle = false;
        [SerializeField] private float idleAlpha = 0.35f;
        [SerializeField] private float activeAlpha = 0.65f;

        private Vector2 _defaultBaseAnchoredPos;
        private Vector2 _inputVector = Vector2.zero;
        private int _activePointerId = -999;
        private Canvas _parentCanvas;
        private Camera _uiCamera;
        private RectTransform _parentRect;

        public Vector2 InputValue => _inputVector;

        private void Awake()
        {
            if (containerRect == null) containerRect = GetComponent<RectTransform>();
            _parentCanvas = GetComponentInParent<Canvas>();
            _uiCamera = (_parentCanvas != null && _parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? _parentCanvas.worldCamera
                : null;

            if (baseRect != null)
            {
                _parentRect = baseRect.parent as RectTransform;
                _defaultBaseAnchoredPos = baseRect.anchoredPosition;
            }

            ResetJoystick();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_activePointerId != -999) return;
            _activePointerId = eventData.pointerId;

            if (_parentRect == null && baseRect != null)
            {
                _parentRect = baseRect.parent as RectTransform;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = activeAlpha;
            }

            // Read active mode from DeviceProfileManager (fallback to serialized behavior)
            JoystickBehavior activeBehavior = (DeviceProfileManager.Instance != null)
                ? ((DeviceProfileManager.JoystickMode == MobileJoystickMode.Dynamic) ? JoystickBehavior.Dynamic : JoystickBehavior.Fixed)
                : behavior;

            // In Dynamic mode, center base at touch location (clamped inside parent)
            if (activeBehavior == JoystickBehavior.Dynamic && baseRect != null && _parentRect != null)
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRect, eventData.position, _uiCamera, out Vector2 localPointInParent))
                {
                    Rect bounds = _parentRect.rect;
                    float halfW = baseRect.rect.width * 0.5f;
                    float halfH = baseRect.rect.height * 0.5f;
                    localPointInParent.x = Mathf.Clamp(localPointInParent.x, bounds.xMin + halfW, bounds.xMax - halfW);
                    localPointInParent.y = Mathf.Clamp(localPointInParent.y, bounds.yMin + halfH, bounds.yMax - halfH);

                    baseRect.localPosition = localPointInParent;
                }
            }

            ProcessDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _activePointerId) return;
            ProcessDrag(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _activePointerId) return;
            ResetJoystick();
        }

        private void ProcessDrag(PointerEventData eventData)
        {
            if (baseRect == null || handleRect == null) return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRect, eventData.position, _uiCamera, out Vector2 localPosInBase))
            {
                float distance = localPosInBase.magnitude;

                if (distance > deadZone)
                {
                    Vector2 direction = localPosInBase / distance;
                    float clampedDist = Mathf.Min(distance, handleRange);
                    handleRect.anchoredPosition = direction * clampedDist;

                    // Response curve: deflecting ~45% of range reaches full 1.0 run speed!
                    float ratio = Mathf.Clamp01((clampedDist - deadZone) / (handleRange - deadZone));
                    float moveSens = DeviceProfileManager.TouchMoveSensitivity;
                    float magnitude = Mathf.Clamp01(Mathf.Pow(ratio, 0.7f) * moveSens);
                    _inputVector = direction * magnitude;
                }
                else
                {
                    handleRect.anchoredPosition = Vector2.zero;
                    _inputVector = Vector2.zero;
                }
            }
        }

        private void ResetJoystick()
        {
            _activePointerId = -999;
            _inputVector = Vector2.zero;

            if (handleRect != null)
            {
                handleRect.anchoredPosition = Vector2.zero;
            }

            if (baseRect != null)
            {
                baseRect.anchoredPosition = _defaultBaseAnchoredPos;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = hideWhenIdle ? 0f : idleAlpha;
            }
        }

        private void OnDisable()
        {
            ResetJoystick();
        }
    }
}
