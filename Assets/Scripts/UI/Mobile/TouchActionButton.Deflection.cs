using UnityEngine;
using UnityEngine.EventSystems;

namespace Shootopia
{
    public partial class TouchActionButton
    {
        private void InitDeflection()
        {
            if (innerButtonRect != null)
            {
                _originalInnerScale = innerButtonRect.localScale;
                _currentInnerScale = _originalInnerScale;
                _targetInnerScale = _originalInnerScale;
                _currentInnerPos = innerButtonRect.anchoredPosition;
                _targetInnerPos = innerButtonRect.anchoredPosition;
            }
        }

        private void UpdateDeflection()
        {
            if (innerButtonRect == null) return;

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

        private void HandlePointerDownDeflection(PointerEventData eventData)
        {
            if (innerButtonRect != null && _rectTransform != null)
            {
                var cam = eventData.pressEventCamera != null ? eventData.pressEventCamera : _uiCamera;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, eventData.position, cam, out Vector2 localPoint))
                {
                    _touchStartLocalPos = localPoint;
                }
                else
                {
                    _touchStartLocalPos = Vector2.zero;
                }
                _targetInnerScale = _originalInnerScale * innerPressedScale;
            }
        }

        private void HandleDragDeflection(PointerEventData eventData)
        {
            if (innerButtonRect != null && _rectTransform != null)
            {
                var cam = eventData.pressEventCamera != null ? eventData.pressEventCamera : _uiCamera;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, eventData.position, cam, out Vector2 currentLocalPos))
                {
                    Vector2 deflection = currentLocalPos - _touchStartLocalPos;
                    _targetInnerPos = Vector2.ClampMagnitude(deflection, maxDeflectionDistance);
                }
            }
        }

        private void HandlePointerUpDeflection()
        {
            if (innerButtonRect != null)
            {
                _targetInnerPos = Vector2.zero;
                _targetInnerScale = _originalInnerScale;
            }
        }

        private void ResetDeflectionState()
        {
            if (innerButtonRect != null)
            {
                _targetInnerPos = Vector2.zero;
                _currentInnerPos = Vector2.zero;
                innerButtonRect.anchoredPosition = Vector2.zero;
                _targetInnerScale = _originalInnerScale;
                _currentInnerScale = _originalInnerScale;
                innerButtonRect.localScale = _originalInnerScale;
            }
        }
    }
}

