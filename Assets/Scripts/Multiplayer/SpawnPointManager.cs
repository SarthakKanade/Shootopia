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
        private static SpawnPointManager _instance;
        public static SpawnPointManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Object.FindAnyObjectByType<SpawnPointManager>();
                    if (_instance != null)
                    {
                        _instance.InitializeSpawnPoints();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        private readonly Dictionary<SpawnPointType, List<Transform>> _spawnPointsByType = new Dictionary<SpawnPointType, List<Transform>>();
        private readonly Dictionary<SpawnPointType, int> _lastUsedIndex = new Dictionary<SpawnPointType, int>();

        private void Awake()
        {
            _instance = this;
            InitializeSpawnPoints();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public void InitializeSpawnPoints()
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
            if (_spawnPointsByType.Count == 0)
            {
                InitializeSpawnPoints();
            }

            if (!_spawnPointsByType.TryGetValue(type, out var points) || points.Count == 0)
            {
                // Fallback to ANY available spawn point list in scene
                foreach (var list in _spawnPointsByType.Values)
                {
                    if (list != null && list.Count > 0)
                    {
                        points = list;
                        break;
                    }
                }
            }

            if (points == null || points.Count == 0)
            {
                Debug.LogWarning($"[SpawnPointManager] No spawn points registered for type {type}!");
                return transform;
            }

            // Filter for clear spawn points (avoiding any points inside buildings, obstacles, or statues)
            var validPoints = points.FindAll(IsSpawnPointClear);
            var candidateList = validPoints.Count > 0 ? validPoints : points;

            int index = 0;
            if (candidateList.Count > 1)
            {
                int lastIdx = _lastUsedIndex.TryGetValue(type, out int idx) ? idx : -1;
                do
                {
                    index = Random.Range(0, candidateList.Count);
                } while (index == lastIdx);
            }
            else
            {
                index = 0;
            }

            _lastUsedIndex[type] = index;
            Transform selectedPoint = candidateList[index];
            Debug.Log("[SpawnPointManager] Assigned spawn: " + selectedPoint.name);
            return selectedPoint;
        }

        private static bool IsSpawnPointClear(Transform sp)
        {
            if (sp == null) return false;
            Vector3 pos = sp.position;

            // Check if player capsule body collides with non-trigger solids (excluding terrain/floors/platforms/roads)
            var overlaps = Physics.OverlapCapsule(pos + Vector3.up * 0.3f, pos + Vector3.up * 1.7f, 0.4f);
            foreach (var col in overlaps)
            {
                if (col.isTrigger) continue;
                string n = col.gameObject.name.ToLower();
                if (n.Contains("ground") || n.Contains("floor") || n.Contains("terrain") ||
                    n.Contains("road") || n.Contains("pavement") || n.Contains("platform"))
                {
                    continue;
                }
                return false;
            }

            // Check if directly underneath a closed building or statue
            if (Physics.Raycast(pos + Vector3.up * 40f, Vector3.down, out RaycastHit roofHit, 45f))
            {
                if (!roofHit.collider.isTrigger)
                {
                    string rName = roofHit.collider.name.ToLower();
                    if (rName.Contains("building") || rName.Contains("house") || rName.Contains("statue"))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
