using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Shootopia
{
    /// <summary>
    /// Delivers modern micro-animations, button hover springs, punch scales,
    /// modal bounce transitions, and audio feedback across all UGUI interfaces.
    /// </summary>
    public partial class UIJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Hover & Click Spring")]
        [SerializeField] private bool enableHoverScale = true;
        [SerializeField] private float hoverScale = 1.05f;
        [SerializeField] private float pressScale = 0.92f;
        [SerializeField] private float springSpeed = 16f;

        [Header("Audio Feedback")]
        [SerializeField] private bool playAudio = true;
        [SerializeField] private AudioClip hoverClip;
        [SerializeField] private AudioClip clickClip;

        private Vector3 _baseScale;
        private Vector3 _targetScale;
        private Coroutine _scaleRoutine;
        private Selectable _selectable;

        private void Awake()
        {
            _baseScale = transform.localScale;
            _targetScale = _baseScale;
            _selectable = GetComponent<Selectable>();
        }

        private void OnEnable()
        {
            transform.localScale = _baseScale;
            _targetScale = _baseScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_selectable != null && !_selectable.interactable) return;

            if (enableHoverScale)
            {
                _targetScale = _baseScale * hoverScale;
                StartScaleTransition();
            }

            if (playAudio)
            {
                PlayHoverSound(hoverClip);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (enableHoverScale)
            {
                _targetScale = _baseScale;
                StartScaleTransition();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_selectable != null && !_selectable.interactable) return;

            _targetScale = _baseScale * pressScale;
            StartScaleTransition();

            if (playAudio)
            {
                PlayClickSound(clickClip);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (enableHoverScale && eventData.hovered.Contains(gameObject))
            {
                _targetScale = _baseScale * hoverScale;
            }
            else
            {
                _targetScale = _baseScale;
            }
            StartScaleTransition();
        }

        private void StartScaleTransition()
        {
            if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);
            _scaleRoutine = StartCoroutine(ScaleRoutine());
        }

        private IEnumerator ScaleRoutine()
        {
            while (Vector3.Distance(transform.localScale, _targetScale) > 0.002f)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.unscaledDeltaTime * springSpeed);
                yield return null;
            }
            transform.localScale = _targetScale;
            _scaleRoutine = null;
        }
    }
}
