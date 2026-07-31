using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Shootopia
{
    /// <summary>
    /// Manages Loot Box spawn locations in gameplay scenes.
    /// Instantiates 1 of each Chest Type (Defense, Traps & Starters, Advanced Weapons) at random spawn points on match start.
    /// </summary>
    public class LootBoxSpawnManager : MonoBehaviour
    {
        public static LootBoxSpawnManager Instance { get; private set; }

        [Header("Chest Prefabs")]
        [SerializeField] private GameObject chest1DefensePrefab;
        [SerializeField] private GameObject chest2TrapsPrefab;
        [SerializeField] private GameObject chest3AdvancedPrefab;

        [Header("Spawn Locations")]
        [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

        private readonly List<GameObject> _activeChests = new List<GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadDefaultPrefabs();
            GatherSceneSpawnPoints();
        }

        private void Start()
        {
            // In multiplayer, clients should not run local random spawning
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsServer)
            {
                return;
            }

            // Check if chests are already pre-placed in the scene hierarchy
            var existingChests = FindObjectsByType<LootBoxController>(FindObjectsSortMode.None);
            if (existingChests.Length >= 3)
            {
                Debug.Log($"[LootBoxSpawnManager] Found {existingChests.Length} pre-placed LootBoxControllers in scene. Skipping dynamic spawn.");
                return;
            }

            SpawnAllChests();
        }

        private void LoadDefaultPrefabs()
        {
            // Try Resources.Load first (works in builds)
            if (chest1DefensePrefab == null)
                chest1DefensePrefab = Resources.Load<GameObject>("LootBox_Chest1_Defense");
            if (chest2TrapsPrefab == null)
                chest2TrapsPrefab = Resources.Load<GameObject>("LootBox_Chest2_Traps");
            if (chest3AdvancedPrefab == null)
                chest3AdvancedPrefab = Resources.Load<GameObject>("LootBox_Chest3_Weapons");

#if UNITY_EDITOR
            // Editor-only fallback from Prefabs folder
            if (chest1DefensePrefab == null)
                chest1DefensePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LootBox_Chest1_Defense.prefab");
            if (chest2TrapsPrefab == null)
                chest2TrapsPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LootBox_Chest2_Traps.prefab");
            if (chest3AdvancedPrefab == null)
                chest3AdvancedPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LootBox_Chest3_Weapons.prefab");
#endif
        }

        public void GatherSceneSpawnPoints()
        {
            spawnPoints.Clear();
            var container = GameObject.Find("LootSpawnPoints");
            if (container != null)
            {
                for (int i = 0; i < container.transform.childCount; i++)
                {
                    spawnPoints.Add(container.transform.GetChild(i));
                }
            }

            if (spawnPoints.Count == 0)
            {
                var objs = GameObject.FindGameObjectsWithTag("LootSpawnPoint");
                foreach (var obj in objs)
                {
                    spawnPoints.Add(obj.transform);
                }
            }
        }

        public void SpawnAllChests()
        {
            // Destroy any existing runtime instantiated chests managed by this spawner
            foreach (var c in _activeChests)
            {
                if (c != null) Destroy(c);
            }
            _activeChests.Clear();

            if (spawnPoints.Count == 0) GatherSceneSpawnPoints();

            // Find any pre-placed or existing chests in scene
            var existingChests = FindObjectsByType<LootBoxController>(FindObjectsSortMode.None);
            var occupiedPositions = new List<Vector3>();
            foreach (var c in existingChests)
            {
                if (c != null && !_activeChests.Contains(c.gameObject))
                {
                    occupiedPositions.Add(c.transform.position);
                }
            }

            var availablePoints = new List<Transform>();
            foreach (var pt in spawnPoints)
            {
                if (pt == null) continue;
                bool isOccupied = false;
                foreach (var occPos in occupiedPositions)
                {
                    if (Vector3.Distance(pt.position, occPos) < 3f)
                    {
                        isOccupied = true;
                        break;
                    }
                }
                if (!isOccupied) availablePoints.Add(pt);
            }

            if (availablePoints.Count < 3) availablePoints = new List<Transform>(spawnPoints);

            // Spawn Chest 1 (Defense)
            if (chest1DefensePrefab != null && availablePoints.Count > 0)
            {
                int r = Random.Range(0, availablePoints.Count);
                var pt = availablePoints[r];
                availablePoints.RemoveAt(r);
                SpawnChestInstance(chest1DefensePrefab, pt.position, "SM_LootBox_01_Defense");
            }

            // Spawn Chest 2 (Traps & Starters)
            if (chest2TrapsPrefab != null && availablePoints.Count > 0)
            {
                int r = Random.Range(0, availablePoints.Count);
                var pt = availablePoints[r];
                availablePoints.RemoveAt(r);
                SpawnChestInstance(chest2TrapsPrefab, pt.position, "SM_LootBox_02_TrapsStarters");
            }

            // Spawn Chest 3 (Advanced Weapons)
            if (chest3AdvancedPrefab != null && availablePoints.Count > 0)
            {
                int r = Random.Range(0, availablePoints.Count);
                var pt = availablePoints[r];
                availablePoints.RemoveAt(r);
                SpawnChestInstance(chest3AdvancedPrefab, pt.position, "SM_LootBox_03_AdvancedWeapons");
            }
        }

        private void SpawnChestInstance(GameObject prefab, Vector3 position, string name)
        {
            if (prefab == null) return;

            var inst = Instantiate(prefab, position, Quaternion.identity);
            inst.name = name;
            _activeChests.Add(inst);

            var ctrl = inst.GetComponent<LootBoxController>();
            if (ctrl != null)
            {
                ctrl.gameObject.SetActive(true);
            }

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsServer)
            {
                var netObj = inst.GetComponent<NetworkObject>();
                if (netObj != null && !netObj.IsSpawned)
                {
                    netObj.Spawn();
                }
            }

            Debug.Log($"[LootBoxSpawnManager] Spawned {name} at {position}");
        }

        public Vector3 GetRandomUnoccupiedSpawnPosition(Vector3 currentPos, LootBoxController requestingChest = null)
        {
            if (spawnPoints.Count == 0) GatherSceneSpawnPoints();
            if (spawnPoints.Count == 0) return currentPos;

            // Gather positions of all active chests currently in scene (excluding the requesting chest)
            var activeChests = FindObjectsByType<LootBoxController>(FindObjectsSortMode.None);
            var occupiedPositions = new List<Vector3>();

            foreach (var chest in activeChests)
            {
                if (chest == null) continue;
                if (requestingChest != null && chest == requestingChest) continue;
                
                // If chest is currently at or near currentPos, ignore (it is relocating from here)
                if (Vector3.Distance(chest.transform.position, currentPos) < 0.5f) continue;

                occupiedPositions.Add(chest.transform.position);
            }

            var candidatePoints = new List<Transform>();
            foreach (var pt in spawnPoints)
            {
                if (pt == null) continue;

                // Skip spawn point if it is the chest's current (old) location
                if (Vector3.Distance(pt.position, currentPos) < 3f) continue;

                // Check if ANY OTHER active chest is currently occupying this spawn point
                bool isOccupied = false;
                foreach (var occPos in occupiedPositions)
                {
                    if (Vector3.Distance(pt.position, occPos) < 3f)
                    {
                        isOccupied = true;
                        break;
                    }
                }

                if (!isOccupied)
                {
                    candidatePoints.Add(pt);
                }
            }

            // Fallback: If all points were < 3f from old position, allow old position IF not occupied by another chest
            if (candidatePoints.Count == 0)
            {
                foreach (var pt in spawnPoints)
                {
                    if (pt == null) continue;
                    bool isOccupied = false;
                    foreach (var occPos in occupiedPositions)
                    {
                        if (Vector3.Distance(pt.position, occPos) < 3f)
                        {
                            isOccupied = true;
                            break;
                        }
                    }

                    if (!isOccupied)
                    {
                        candidatePoints.Add(pt);
                    }
                }
            }

            if (candidatePoints.Count > 0)
            {
                return candidatePoints[Random.Range(0, candidatePoints.Count)].position;
            }

            return currentPos;
        }
    }
}
