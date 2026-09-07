using System.Collections;
using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// Component attached to pooled particle VFX.
    /// Automatically returns the GameObject to GlobalPoolManager when playback finishes.
    /// </summary>
    public class PooledParticleEffect : MonoBehaviour
    {
        [SerializeField] private float fallbackDuration = 2.0f;
        private ParticleSystem _particleSystem;
        private Coroutine _recycleRoutine;

        private void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
        }

        private void OnEnable()
        {
            float duration = fallbackDuration;
            if (_particleSystem != null)
            {
                var main = _particleSystem.main;
                duration = Mathf.Max(main.duration + main.startLifetime.constantMax, 0.5f);
                _particleSystem.Play();
            }

            if (_recycleRoutine != null) StopCoroutine(_recycleRoutine);
            _recycleRoutine = StartCoroutine(RecycleRoutine(duration));
        }

        private void OnDisable()
        {
            if (_recycleRoutine != null)
            {
                StopCoroutine(_recycleRoutine);
                _recycleRoutine = null;
            }
        }

        private IEnumerator RecycleRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (GlobalPoolManager.Instance != null)
            {
                GlobalPoolManager.Instance.Despawn(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
            _recycleRoutine = null;
        }
    }
}
