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
        public LootChestType ChestType => chestType;
        [SerializeField] private LootTableSO lootTable;
        [SerializeField] private float cooldownSeconds = 7f;
        [SerializeField] private Transform previewPivot;
        [SerializeField] private FloatingItemPreview floatingPreview;

        [Header("Drop-In Fall Animation")]
        [Tooltip("Height above target ground point from which chest drops when appearing.")]
        [SerializeField] private float dropHeight = 0.6f;
        [Tooltip("Duration of the drop fall in seconds.")]
        [SerializeField] private float dropDuration = 0.22f;
        [SerializeField] private AudioClip landingSound;

        [Header("VFX & Audio Juice")]
        [SerializeField] private GameObject lootVFXPrefab;
        [SerializeField] private AudioClip lootSound;
        [SerializeField] private GameObject chestMeshObject;

        [Header("Fallback Health & Shield Attachments")]
        [SerializeField] private WeaponAttachSO healthBoxAttach;
        [SerializeField] private WeaponAttachSO shieldAttach;

        private bool _isDropping = false;
        private Coroutine _dropRoutine;
        private Vector3 _groundPosition = Vector3.zero;

        // Network State
        private readonly NetworkVariable<bool> _isAvailable = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _currentEntryIndex = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<Vector3> _netPosition = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        // Local non-networked fallback index for offline sandbox/testing
        private int _localOfflineIndex = 0;

        public bool IsAvailable => IsSpawned ? _isAvailable.Value : true;

        private void Awake()
        {
            transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);

            var rootRenderer = GetComponent<MeshRenderer>();
            if (rootRenderer != null) rootRenderer.enabled = false;

            if (previewPivot == null)
            {
                var p = transform.Find("PreviewPivot");
                if (p != null) previewPivot = p;
                else
                {
                    var pObj = new GameObject("PreviewPivot");
                    pObj.transform.SetParent(transform, false);
                    pObj.transform.localPosition = new Vector3(0f, 2.25f, 0f);
                    previewPivot = pObj.transform;
                }
            }

            if (floatingPreview == null && previewPivot != null)
            {
                floatingPreview = previewPivot.GetComponent<FloatingItemPreview>();
                if (floatingPreview == null)
                {
                    floatingPreview = previewPivot.gameObject.AddComponent<FloatingItemPreview>();
                }
            }

            ResetLidBones();
        }

        public void SetGroundPosition(Vector3 groundPos)
        {
            _groundPosition = groundPos;
            transform.position = groundPos;
            if (IsServer && _netPosition != null)
            {
                _netPosition.Value = groundPos;
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
                if (_groundPosition == Vector3.zero)
                {
                    _groundPosition = transform.position;
                }
                _netPosition.Value = _groundPosition;
                RollNewItemServer();
                _isAvailable.Value = true;
            }
            else
            {
                if (_netPosition.Value != Vector3.zero)
                {
                    _groundPosition = _netPosition.Value;
                    transform.position = _groundPosition;
                }
            }

            UpdateVisualState();

            // Trigger drop on client when it spawns into the world
            if (!IsServer && IsAvailable)
            {
                TriggerDropAnimation(_groundPosition);
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            _isAvailable.OnValueChanged -= OnAvailabilityChanged;
            _currentEntryIndex.OnValueChanged -= OnItemIndexChanged;
            _netPosition.OnValueChanged -= OnPositionChanged;
        }

        private void OnDisable()
        {
            if (_dropRoutine != null)
            {
                StopCoroutine(_dropRoutine);
                _dropRoutine = null;
            }
            if (_groundPosition != Vector3.zero)
            {
                transform.position = _groundPosition;
            }
            transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
            _isDropping = false;
        }

        private void OnPositionChanged(Vector3 previousValue, Vector3 newValue)
        {
            if (newValue != Vector3.zero)
            {
                _groundPosition = newValue;
                transform.position = newValue;
                if (IsAvailable && !_isOpening)
                {
                    TriggerDropAnimation(newValue);
                }
            }
        }

        private void Start()
        {
            if (_groundPosition == Vector3.zero)
            {
                _groundPosition = transform.position;
            }

            if (lootTable != null && lootTable.Entries != null)
            {
                foreach (var entry in lootTable.Entries)
                {
                    if (entry != null && entry.weaponAttach != null)
                    {
                        WeaponController.RegisterAttach(entry.weaponAttach);
                    }
                }
            }

            // For offline testing / sandbox scenes without active Netcode server
            if (!IsSpawned || !NetworkManager.Singleton.IsListening)
            {
                if (lootTable != null && lootTable.Entries.Count > 0)
                {
                    _localOfflineIndex = lootTable.GetRandomEntryIndex();
                }

                UpdateVisualState();

                if (IsAvailable)
                {
                    TriggerDropAnimation(_groundPosition);
                }
            }
            else
            {
                UpdateVisualState();
            }
        }

        private bool _isOpening = false;

        private void OnTriggerEnter(Collider other)
        {
            bool available = IsSpawned ? _isAvailable.Value : true;
            if (!available || _isOpening || _isDropping) return;

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
                StartCoroutine(OpenChestAndGrantLootRoutine(health.gameObject));
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
            if (!IsServer || !_isAvailable.Value || _isOpening) return;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null && NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetworkObjectId, out var playerNetObj))
            {
                StartCoroutine(OpenChestAndGrantLootRoutine(playerNetObj.gameObject));
            }
        }

        private IEnumerator OpenChestAndGrantLootRoutine(GameObject playerObj)
        {
            _isOpening = true;

            // Trigger open VFX, SFX, and lid opening animation across all clients
            if (IsSpawned)
            {
                TriggerLootEffectsClientRpc(transform.position);
            }
            else
            {
                TriggerLocalLootEffects(transform.position);
            }

            float duration = 0.6f;

            // Clear floating preview
            if (floatingPreview != null)
            {
                floatingPreview.ClearPreview();
            }

            yield return new WaitForSeconds(duration);

            // Grant loot item to player
            ExecuteLootForPlayer(playerObj);

            yield return new WaitForSeconds(0.2f);

            _isOpening = false;

            if (IsSpawned && IsServer)
            {
                _isAvailable.Value = false;
                StartCoroutine(CooldownRoutine());
            }
            else if (!IsSpawned)
            {
                StartCoroutine(OfflineCooldownRoutine());
            }
        }

        private System.Collections.Generic.List<Transform> GetLidBones()
        {
            var bones = new System.Collections.Generic.List<Transform>();
            if (chestMeshObject == null) return bones;

            var allTransforms = chestMeshObject.GetComponentsInChildren<Transform>(true);
            foreach (var t in allTransforms)
            {
                if (t.name == "UP" || t.name == "Left" || t.name == "Right" || t.name == "Rotation" || t.name == "Down")
                {
                    bones.Add(t);
                }
            }
            return bones;
        }

        private void ResetLidBones()
        {
            if (chestMeshObject != null)
            {
                var anim = chestMeshObject.GetComponentInChildren<Animator>(true);
                if (anim != null)
                {
                    anim.speed = 0f;
                    anim.Play(0, 0, 0f);
                    anim.Update(0f);
                    return;
                }
            }

            var lidBones = GetLidBones();
            foreach (var b in lidBones)
            {
                if (b.name == "Down") b.localRotation = Quaternion.Euler(89.98f, 0f, 0f);
                else if (b.name == "Rotation") b.localRotation = Quaternion.Euler(90f, 270f, 0f);
                else if (b.name == "UP" && chestType == LootChestType.Chest3_AdvancedWeapons) b.localRotation = Quaternion.Euler(89.98f, 0f, 0f);
                else if (b.name == "UP" && chestType == LootChestType.Chest2_TrapsAndStarters) b.localRotation = Quaternion.Euler(359.12f, 180f, 180f);
                else if (b.name == "UP") b.localRotation = Quaternion.Euler(0.62f, 180f, 180f);
                else if (b.name == "Left" || b.name == "Right") b.localRotation = Quaternion.Euler(0.62f, 180f, 180f);
            }
        }

        private void ExecuteLootForPlayer(GameObject playerObj)
        {
            if (playerObj == null) return;

            var health = playerObj.GetComponent<PlayerHealth>();
            var shield = playerObj.GetComponent<PlayerShield>();
            var weaponCtrl = playerObj.GetComponent<WeaponController>();

            if (health == null) return;

            int activeIndex = GetCurrentActiveIndex();
            if (lootTable == null || lootTable.Entries.Count == 0) return;
            if (activeIndex < 0 || activeIndex >= lootTable.Entries.Count) activeIndex = 0;
            var entry = lootTable.Entries[activeIndex];
            if (entry == null) return;

            if (chestType == LootChestType.Chest1_Defense)
            {
                var invis = playerObj.GetComponent<PlayerInvisibility>() ?? playerObj.GetComponentInParent<PlayerInvisibility>() ?? playerObj.GetComponentInChildren<PlayerInvisibility>();

                // Check itemType enum directly (with backwards-compatible name check fallback)
                LootItemType resolvedType = entry.itemType;
                if (resolvedType == LootItemType.Weapon)
                {
                    string itemName = (entry.displayName ?? entry.weaponAttach?.name ?? "").ToLower();
                    if (itemName.Contains("invis") || itemName.Contains("cloak")) resolvedType = LootItemType.Invisibility;
                    else if (itemName.Contains("shield")) resolvedType = LootItemType.Shield;
                    else resolvedType = LootItemType.FullHeal;
                }

                switch (resolvedType)
                {
                    case LootItemType.Invisibility:
                        if (invis != null)
                        {
                            invis.ActivateInvisibility(10f);
                            Debug.Log($"[LootBoxController] Player granted Invisibility Cloak from {chestType}!");
                        }
                        break;
                    case LootItemType.Shield:
                        if (shield != null)
                        {
                            shield.GrantShield(50f);
                            Debug.Log($"[LootBoxController] Player granted 50 HP Shield from {chestType}!");
                        }
                        break;
                    default:
                        // Health pack: heals player completely to full HP. Never activates shield!
                        health.Heal(health.GetMaxHp());
                        Debug.Log($"[LootBoxController] Player restored completely to MAX HP from {chestType}!");
                        break;
                }
            }
            else
            {
                if (weaponCtrl != null && entry.weaponAttach != null)
                {
                    WeaponController.RegisterAttach(entry.weaponAttach);
                    weaponCtrl.ReplaceCurrentWeapon(entry.weaponAttach);
                    Debug.Log($"[LootBoxController] Player acquired {entry.displayName ?? entry.weaponAttach.name} from {chestType}!");
                }
            }
        }

        public float GetCurrentCooldown()
        {
            if (LootBoxSpawnManager.Instance != null)
            {
                return LootBoxSpawnManager.Instance.GetChestCooldown();
            }
            return cooldownSeconds;
        }

        private IEnumerator CooldownRoutine()
        {
            yield return new WaitForSeconds(GetCurrentCooldown());

            if (IsServer)
            {
                RelocateToRandomSpawnPointServer();
                RollNewItemServer();
                _isAvailable.Value = true;
            }
        }

        public void ResetChestForMatchStart()
        {
            StopAllCoroutines();
            _dropRoutine = null;
            _isDropping = false;
            _isOpening = false;
            transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
            if (_groundPosition != Vector3.zero)
            {
                transform.position = _groundPosition;
            }
            ResetLidBones();

            if (IsServer)
            {
                RollNewItemServer();
                _isAvailable.Value = false;
                _isAvailable.Value = true;
            }
            else if (!IsSpawned)
            {
                if (lootTable != null && lootTable.Entries.Count > 0)
                {
                    _localOfflineIndex = lootTable.GetRandomEntryIndex();
                }
                UpdateVisualState();
                TriggerDropAnimation(_groundPosition);
            }
        }

        private IEnumerator OfflineCooldownRoutine()
        {
            yield return new WaitForSeconds(GetCurrentCooldown());

            if (lootTable != null && lootTable.Entries.Count > 0)
            {
                _localOfflineIndex = lootTable.GetRandomEntryIndex();
            }
            UpdateVisualState();
            TriggerDropAnimation(_groundPosition);
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
                _groundPosition = newPos;
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

        private int GetCurrentActiveIndex()
        {
            if (IsSpawned && _currentEntryIndex.Value >= 0)
            {
                return _currentEntryIndex.Value;
            }
            return _localOfflineIndex;
        }

        private void OnAvailabilityChanged(bool previousValue, bool newValue)
        {
            UpdateVisualState();
            if (!previousValue && newValue)
            {
                Vector3 targetPos = (_netPosition != null && _netPosition.Value != Vector3.zero) 
                    ? _netPosition.Value 
                    : (_groundPosition != Vector3.zero ? _groundPosition : transform.position);
                TriggerDropAnimation(targetPos);
            }
        }

        private void OnItemIndexChanged(int previousValue, int newValue)
        {
            UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            var rootRenderer = GetComponent<MeshRenderer>();
            if (rootRenderer != null) rootRenderer.enabled = false;

            bool available = IsSpawned ? _isAvailable.Value : true;

            if (chestMeshObject != null && chestMeshObject != gameObject)
            {
                chestMeshObject.SetActive(available);
            }
            else
            {
                var renderers = GetComponentsInChildren<MeshRenderer>(true);
                foreach (var r in renderers)
                {
                    if (r.gameObject == gameObject) { r.enabled = false; continue; }
                    if (floatingPreview != null && r.transform.IsChildOf(floatingPreview.transform)) continue;
                    r.enabled = available;
                }
            }

            if (!available)
            {
                if (floatingPreview != null) floatingPreview.ClearPreview();
                return;
            }

            ResetLidBones();

            if (floatingPreview == null) return;

            GameObject previewPrefab = null;

            if (lootTable != null && lootTable.Entries != null && lootTable.Entries.Count > 0)
            {
                int activeIndex = GetCurrentActiveIndex();
                if (activeIndex < 0 || activeIndex >= lootTable.Entries.Count) activeIndex = 0;

                var entry = lootTable.Entries[activeIndex];
                if (entry != null)
                {
                    previewPrefab = entry.customPreviewPrefab != null
                        ? entry.customPreviewPrefab
                        : (entry.weaponAttach != null ? entry.weaponAttach.visualPrefab : null);
                }
            }
            else if (chestType == LootChestType.Chest1_Defense)
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
            TriggerLocalLootEffects(lootPosition);
        }

        private void TriggerLocalLootEffects(Vector3 lootPosition)
        {
            if (chestMeshObject != null)
            {
                var anim = chestMeshObject.GetComponentInChildren<Animator>(true);
                if (anim != null)
                {
                    anim.speed = 1.5f;
                    anim.Play(0, 0, 0f);
                }
            }

            if (floatingPreview != null)
            {
                floatingPreview.ClearPreview();
            }

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

        public void TriggerDropAnimation(Vector3 targetGroundPos)
        {
            if (!gameObject.activeInHierarchy) return;

            if (targetGroundPos != Vector3.zero)
            {
                _groundPosition = targetGroundPos;
            }
            else if (_groundPosition == Vector3.zero)
            {
                _groundPosition = transform.position;
            }

            if (_dropRoutine != null)
            {
                StopCoroutine(_dropRoutine);
                _dropRoutine = null;
            }
            _dropRoutine = StartCoroutine(DropAnimationRoutine(_groundPosition));
        }

        private IEnumerator DropAnimationRoutine(Vector3 targetGroundPos)
        {
            _isDropping = true;
            Vector3 baseScale = new Vector3(0.75f, 0.75f, 0.75f);
            _groundPosition = targetGroundPos;
            Vector3 startPos = targetGroundPos + Vector3.up * dropHeight;
            transform.position = startPos;
            transform.localScale = baseScale;

            float elapsed = 0f;
            float duration = Mathf.Max(0.08f, dropDuration);

            // Gravity fall with acceleration (ease-in: t * t)
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easeIn = t * t;
                transform.position = Vector3.Lerp(startPos, targetGroundPos, easeIn);
                yield return null;
            }

            transform.position = targetGroundPos;

            // Landing sound
            if (landingSound != null)
            {
                AudioSource.PlayClipAtPoint(landingSound, targetGroundPos, 0.7f);
            }

            // Impact juice: quick squash on landing
            float squashDuration = 0.05f;
            elapsed = 0f;
            Vector3 squashScale = new Vector3(baseScale.x * 1.10f, baseScale.y * 0.75f, baseScale.z * 1.10f);
            while (elapsed < squashDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / squashDuration);
                transform.localScale = Vector3.Lerp(baseScale, squashScale, t);
                yield return null;
            }

            // Slight rebound
            float reboundDuration = 0.07f;
            elapsed = 0f;
            Vector3 reboundScale = new Vector3(baseScale.x * 0.94f, baseScale.y * 1.08f, baseScale.z * 0.94f);
            while (elapsed < reboundDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / reboundDuration);
                transform.localScale = Vector3.Lerp(squashScale, reboundScale, t);
                yield return null;
            }

            // Settle to base scale
            float settleDuration = 0.05f;
            elapsed = 0f;
            while (elapsed < settleDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / settleDuration);
                transform.localScale = Vector3.Lerp(reboundScale, baseScale, t);
                yield return null;
            }

            transform.position = targetGroundPos;
            transform.localScale = baseScale;
            _isDropping = false;
            _dropRoutine = null;
        }
    }
}
