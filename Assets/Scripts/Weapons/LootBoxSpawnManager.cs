using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Shootopia
{
    /// <summary>
    /// Manages Loot Box spawn locations and scaling in gameplay scenes.
    /// Orchestrates chest counts according to connected players.
    /// </summary>
    public partial class LootBoxSpawnManager : MonoBehaviour
    {
        public static LootBoxSpawnManager Instance { get; private set; }

        [Header("Chest Prefabs")]
        [SerializeField] private GameObject chest1DefensePrefab;
        [SerializeField] private GameObject chest2TrapsPrefab;
        [SerializeField] private GameObject chest3AdvancedPrefab;

        [Header("Player Ratio Scaling")]
        [SerializeField] private float chestsPerPlayerRatio = 2.5f;
        [SerializeField] private int minChests = 8;
        [SerializeField] private int maxChests = 20;

        [Header("Chest Interval Scaling")]
        [SerializeField] private float baseCooldownSeconds = 7.0f;
        [SerializeField] private float minCooldownSeconds = 3.5f;

        [Header("Spawn Locations")]
        [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

        private readonly List<GameObject> _activeChests = new List<GameObject>();

        public float GetChestCooldown()
        {
            int players = GetCurrentPlayerCount();
            return Mathf.Max(baseCooldownSeconds - ((players - 1) * 3f), minCooldownSeconds);
        }

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
            if (NetworkManager.Singleton != null) NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
        }

        private void OnDisable()
        {
            if (NetworkManager.Singleton != null) NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
        }

        private void HandleClientConnected(ulong clientId)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer) CheckAndAdjustChestsForPlayerCount();
        }

        private void Start()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsServer) return;

            int targetChests = CalculateTargetChestCount(GetCurrentPlayerCount(), spawnPoints.Count > 0 ? spawnPoints.Count : 10);
            var existingChests = FindObjectsByType<LootBoxController>(FindObjectsSortMode.None);

            if (existingChests.Length >= targetChests && _activeChests.Count == 0)
            {
                foreach (var c in existingChests) if (c != null) _activeChests.Add(c.gameObject);
                return;
            }

            SpawnAllChests();
        }

        public int GetCurrentPlayerCount()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && NetworkManager.Singleton.ConnectedClientsList != null)
                return Mathf.Max(1, NetworkManager.Singleton.ConnectedClientsList.Count);
            if (SessionManager.Instance != null && SessionManager.Instance.GetCurrentSession()?.Players != null)
                return Mathf.Max(1, SessionManager.Instance.GetCurrentSession().Players.Count);
            return 1;
        }

        public int CalculateTargetChestCount(int playerCount, int availableSpawnPointsCount)
        {
            int calculated = Mathf.RoundToInt(playerCount * chestsPerPlayerRatio);
            return Mathf.Clamp(calculated, minChests, Mathf.Min(maxChests, Mathf.Max(minChests, availableSpawnPointsCount)));
        }

        public void CheckAndAdjustChestsForPlayerCount()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsServer) return;

            int count = CalculateTargetChestCount(GetCurrentPlayerCount(), spawnPoints.Count > 0 ? spawnPoints.Count : 10);
            var current = FindObjectsByType<LootBoxController>(FindObjectsSortMode.None);
            if (current.Length < count) SpawnAdditionalChests(count - current.Length);
        }

        public void ResetAllChestsForMatch()
        {
            CheckAndAdjustChestsForPlayerCount();
            foreach (var chest in FindObjectsByType<LootBoxController>(FindObjectsSortMode.None))
            {
                if (chest != null) chest.ResetChestForMatchStart();
            }
        }

        public void SpawnAdditionalChests(int countToSpawn)
        {
            if (countToSpawn <= 0) return;
            if (chest1DefensePrefab == null) LoadDefaultPrefabs();
            if (spawnPoints.Count == 0) GatherSceneSpawnPoints();

            var available = GetAvailableSpawnPoints();
            GameObject[] prefabs = { chest1DefensePrefab, chest2TrapsPrefab, chest3AdvancedPrefab };
            string[] names = { "SM_LootBox_01_Defense", "SM_LootBox_02_TrapsStarters", "SM_LootBox_03_AdvancedWeapons" };

            int spawned = 0;
            for (int i = 0; i < available.Count && spawned < countToSpawn; i++)
            {
                int idx = Random.Range(0, 3);
                SpawnChestInstance(prefabs[idx], available[i].position, names[idx]);
                spawned++;
            }
        }

        public void SpawnAllChests()
        {
            foreach (var c in _activeChests)
            {
                if (c == null) continue;
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsServer)
                {
                    var netObj = c.GetComponent<NetworkObject>();
                    if (netObj != null && netObj.IsSpawned) { netObj.Despawn(true); continue; }
                }
                Destroy(c);
            }
            _activeChests.Clear();

            if (chest1DefensePrefab == null) LoadDefaultPrefabs();
            if (spawnPoints.Count == 0) GatherSceneSpawnPoints();

            var available = GetAvailableSpawnPoints();
            int targetCount = CalculateTargetChestCount(GetCurrentPlayerCount(), available.Count);

            GameObject[] prefabs = { chest1DefensePrefab, chest2TrapsPrefab, chest3AdvancedPrefab };
            string[] names = { "SM_LootBox_01_Defense", "SM_LootBox_02_TrapsStarters", "SM_LootBox_03_AdvancedWeapons" };

            for (int i = 0; i < targetCount && i < available.Count; i++)
            {
                int idx = Random.Range(0, 3);
                SpawnChestInstance(prefabs[idx], available[i].position, names[idx]);
            }
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
                ctrl.SetGroundPosition(position);
            }

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsServer)
            {
                var netObj = inst.GetComponent<NetworkObject>();
                if (netObj != null && !netObj.IsSpawned) netObj.Spawn();
            }

            if (ctrl != null) ctrl.TriggerDropAnimation(position);
        }

        private void LoadDefaultPrefabs()
        {
            if (chest1DefensePrefab == null) chest1DefensePrefab = Resources.Load<GameObject>("LootBox_Chest1_Defense");
            if (chest2TrapsPrefab == null) chest2TrapsPrefab = Resources.Load<GameObject>("LootBox_Chest2_Traps");
            if (chest3AdvancedPrefab == null) chest3AdvancedPrefab = Resources.Load<GameObject>("LootBox_Chest3_Weapons");
        }
    }
}
