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
    public partial class TouchActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
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

            InitDeflection();
            ApplyVisualState(false);
        }

        private void Update()
        {
            UpdateDeflection();
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

            HandlePointerDownDeflection(eventData);

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

            HandleDragDeflection(eventData);

            if (allowDragToAim)
            {
                OnDragLook?.Invoke(eventData.delta);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _activePointerId) return;
            _activePointerId = -999;

            HandlePointerUpDeflection();

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
            ResetDeflectionState();
            ApplyVisualState(false);
        }

        private void OnDisable()
        {
            ResetState();
        }
    }
}
