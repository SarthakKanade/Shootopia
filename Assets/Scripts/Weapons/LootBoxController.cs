using System.Collections;
using UnityEngine;
using Unity.Netcode;

namespace Shootopia
{
    public enum LootChestType
    {
        Chest1_Defense,
        Chest2_TrapsAndStarters,
        Chest3_AdvancedWeapons
    }

    /// <summary>
    /// Networked stationary Loot Box controller.
    /// Spawns a floating/rotating 3D item preview above the chest.
    /// Handles player acquisition, dynamic health/shield rules, weapon loadout replacement, and wave cooldowns.
    /// </summary>
    public class LootBoxController : NetworkBehaviour
    {
        [Header("Chest Configuration")]
        [SerializeField] private LootChestType chestType = LootChestType.Chest1_Defense;
        [SerializeField] private LootTableSO lootTable;
        [SerializeField] private float cooldownSeconds = 30f;
        [SerializeField] private Transform previewPivot;
        [SerializeField] private FloatingItemPreview floatingPreview;

        [Header("VFX & Audio Juice")]
        [SerializeField] private GameObject lootVFXPrefab;
        [SerializeField] private AudioClip lootSound;
        [SerializeField] private GameObject chestMeshObject;

        [Header("Fallback Health & Shield Attachments (For Chest 1 Preview)")]
        [SerializeField] private WeaponAttachSO healthBoxAttach;
        [SerializeField] private WeaponAttachSO shieldAttach;

        // Network State
        private readonly NetworkVariable<bool> _isAvailable = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _currentEntryIndex = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<Vector3> _netPosition = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public bool IsAvailable => _isAvailable.Value;

        private void Awake()
        {
            if (floatingPreview == null && previewPivot != null)
            {
                floatingPreview = previewPivot.GetComponent<FloatingItemPreview>();
                if (floatingPreview == null)
                {
                    floatingPreview = previewPivot.gameObject.AddComponent<FloatingItemPreview>();
                }
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _isAvailable.OnValueChanged += OnAvailabilityChanged;
            _currentEntryIndex.OnValueChanged += OnItemIndexChanged;
            _netPosition.OnValueChanged += OnPositionChanged;

            if (IsServer)
            {
                _netPosition.Value = transform.position;
                RollNewItemServer();
                _isAvailable.Value = true;
            }
            else
            {
                if (_netPosition.Value != Vector3.zero)
                {
                    transform.position = _netPosition.Value;
                }
            }

            UpdateVisualState();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            _isAvailable.OnValueChanged -= OnAvailabilityChanged;
            _currentEntryIndex.OnValueChanged -= OnItemIndexChanged;
            _netPosition.OnValueChanged -= OnPositionChanged;
        }

        private void OnPositionChanged(Vector3 previousValue, Vector3 newValue)
        {
            if (newValue != Vector3.zero)
            {
                transform.position = newValue;
            }
        }

        private void Start()
        {
            // OnNetworkSpawn already calls RollNewItemServer on the server.
            // For non-networked testing or late joins, just refresh visuals.
            UpdateVisualState();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isAvailable.Value) return;

            var health = other.GetComponentInParent<PlayerHealth>();
            if (health == null) health = other.GetComponent<PlayerHealth>();
            if (health == null)
            {
                var motor = other.GetComponentInParent<CharacterMotor>();
                if (motor != null) health = motor.GetComponent<PlayerHealth>();
            }
            if (health == null) return;

            if (IsServer || !IsSpawned)
            {
                ExecuteLootForPlayer(health.gameObject);
            }
            else
            {
                var networkObj = health.GetComponent<NetworkObject>();
                if (networkObj != null)
                {
                    RequestLootServerRpc(networkObj.NetworkObjectId);
                }
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestLootServerRpc(ulong playerNetworkObjectId)
        {
            if (!IsServer || !_isAvailable.Value) return;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null && NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetworkObjectId, out var playerNetObj))
            {
                ExecuteLootForPlayer(playerNetObj.gameObject);
            }
        }

        private void ExecuteLootForPlayer(GameObject playerObj)
        {
            if (playerObj == null || !_isAvailable.Value) return;

            var health = playerObj.GetComponent<PlayerHealth>();
            var shield = playerObj.GetComponent<PlayerShield>();
            var weaponCtrl = playerObj.GetComponent<WeaponController>();

            if (health == null) return;

            bool successfullyLooted = false;

            if (chestType == LootChestType.Chest1_Defense)
            {
                if (health.GetCurrentHp() < health.GetMaxHp())
                {
                    // Player has less than full health: heal player completely back to MAX HP
                    health.Heal(health.GetMaxHp());
                    Debug.Log($"[LootBoxController] Player restored to MAX HP from Chest 1!");
                    successfullyLooted = true;
                }
                else if (shield != null)
                {
                    // Player is already at full health: grant fresh 75 HP Shield
                    shield.GrantShield(75f);
                    Debug.Log($"[LootBoxController] Player granted fresh 75 HP Shield from Chest 1!");
                    successfullyLooted = true;
                }
            }
            else
            {
                // Chest 2 (Traps & Starters) and Chest 3 (Advanced Weapons)
                // Always acquirable: replaces whatever weapon is currently in hand
                if (lootTable != null && lootTable.Entries.Count > 0 && weaponCtrl != null)
                {
                    int index = _currentEntryIndex.Value;
                    if (index < 0 || index >= lootTable.Entries.Count)
                    {
                        index = 0;
                    }
                    var entry = lootTable.Entries[index];
                    if (entry != null && entry.weaponAttach != null)
                    {
                        // Replace the weapon in hand completely — fresh ammo/durability
                        weaponCtrl.ReplaceCurrentWeapon(entry.weaponAttach);
                        Debug.Log($"[LootBoxController] Player acquired {entry.displayName ?? entry.weaponAttach.name} from {chestType} (replaced weapon in hand)!");
                        successfullyLooted = true;
                    }
                }
            }

            if (successfullyLooted)
            {
                TriggerLootEffectsClientRpc(playerObj.transform.position);
                _isAvailable.Value = false;
                StartCoroutine(CooldownRoutine());
            }
        }

