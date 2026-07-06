using System.Collections.Generic;
using UnityEngine;

namespace Shootopia
{
    public enum SpawnPointType
    {
        FFA,
        TeamBlue,
        TeamRed
    }

    public class SpawnPointManager : MonoBehaviour
    {
        public static SpawnPointManager Instance { get; private set; }

        private readonly Dictionary<SpawnPointType, List<Transform>> _spawnPointsByType = new Dictionary<SpawnPointType, List<Transform>>();
        private readonly Dictionary<SpawnPointType, int> _lastUsedIndex = new Dictionary<SpawnPointType, int>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeSpawnPoints();
        }

        private void InitializeSpawnPoints()
        {
            _spawnPointsByType.Clear();
            _lastUsedIndex.Clear();

            // Find all spawn point components in the scene
            var scenePoints = Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
            foreach (var sp in scenePoints)
            {
                if (sp == null || sp.transform == null) continue;

                if (!_spawnPointsByType.ContainsKey(sp.Type))
                {
                    _spawnPointsByType[sp.Type] = new List<Transform>();
                    _lastUsedIndex[sp.Type] = -1;
                }
                _spawnPointsByType[sp.Type].Add(sp.transform);
            }

            Debug.Log($"[SpawnPointManager] Initialized. Found {scenePoints.Length} spawn points in the scene.");
        }

        public Transform GetSpawnPoint(SpawnPointType type)
        {
            if (!_spawnPointsByType.TryGetValue(type, out var points) || points.Count == 0)
            {
                Debug.LogWarning($"[SpawnPointManager] No spawn points registered for type {type}!");
                return transform;
            }

            int index = 0;
            if (points.Count > 1)
            {
                int lastIdx = _lastUsedIndex[type];
                do
                {
                    index = Random.Range(0, points.Count);
                } while (index == lastIdx);
            }
            else
            {
                index = 0;
            }

            _lastUsedIndex[type] = index;
            Transform selectedPoint = points[index];
            Debug.Log("[SpawnPointManager] Assigned spawn: " + selectedPoint.name);
            return selectedPoint;
        }
    }
}
