using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Shootopia
{
    public partial class LootBoxSpawnManager
    {
        private readonly List<Vector3> _reservedRelocationPositions = new List<Vector3>();

        public void GatherSceneSpawnPoints()
        {
            spawnPoints.Clear();
            var container = GameObject.Find("LootSpawnPoints");
            if (container != null)
            {
                for (int i = 0; i < container.transform.childCount; i++)
                    spawnPoints.Add(container.transform.GetChild(i));
            }

            if (spawnPoints.Count == 0)
            {
                foreach (var obj in GameObject.FindGameObjectsWithTag("LootSpawnPoint"))
                    spawnPoints.Add(obj.transform);
            }
        }

        private List<Transform> GetAvailableSpawnPoints()
        {
            var occupied = new List<Vector3>();
            foreach (var c in FindObjectsByType<LootBoxController>(FindObjectsSortMode.None))
            {
                if (c != null) occupied.Add(c.transform.position);
            }

            var available = new List<Transform>();
            foreach (var pt in spawnPoints)
            {
                if (pt == null || IsPositionObstructed(pt.position)) continue;
                bool isOccupied = false;
                foreach (var occ in occupied)
                {
                    if (Vector3.Distance(pt.position, occ) < 3f) { isOccupied = true; break; }
                }
                if (!isOccupied) available.Add(pt);
            }

            // Shuffle available points
            for (int i = 0; i < available.Count; i++)
            {
                int r = Random.Range(i, available.Count);
                var temp = available[i];
                available[i] = available[r];
                available[r] = temp;
            }

            return available;
        }

        public bool IsPositionObstructed(Vector3 position)
        {
            if (Physics.Raycast(position + Vector3.up * 0.2f, Vector3.up, out RaycastHit roofHit, 15f))
            {
                string roofName = roofHit.collider.name.ToLower();
                if (roofName.Contains("building") || roofName.Contains("station") || roofName.Contains("roof") || roofName.Contains("ceiling"))
                    return true;
            }

            var colliders = Physics.OverlapSphere(position + Vector3.up * 0.5f, 0.5f);
            foreach (var col in colliders)
            {
                if (col.isTrigger) continue;
                string n = col.gameObject.name.ToLower();
                if (n.Contains("floor") || n.Contains("ground") || n.Contains("terrain") || n.Contains("platform")) continue;
                return true;
            }
            return false;
        }

        public Vector3 GetRandomUnoccupiedSpawnPosition(Vector3 currentPos, LootBoxController requestingChest = null)
        {
            if (spawnPoints.Count == 0) GatherSceneSpawnPoints();
            if (spawnPoints.Count == 0) return currentPos;

            var occupied = new List<Vector3>();
            occupied.AddRange(_reservedRelocationPositions);

            foreach (var chest in FindObjectsByType<LootBoxController>(FindObjectsSortMode.None))
            {
                if (chest == null || chest == requestingChest) continue;
                if (Vector3.Distance(chest.transform.position, currentPos) < 0.5f) continue;
                occupied.Add(chest.transform.position);
            }

            var candidates = new List<Transform>();
            foreach (var pt in spawnPoints)
            {
                if (pt == null || IsPositionObstructed(pt.position)) continue;
                if (Vector3.Distance(pt.position, currentPos) < 3f) continue;

                bool isOcc = false;
                foreach (var occ in occupied)
                {
                    if (Vector3.Distance(pt.position, occ) < 3f) { isOcc = true; break; }
                }
                if (!isOcc) candidates.Add(pt);
            }

            if (candidates.Count == 0)
            {
                foreach (var pt in spawnPoints)
                {
                    if (pt == null || IsPositionObstructed(pt.position)) continue;
                    bool isOcc = false;
                    foreach (var occ in occupied)
                    {
                        if (Vector3.Distance(pt.position, occ) < 3f) { isOcc = true; break; }
                    }
                    if (!isOcc) candidates.Add(pt);
                }
            }

            if (candidates.Count > 0)
            {
                Vector3 chosen = candidates[Random.Range(0, candidates.Count)].position;
                _reservedRelocationPositions.Add(chosen);
                StartCoroutine(ReleaseReservedPosition(chosen));
                return chosen;
            }

            return currentPos;
        }

        private IEnumerator ReleaseReservedPosition(Vector3 pos)
        {
            yield return new WaitForSeconds(2.0f);
            _reservedRelocationPositions.Remove(pos);
        }
    }
}