        private IEnumerator CooldownRoutine()
        {
            yield return new WaitForSeconds(cooldownSeconds);

            if (IsServer)
            {
                RelocateToRandomSpawnPointServer();
                RollNewItemServer();
                _isAvailable.Value = true;
            }
        }

        private void RelocateToRandomSpawnPointServer()
        {
            Vector3 newPos = transform.position;
            if (LootBoxSpawnManager.Instance != null)
            {
                newPos = LootBoxSpawnManager.Instance.GetRandomUnoccupiedSpawnPosition(transform.position, this);
            }
            else
            {
                var spawnObjects = GameObject.FindGameObjectsWithTag("LootSpawnPoint");
                if (spawnObjects == null || spawnObjects.Length == 0)
                {
                    var allObjs = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                    var list = new System.Collections.Generic.List<GameObject>();
                    foreach (var obj in allObjs)
                    {
                        if (obj != null && obj.name.StartsWith("LootSpawnPoint"))
                        {
                            list.Add(obj);
                        }
                    }
                    spawnObjects = list.ToArray();
                }

                if (spawnObjects != null && spawnObjects.Length > 0)
                {
                    var otherChests = FindObjectsByType<LootBoxController>(FindObjectsSortMode.None);
                    var validCandidates = new System.Collections.Generic.List<GameObject>();
                    foreach (var sp in spawnObjects)
                    {
                        if (sp == null) continue;
                        bool occupied = false;
                        foreach (var oc in otherChests)
                        {
                            if (oc != null && oc != this && Vector3.Distance(sp.transform.position, oc.transform.position) < 3f)
                            {
                                occupied = true;
                                break;
                            }
                        }
                        if (!occupied) validCandidates.Add(sp);
                    }

                    if (validCandidates.Count > 0)
                    {
                        newPos = validCandidates[Random.Range(0, validCandidates.Count)].transform.position;
                    }
                }
            }

            if (newPos != Vector3.zero)
            {
                _netPosition.Value = newPos;
                transform.position = newPos;
                Debug.Log($"[LootBoxController] {chestType} relocated to new spawn point at {newPos}");
            }
        }

        private void RollNewItemServer()
        {
            if (lootTable != null && lootTable.Entries.Count > 0)
            {
                int index = lootTable.GetRandomEntryIndex();
                _currentEntryIndex.Value = Mathf.Max(0, index);
            }
            else
            {
                _currentEntryIndex.Value = 0;
            }
        }

        private void OnAvailabilityChanged(bool previousValue, bool newValue)
        {
            UpdateVisualState();
        }

        private void OnItemIndexChanged(int previousValue, int newValue)
        {
            UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            if (chestMeshObject != null && chestMeshObject != gameObject)
            {
                chestMeshObject.SetActive(_isAvailable.Value);
            }
            else
            {
                var renderers = GetComponentsInChildren<MeshRenderer>(true);
                foreach (var r in renderers)
                {
                    if (floatingPreview != null && r.transform.IsChildOf(floatingPreview.transform)) continue;
                    r.enabled = _isAvailable.Value;
                }
            }

            if (!_isAvailable.Value)
            {
                if (floatingPreview != null) floatingPreview.ClearPreview();
                return;
            }

            if (floatingPreview == null) return;

            GameObject previewPrefab = null;

            if (chestType == LootChestType.Chest1_Defense)
            {
                if (healthBoxAttach != null && healthBoxAttach.visualPrefab != null)
                {
                    previewPrefab = healthBoxAttach.visualPrefab;
                }
                else if (shieldAttach != null && shieldAttach.visualPrefab != null)
                {
                    previewPrefab = shieldAttach.visualPrefab;
                }
            }
            else
            {
                int targetIndex = _currentEntryIndex.Value >= 0 ? _currentEntryIndex.Value : 0;
                if (lootTable != null && targetIndex >= 0 && targetIndex < lootTable.Entries.Count)
                {
                    var entry = lootTable.Entries[targetIndex];
                    if (entry != null)
                    {
                        previewPrefab = entry.customPreviewPrefab != null ? entry.customPreviewPrefab : (entry.weaponAttach != null ? entry.weaponAttach.visualPrefab : null);
                    }
                }
            }

            if (previewPrefab != null)
            {
                floatingPreview.SetPreviewVisual(previewPrefab);
            }
            else
            {
                floatingPreview.ClearPreview();
            }
        }

        [Rpc(SendTo.Everyone)]
        private void TriggerLootEffectsClientRpc(Vector3 lootPosition)
        {
            if (lootSound != null)
            {
                AudioSource.PlayClipAtPoint(lootSound, lootPosition, 1f);
            }

            if (lootVFXPrefab != null)
            {
                var vfx = Instantiate(lootVFXPrefab, transform.position + Vector3.up * 1f, Quaternion.identity);
                Destroy(vfx, 3f);
            }
        }
    }
}
