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

        [Header("Player Ratio Scaling")]
        [Tooltip("Target number of chests per player ratio (e.g. 1.5 means 6 chests for 4 players).")]
        [SerializeField] private float chestsPerPlayerRatio = 1.5f;

        [Tooltip("Minimum number of chests to spawn regardless of player count (guarantees at least 1 of each type).")]
        [SerializeField] private int minChests = 4;

        [Tooltip("Maximum chests to spawn on map.")]
        [SerializeField] private int maxChests = 12;

        [Header("Chest Interval Scaling")]
        [Tooltip("Base cooldown in seconds when 1 player is connected.")]
        [SerializeField] private float baseCooldownSeconds = 25f;

        [Tooltip("Minimum cooldown seconds regardless of player count.")]
        [SerializeField] private float minCooldownSeconds = 12f;

        public float GetChestCooldown()
        {
            int players = GetCurrentPlayerCount();
            float cd = baseCooldownSeconds - ((players - 1) * 3f);
            return Mathf.Max(cd, minCooldownSeconds);
        }

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

        private void OnEnable()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            }
        }

        private void OnDisable()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            }
        }

        private void HandleClientConnected(ulong clientId)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                CheckAndAdjustChestsForPlayerCount();
            }
        }

        private void Start()
        {
            // In multiplayer, clients should not run local random spawning
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsServer)
            {
                return;
            }

            int playerCount = GetCurrentPlayerCount();
            int targetChests = CalculateTargetChestCount(playerCount, spawnPoints.Count > 0 ? spawnPoints.Count : 10);

            // Check if chests are already pre-placed in the scene hierarchy
            var existingChests = FindObjectsByType<LootBoxController>(FindObjectsSortMode.None);
            if (existingChests.Length >= targetChests && _activeChests.Count == 0)
            {
                Debug.Log($"[LootBoxSpawnManager] Found {existingChests.Length} pre-placed LootBoxControllers in scene. Skipping dynamic spawn.");
                return;
            }

            SpawnAllChests();
        }

        public int GetCurrentPlayerCount()
        {
            int count = 1;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                count = Mathf.Max(count, NetworkManager.Singleton.ConnectedClientsIds.Count);
            }

            if (SessionManager.Instance != null)
            {
                var session = SessionManager.Instance.GetCurrentSession();
                if (session != null && session.Players != null && session.Players.Count > 0)
                {
                    count = Mathf.Max(count, session.Players.Count);
                }
            }

            if (PlayerHealth.ActivePlayers.Count > 0)
            {
                count = Mathf.Max(count, PlayerHealth.ActivePlayers.Count);
            }

            return count;
        }

        public int CalculateTargetChestCount(int playerCount, int availableSpawnPointsCount)
        {
            // Exact 1:1:1 equal ratio: target count is quantized in sets of 3 (1 of each chest type)
            int sets = Mathf.Max(1, Mathf.RoundToInt((playerCount * chestsPerPlayerRatio) / 3f));
            int target = sets * 3;
            target = Mathf.Max(target, 3);
            target = Mathf.Min(target, maxChests);

            if (availableSpawnPointsCount > 0)
            {
                int maxSets = availableSpawnPointsCount / 3;
                if (maxSets >= 1)
                {
                    target = Mathf.Min(target, maxSets * 3);
                }
                else
                {
                    target = Mathf.Min(target, availableSpawnPointsCount);
                }
            }
            return target;
        }

        public void CheckAndAdjustChestsForPlayerCount()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsServer)
            {
                return;
            }

            _activeChests.RemoveAll(c => c == null);
            if (spawnPoints.Count == 0) GatherSceneSpawnPoints();

            int playerCount = GetCurrentPlayerCount();
            int targetCount = CalculateTargetChestCount(playerCount, spawnPoints.Count);

            if (_activeChests.Count < targetCount)
            {
                SpawnAdditionalChests(targetCount - _activeChests.Count);
            }
        }

        public void ResetAllChestsForMatch()
        {
            var chests = FindObjectsByType<LootBoxController>(FindObjectsSortMode.None);
            foreach (var chest in chests)
            {
                if (chest != null)
                {
                    chest.ResetChestForMatchStart();
                }
            }
        }

        public void SpawnAdditionalChests(int countToSpawn)
        {
            if (countToSpawn <= 0) return;

            if (chest1DefensePrefab == null || chest2TrapsPrefab == null || chest3AdvancedPrefab == null)
            {
                LoadDefaultPrefabs();
            }

            if (spawnPoints.Count == 0) GatherSceneSpawnPoints();

            var occupiedPositions = new List<Vector3>();
            var allChests = FindObjectsByType<LootBoxController>(FindObjectsSortMode.None);
            foreach (var c in allChests)
            {
                if (c != null) occupiedPositions.Add(c.transform.position);
            }

            var availablePoints = new List<Transform>();
            foreach (var pt in spawnPoints)
            {
                if (pt == null || IsPositionObstructed(pt.position)) continue;
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

            for (int i = 0; i < availablePoints.Count; i++)
            {
                int r = Random.Range(i, availablePoints.Count);
                var temp = availablePoints[i];
                availablePoints[i] = availablePoints[r];
                availablePoints[r] = temp;
            }

            GameObject[] prefabs = new GameObject[] { chest1DefensePrefab, chest2TrapsPrefab, chest3AdvancedPrefab };
            string[] names = new string[] { "SM_LootBox_01_Defense", "SM_LootBox_02_TrapsStarters", "SM_LootBox_03_AdvancedWeapons" };

            // Maintain equal 1:1:1 balance by picking whichever type currently has the least count among active chests
            int count1 = 0, count2 = 0, count3 = 0;
            foreach (var c in allChests)
            {
                if (c == null) continue;
                var lbc = c.GetComponent<LootBoxController>();
                if (lbc == null) continue;
                if (lbc.ChestType == LootChestType.Chest1_Defense) count1++;
                else if (lbc.ChestType == LootChestType.Chest2_TrapsAndStarters) count2++;
                else if (lbc.ChestType == LootChestType.Chest3_AdvancedWeapons) count3++;
            }

            int spawned = 0;
            for (int i = 0; i < availablePoints.Count && spawned < countToSpawn; i++)
            {
                int idx = 0;
                if (count2 <= count1 && count2 <= count3) idx = 1;
                else if (count3 <= count1 && count3 <= count2) idx = 2;
                else idx = 0;

                if (idx == 0) count1++;
                else if (idx == 1) count2++;
                else count3++;

                var pt = availablePoints[i];
                SpawnChestInstance(prefabs[idx], pt.position, names[idx]);
                spawned++;
            }
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
                if (c != null)
                {
                    if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsServer)
                    {
                        var netObj = c.GetComponent<NetworkObject>();
                        if (netObj != null && netObj.IsSpawned)
                        {
                            netObj.Despawn(true);
                            continue;
                        }
                    }
                    Destroy(c);
                }
            }
            _activeChests.Clear();

            if (chest1DefensePrefab == null || chest2TrapsPrefab == null || chest3AdvancedPrefab == null)
            {
                LoadDefaultPrefabs();
            }

            if (spawnPoints.Count == 0) GatherSceneSpawnPoints();

            var availablePoints = new List<Transform>();
            foreach (var pt in spawnPoints)
            {
                if (pt != null && !IsPositionObstructed(pt.position))
                {
                    availablePoints.Add(pt);
                }
            }
            if (availablePoints.Count == 0) availablePoints = new List<Transform>(spawnPoints);

            for (int i = 0; i < availablePoints.Count; i++)
            {
                int r = Random.Range(i, availablePoints.Count);
                var temp = availablePoints[i];
                availablePoints[i] = availablePoints[r];
                availablePoints[r] = temp;
            }

            int playerCount = GetCurrentPlayerCount();
            int targetCount = CalculateTargetChestCount(playerCount, availablePoints.Count);

            var toSpawn = new List<(GameObject prefab, string name)>();
            GameObject[] prefabs = new GameObject[] { chest1DefensePrefab, chest2TrapsPrefab, chest3AdvancedPrefab };
            string[] names = new string[] { "SM_LootBox_01_Defense", "SM_LootBox_02_TrapsStarters", "SM_LootBox_03_AdvancedWeapons" };

            // Guarantee strictly equal 1:1:1 distribution across all 3 types
            int sets = targetCount / 3;
            for (int s = 0; s < sets; s++)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (prefabs[i] != null) toSpawn.Add((prefabs[i], names[i]));
                }
            }
            int remainder = targetCount % 3;
            for (int i = 0; i < remainder; i++)
            {
                if (prefabs[i] != null) toSpawn.Add((prefabs[i], names[i]));
            }

            // Shuffle types order across spawn positions
            for (int i = 0; i < toSpawn.Count; i++)
            {
                int r = Random.Range(i, toSpawn.Count);
                var temp = toSpawn[i];
                toSpawn[i] = toSpawn[r];
                toSpawn[r] = temp;
            }

            for (int i = 0; i < toSpawn.Count && i < availablePoints.Count; i++)
            {
                var pt = availablePoints[i];
                if (pt == null) continue;
                SpawnChestInstance(toSpawn[i].prefab, pt.position, toSpawn[i].name);
            }

            Debug.Log($"[LootBoxSpawnManager] Spawned {toSpawn.Count} chests for {playerCount} players (Ratio: {chestsPerPlayerRatio:F2})");
        }

        private void SpawnChestInstance(GameObject prefab, Vector3 position, string name)
        {
            if (prefab == null) return;

            var inst = Instantiate(prefab, position, Quaternion.identity);
            inst.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
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

            Debug.Log($"[LootBoxSpawnManager] Spawned {name} (75% scale) at {position}");
        }

        public bool IsPositionObstructed(Vector3 position)
        {
            // 1. Check for enclosed roof/building above within 15 meters
            if (Physics.Raycast(position + Vector3.up * 0.2f, Vector3.up, out RaycastHit roofHit, 15f))
            {
                string roofName = roofHit.collider.name.ToLower();
                if (roofName.Contains("building") || roofName.Contains("station") || roofName.Contains("house") || roofName.Contains("roof") || roofName.Contains("ceilingbarrier"))
                {
                    return true;
                }
            }

            // 2. Check for overlapping obstacle colliders
            var colliders = Physics.OverlapSphere(position + Vector3.up * 0.5f, 0.5f);
            foreach (var col in colliders)
            {
                if (col.isTrigger) continue;
                string name = col.gameObject.name.ToLower();
                if (name.Contains("platform") || name.Contains("floor") || name.Contains("ground") || name.Contains("coal") || name.Contains("pedestal") || name.Contains("stairs") || name.Contains("bridge") || name.Contains("terrain") || name.Contains("road") || name.Contains("pavement"))
                {
                    continue;
                }
                return true;
            }
            return false;
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
                if (pt == null || IsPositionObstructed(pt.position)) continue;

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
                    if (pt == null || IsPositionObstructed(pt.position)) continue;
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
