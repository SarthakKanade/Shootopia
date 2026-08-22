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
    public class UIJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
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

        // ──────────────────────── STATIC HELPERS ────────────────────────

        public static void PopIn(RectTransform target, CanvasGroup group = null, float duration = 0.22f)
        {
            if (target == null) return;
            var mono = target.GetComponentInParent<MonoBehaviour>();
            if (mono != null && mono.gameObject.activeInHierarchy)
            {
                mono.StartCoroutine(PopInRoutine(target, group, duration));
            }
            else
            {
                target.localScale = Vector3.one;
                if (group != null) { group.alpha = 1f; group.blocksRaycasts = true; group.interactable = true; }
            }
        }

        private static IEnumerator PopInRoutine(RectTransform target, CanvasGroup group, float duration)
        {
            target.gameObject.SetActive(true);
            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
            }

            target.localScale = Vector3.one * 0.75f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                
                float scaleT = Mathf.Sin(t * Mathf.PI * 0.5f);
                float overshoot = 1f + Mathf.Sin(t * Mathf.PI) * 0.12f;
                target.localScale = Vector3.one * Mathf.Lerp(0.75f, overshoot, scaleT);

                if (group != null)
                {
                    group.alpha = Mathf.Clamp01(t * 1.5f);
                }
                yield return null;
            }

            target.localScale = Vector3.one;
            if (group != null)
            {
                group.alpha = 1f;
                group.blocksRaycasts = true;
                group.interactable = true;
            }
        }

        public static void PopOut(RectTransform target, CanvasGroup group = null, float duration = 0.15f, Action onComplete = null)
        {
            if (target == null) return;
            var mono = target.GetComponentInParent<MonoBehaviour>();
            if (mono != null && mono.gameObject.activeInHierarchy)
            {
                mono.StartCoroutine(PopOutRoutine(target, group, duration, onComplete));
            }
            else
            {
                if (group != null) { group.alpha = 0f; group.blocksRaycasts = false; group.interactable = false; }
                target.gameObject.SetActive(false);
                onComplete?.Invoke();
            }
        }

        private static IEnumerator PopOutRoutine(RectTransform target, CanvasGroup group, float duration, Action onComplete)
        {
            if (group != null)
            {
                group.blocksRaycasts = false;
                group.interactable = false;
            }

            Vector3 startScale = target.localScale;
            float startAlpha = group != null ? group.alpha : 1f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                target.localScale = Vector3.Lerp(startScale, Vector3.one * 0.8f, t);

                if (group != null)
                {
                    group.alpha = Mathf.Lerp(startAlpha, 0f, t);
                }
                yield return null;
            }

            target.localScale = Vector3.one;
            target.gameObject.SetActive(false);
            onComplete?.Invoke();
        }

        public static void PunchScale(RectTransform target, float punchAmount = 0.15f)
        {
            if (target == null) return;
            var mono = target.GetComponentInParent<MonoBehaviour>();
            if (mono != null && mono.gameObject.activeInHierarchy)
            {
                mono.StartCoroutine(PunchRoutine(target, punchAmount));
            }
        }

        private static IEnumerator PunchRoutine(RectTransform target, float punchAmount)
        {
            Vector3 original = Vector3.one;
            float duration = 0.2f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float punch = Mathf.Sin(t * Mathf.PI) * punchAmount;
                target.localScale = original + Vector3.one * punch;
                yield return null;
            }
            target.localScale = original;
        }

        public static void PlayClickSound(AudioClip overrideClip = null)
        {
            if (AudioManager.Instance != null && overrideClip != null)
            {
                AudioManager.Instance.PlaySFX2D(overrideClip, 0.8f);
            }
        }

        public static void PlayHoverSound(AudioClip overrideClip = null)
        {
            if (AudioManager.Instance != null && overrideClip != null)
            {
                AudioManager.Instance.PlaySFX2D(overrideClip, 0.4f);
            }
        }
    }
}
