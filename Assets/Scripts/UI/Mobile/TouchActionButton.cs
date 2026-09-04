using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Shootopia
{
    /// <summary>
    /// High-performance touch action button supporting both held actions (Fire, ADS)
    /// and edge-triggered impulses (Jump, Dash, Roll, Reload, Weapon Cycle, Shield).
    /// Includes visual press feedback and optional cooldown overlays.
    /// </summary>
    public class TouchActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [Header("Visual Feedback")]
        [SerializeField] private Graphic targetGraphic;
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.85f);
        [SerializeField] private Color pressedColor = new Color(0.3f, 0.8f, 1f, 1f);
        [SerializeField] private float pressedScale = 0.92f;
        [SerializeField] private Image cooldownOverlay;
        [SerializeField] private bool isToggle = false;

        [Header("Aim Integration")]
        [SerializeField] private bool allowDragToAim = false;

        [Header("2-Part Deflection (CODM Style)")]
        [SerializeField] private RectTransform innerButtonRect;
        [SerializeField] private float maxDeflectionDistance = 35f;
        [SerializeField] private float innerPressedScale = 0.88f;
        [SerializeField] private float springReturnSpeed = 22f;

        public event System.Action OnPressed;
        public event System.Action<bool> OnToggled;
        public event System.Action<Vector2> OnDragLook;

        private RectTransform _rectTransform;
        private Vector3 _originalScale = Vector3.one;
        private bool _isPressed = false;
        private bool _wasPressedThisFrame = false;
        private int _activePointerId = -999;

        private Vector2 _touchStartLocalPos;
        private Vector2 _currentInnerPos;
        private Vector2 _targetInnerPos;
        private Vector3 _originalInnerScale = Vector3.one;
        private Vector3 _currentInnerScale = Vector3.one;
        private Vector3 _targetInnerScale = Vector3.one;
        private Camera _uiCamera;
        private Canvas _parentCanvas;

        public bool IsPressed => _isPressed;
        public bool IsToggleMode => isToggle;
        public bool AllowDragToAim
        {
            get => allowDragToAim;
            set => allowDragToAim = value;
        }

        public RectTransform InnerButtonRect
        {
            get => innerButtonRect;
            set => innerButtonRect = value;
        }

        public float MaxDeflectionDistance
        {
            get => maxDeflectionDistance;
            set => maxDeflectionDistance = value;
        }

        public void SetToggle(bool toggle)
        {
            isToggle = toggle;
        }

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform != null) _originalScale = _rectTransform.localScale;
            if (targetGraphic == null) targetGraphic = GetComponent<Graphic>();

            _parentCanvas = GetComponentInParent<Canvas>();
            _uiCamera = (_parentCanvas != null && _parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? _parentCanvas.worldCamera
                : null;

            if (innerButtonRect != null)
            {
                _originalInnerScale = innerButtonRect.localScale;
                _currentInnerScale = _originalInnerScale;
                _targetInnerScale = _originalInnerScale;
                _currentInnerPos = innerButtonRect.anchoredPosition;
                _targetInnerPos = innerButtonRect.anchoredPosition;
            }

            ApplyVisualState(false);
        }

        private void Update()
        {
            if (innerButtonRect != null)
            {
                if (Vector2.Distance(_currentInnerPos, _targetInnerPos) > 0.01f)
                {
                    _currentInnerPos = Vector2.Lerp(_currentInnerPos, _targetInnerPos, Time.unscaledDeltaTime * springReturnSpeed);
                    innerButtonRect.anchoredPosition = _currentInnerPos;
                }
                else
                {
                    innerButtonRect.anchoredPosition = _targetInnerPos;
                    _currentInnerPos = _targetInnerPos;
                }

                if (Vector3.Distance(_currentInnerScale, _targetInnerScale) > 0.005f)
                {
                    _currentInnerScale = Vector3.Lerp(_currentInnerScale, _targetInnerScale, Time.unscaledDeltaTime * springReturnSpeed);
                    innerButtonRect.localScale = _currentInnerScale;
                }
                else
                {
                    innerButtonRect.localScale = _targetInnerScale;
                    _currentInnerScale = _targetInnerScale;
                }
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_activePointerId != -999) return;

            _activePointerId = eventData.pointerId;

            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
            if (_parentCanvas == null) _parentCanvas = GetComponentInParent<Canvas>();
            if (_uiCamera == null && _parentCanvas != null && _parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                _uiCamera = _parentCanvas.worldCamera;
            }

            if (innerButtonRect != null && _rectTransform != null)
            {
                _originalInnerScale = innerButtonRect.localScale;
                var cam = eventData.pressEventCamera != null ? eventData.pressEventCamera : _uiCamera;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, eventData.position, cam, out _touchStartLocalPos);
                _targetInnerPos = Vector2.zero;
                _targetInnerScale = _originalInnerScale * innerPressedScale;
            }

            if (isToggle)
            {
                _isPressed = !_isPressed;
                _wasPressedThisFrame = _isPressed;
                ApplyVisualState(_isPressed);
                OnToggled?.Invoke(_isPressed);
            }
            else
            {
                _isPressed = true;
                _wasPressedThisFrame = true;
                ApplyVisualState(true);
            }

            OnPressed?.Invoke();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _activePointerId) return;

            if (innerButtonRect != null && _rectTransform != null)
            {
                var cam = eventData.pressEventCamera != null ? eventData.pressEventCamera : _uiCamera;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, eventData.position, cam, out Vector2 currentLocalPos))
                {
                    Vector2 deflection = currentLocalPos - _touchStartLocalPos;
                    _targetInnerPos = Vector2.ClampMagnitude(deflection, maxDeflectionDistance);
                }
            }

            if (allowDragToAim)
            {
                OnDragLook?.Invoke(eventData.delta);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _activePointerId) return;
            _activePointerId = -999;

            if (innerButtonRect != null)
            {
                _targetInnerPos = Vector2.zero;
                _targetInnerScale = _originalInnerScale;
            }

            if (!isToggle)
            {
                _isPressed = false;
                ApplyVisualState(false);
            }
        }

        /// <summary>
        /// Reads whether the button was pressed down this frame and clears the edge flag.
        /// </summary>
        public bool ConsumeWasPressed()
        {
            if (_wasPressedThisFrame)
            {
                _wasPressedThisFrame = false;
                return true;
            }
            return false;
        }

        public void SetCooldown(float normalizedFill)
        {
            if (cooldownOverlay != null)
            {
                cooldownOverlay.gameObject.SetActive(normalizedFill > 0.001f);
                cooldownOverlay.fillAmount = Mathf.Clamp01(normalizedFill);
            }
        }

        private void ApplyVisualState(bool pressed)
        {
            if (targetGraphic != null)
            {
                targetGraphic.color = pressed ? pressedColor : normalColor;
            }

            if (innerButtonRect == null && _rectTransform != null)
            {
                _rectTransform.localScale = pressed ? _originalScale * pressedScale : _originalScale;
            }
        }

        public void ResetState()
        {
            _activePointerId = -999;
            _isPressed = false;
            _wasPressedThisFrame = false;
            if (innerButtonRect != null)
            {
                _targetInnerPos = Vector2.zero;
                _currentInnerPos = Vector2.zero;
                innerButtonRect.anchoredPosition = Vector2.zero;
                _targetInnerScale = _originalInnerScale;
                _currentInnerScale = _originalInnerScale;
                innerButtonRect.localScale = _originalInnerScale;
            }
            ApplyVisualState(false);
        }

        private void OnDisable()
        {
            ResetState();
        }
    }
}
