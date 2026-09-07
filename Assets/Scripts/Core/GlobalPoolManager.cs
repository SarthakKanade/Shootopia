using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// High-performance centralized Object Pool.
    /// Eliminates GC heap churn and runtime hitching by pre-warming,
    /// reusing, and recycling projectiles, impacts, and combat VFX.
    /// </summary>
    public class GlobalPoolManager : MonoBehaviour
    {
        private static GlobalPoolManager _instance;
        public static GlobalPoolManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<GlobalPoolManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("GlobalPoolManager");
                        _instance = go.AddComponent<GlobalPoolManager>();
                        if (Application.isPlaying) DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        private readonly Dictionary<int, Queue<GameObject>> _pools = new Dictionary<int, Queue<GameObject>>();
        private Transform _poolRoot;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                if (Application.isPlaying) Destroy(gameObject);
                else DestroyImmediate(gameObject);
                return;
            }

            _instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            EnsurePoolRoot();
        }

        private void EnsurePoolRoot()
        {
            if (_poolRoot == null)
            {
                var rootGo = new GameObject("PoolContainer");
                rootGo.transform.SetParent(transform, false);
                _poolRoot = rootGo.transform;
            }
        }

        /// <summary>
        /// Pre-instantiates a given count of prefab instances into the pool.
        /// </summary>
        public void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null || count <= 0) return;
            EnsurePoolRoot();

            int id = prefab.GetInstanceID();
            if (!_pools.TryGetValue(id, out var queue))
            {
                queue = new Queue<GameObject>(count);
                _pools[id] = queue;
            }

            for (int i = 0; i < count; i++)
            {
                var inst = Instantiate(prefab, _poolRoot);
                var tag = inst.GetComponent<PooledInstance>() ?? inst.AddComponent<PooledInstance>();
                tag.PrefabInstanceId = id;
                inst.SetActive(false);
                queue.Enqueue(inst);
            }
        }

        /// <summary>
        /// Retrieves an instance from the pool, or instantiates a new one if exhausted.
        /// </summary>
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;

            int id = prefab.GetInstanceID();
            if (_pools.TryGetValue(id, out var queue))
            {
                while (queue.Count > 0)
                {
                    var inst = queue.Dequeue();
                    if (inst != null)
                    {
                        inst.transform.SetParent(null);
                        inst.transform.position = position;
                        inst.transform.rotation = rotation;
                        inst.SetActive(true);
                        return inst;
                    }
                }
            }

            // Pool empty or non-existent: instantiate new
            var newInst = Instantiate(prefab, position, rotation);
            var tag = newInst.GetComponent<PooledInstance>() ?? newInst.AddComponent<PooledInstance>();
            tag.PrefabInstanceId = id;
            return newInst;
        }

        /// <summary>
        /// Returns a spawned instance to the pool.
        /// </summary>
        public void Despawn(GameObject instance)
        {
            if (instance == null) return;
            EnsurePoolRoot();

            var tag = instance.GetComponent<PooledInstance>();
            if (tag != null)
            {
                // Reset Rigidbody physics velocity if present
                var rb = instance.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                instance.SetActive(false);
                instance.transform.SetParent(_poolRoot, false);

                if (!_pools.TryGetValue(tag.PrefabInstanceId, out var queue))
                {
                    queue = new Queue<GameObject>();
                    _pools[tag.PrefabInstanceId] = queue;
                }

                queue.Enqueue(instance);
            }
            else
            {
                // Not a pooled instance: fall back to normal destruction
                if (Application.isPlaying) Destroy(instance);
                else DestroyImmediate(instance);
            }
        }

        /// <summary>
        /// Recycles an instance back into the pool after delaySeconds.
        /// </summary>
        public void DespawnDelayed(GameObject instance, float delaySeconds)
        {
            if (instance == null) return;
            if (delaySeconds <= 0f)
            {
                Despawn(instance);
                return;
            }

            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(DespawnRoutine(instance, delaySeconds));
            }
            else
            {
                Despawn(instance);
            }
        }

        private IEnumerator DespawnRoutine(GameObject instance, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (instance != null)
            {
                Despawn(instance);
            }
        }
    }
}
