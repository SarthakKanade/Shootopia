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
    /// Manages player pickup detection, network synchronization, and item previewing.
    /// </summary>
    public partial class LootBoxController : NetworkBehaviour
    {
        [Header("Chest Configuration")]
        [SerializeField] private LootChestType chestType = LootChestType.Chest1_Defense;
        public LootChestType ChestType => chestType;
        [SerializeField] private LootTableSO lootTable;
        [SerializeField] private float cooldownSeconds = 7f;
        [SerializeField] private Transform previewPivot;
        [SerializeField] private FloatingItemPreview floatingPreview;

        [Header("Drop-In Fall Animation")]
        [SerializeField] private float dropHeight = 0.6f;
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
        private bool _isOpening = false;
        private Coroutine _dropRoutine;
        private Vector3 _groundPosition = Vector3.zero;

        private readonly NetworkVariable<bool> _isAvailable = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _currentEntryIndex = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<Vector3> _netPosition = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private int _localOfflineIndex = 0;
        public bool IsAvailable => IsSpawned ? _isAvailable.Value : true;

        private void Awake()
        {
            transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
            var rootRenderer = GetComponent<MeshRenderer>();
            if (rootRenderer != null) rootRenderer.enabled = false;

            if (previewPivot == null)
            {
                previewPivot = transform.Find("PreviewPivot");
                if (previewPivot == null)
                {
                    var pObj = new GameObject("PreviewPivot");
                    pObj.transform.SetParent(transform, false);
                    pObj.transform.localPosition = new Vector3(0f, 2.25f, 0f);
                    previewPivot = pObj.transform;
                }
            }

            if (floatingPreview == null && previewPivot != null)
            {
                floatingPreview = previewPivot.GetComponent<FloatingItemPreview>() ?? previewPivot.gameObject.AddComponent<FloatingItemPreview>();
            }

            ResetLidBones();
        }

        public void SetGroundPosition(Vector3 groundPos)
        {
            _groundPosition = groundPos;
            transform.position = groundPos;
            if (IsServer && _netPosition != null) _netPosition.Value = groundPos;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _isAvailable.OnValueChanged += OnAvailabilityChanged;
            _currentEntryIndex.OnValueChanged += OnItemIndexChanged;
            _netPosition.OnValueChanged += OnPositionChanged;

            if (IsServer)
            {
                if (_groundPosition == Vector3.zero) _groundPosition = transform.position;
                _netPosition.Value = _groundPosition;
                RollNewItemServer();
                _isAvailable.Value = true;
            }
            else if (_netPosition.Value != Vector3.zero)
            {
                _groundPosition = _netPosition.Value;
                transform.position = _groundPosition;
            }

            UpdateVisualState();
            if (!IsServer && IsAvailable) TriggerDropAnimation(_groundPosition);
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
            if (_dropRoutine != null) { StopCoroutine(_dropRoutine); _dropRoutine = null; }
            if (_groundPosition != Vector3.zero) transform.position = _groundPosition;
            transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
            _isDropping = false;
        }

        private void OnPositionChanged(Vector3 previousValue, Vector3 newValue)
        {
            if (newValue != Vector3.zero)
            {
                _groundPosition = newValue;
                transform.position = newValue;
                if (IsAvailable && !_isOpening) TriggerDropAnimation(newValue);
            }
        }

        private void Start()
        {
            if (_groundPosition == Vector3.zero) _groundPosition = transform.position;

            if (lootTable?.Entries != null)
            {
                foreach (var entry in lootTable.Entries)
                {
                    if (entry?.weaponAttach != null) WeaponController.RegisterAttach(entry.weaponAttach);
                }
            }

            if (!IsSpawned || !NetworkManager.Singleton.IsListening)
            {
                if (lootTable != null && lootTable.Entries.Count > 0) _localOfflineIndex = lootTable.GetRandomEntryIndex();
                UpdateVisualState();
                if (IsAvailable) TriggerDropAnimation(_groundPosition);
            }
            else UpdateVisualState();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsAvailable || _isOpening || _isDropping) return;

            var health = other.GetComponentInParent<PlayerHealth>() ?? other.GetComponent<PlayerHealth>();
            if (health == null) return;

            if (IsServer || !IsSpawned) StartCoroutine(OpenChestAndGrantLootRoutine(health.gameObject));
            else
            {
                var networkObj = health.GetComponent<NetworkObject>();
                if (networkObj != null) RequestLootServerRpc(networkObj.NetworkObjectId);
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestLootServerRpc(ulong playerNetworkObjectId)
        {
            if (!IsServer || !_isAvailable.Value || _isOpening) return;
            if (NetworkManager.Singleton?.SpawnManager != null && NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetworkObjectId, out var playerNetObj))
            {
                StartCoroutine(OpenChestAndGrantLootRoutine(playerNetObj.gameObject));
            }
        }

        public void ResetChestForMatchStart()
        {
            StopAllCoroutines();
            _dropRoutine = null;
            _isDropping = false;
            _isOpening = false;
            transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
            if (_groundPosition != Vector3.zero) transform.position = _groundPosition;
            ResetLidBones();

            if (IsServer)
            {
                RollNewItemServer();
                _isAvailable.Value = false;
                _isAvailable.Value = true;
            }
            else if (!IsSpawned)
            {
                if (lootTable != null && lootTable.Entries.Count > 0) _localOfflineIndex = lootTable.GetRandomEntryIndex();
                UpdateVisualState();
                TriggerDropAnimation(_groundPosition);
            }
        }
    }
}
