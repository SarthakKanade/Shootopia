using System.Collections;
using UnityEngine;

namespace Shootopia
{
    public class TimedDestroy : MonoBehaviour
    {
        public float delay = 1.0f;
        private Coroutine _despawnRoutine;

        private void OnEnable()
        {
            var pooled = GetComponent<PooledInstance>();
            if (pooled != null && GlobalPoolManager.Instance != null)
            {
                if (_despawnRoutine != null) StopCoroutine(_despawnRoutine);
                _despawnRoutine = StartCoroutine(RecycleRoutine(delay));
            }
            else
            {
                Destroy(gameObject, delay);
            }
        }

        private void OnDisable()
        {
            if (_despawnRoutine != null)
            {
                StopCoroutine(_despawnRoutine);
                _despawnRoutine = null;
            }
        }

        private IEnumerator RecycleRoutine(float t)
        {
            yield return new WaitForSeconds(t);
            _despawnRoutine = null;
            if (GlobalPoolManager.Instance != null)
            {
                GlobalPoolManager.Instance.Despawn(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
