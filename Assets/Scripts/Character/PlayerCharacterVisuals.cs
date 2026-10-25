using UnityEngine;
using Unity.Netcode;

namespace Shootopia
{
    /// <summary>
    /// Synchronizes the selected character visual model (0 = Vanguard [Male], 1 = Valkyrie [Female])
    /// across all network clients in real-time.
    /// Ensures only the active model's SkinnedMeshRenderers are enabled for rendering and invincibility overlays.
    /// </summary>
    public class PlayerCharacterVisuals : NetworkBehaviour
    {
        [Header("Model Renderers")]
        [SerializeField] private GameObject maleBody;   // Body01
        [SerializeField] private GameObject maleHead;   // Head01
        [SerializeField] private GameObject femaleBody; // Body02
        [SerializeField] private GameObject femaleHead; // Head02

        private readonly NetworkVariable<int> _activeModelIndex = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public int ActiveModelIndex => _activeModelIndex.Value;

        private void Awake()
        {
            FindRenderersIfNull();
        }

        private void FindRenderersIfNull()
        {
            if (maleBody == null) maleBody = transform.Find("ModelRoot/DefaultMalePolyart/Body01")?.gameObject;
            if (maleHead == null) maleHead = transform.Find("ModelRoot/DefaultMalePolyart/Head01")?.gameObject;
            if (femaleBody == null) femaleBody = transform.Find("ModelRoot/DefaultMalePolyart/Body02")?.gameObject;
            if (femaleHead == null) femaleHead = transform.Find("ModelRoot/DefaultMalePolyart/Head02")?.gameObject;
        }

        public override void OnNetworkSpawn()
        {
            FindRenderersIfNull();
            _activeModelIndex.OnValueChanged += OnModelIndexChanged;

            // Apply current network state
            ApplyModelVisuals(_activeModelIndex.Value);

            // If local player, broadcast our chosen profile model
            if (IsOwner)
            {
                int localChoice = PlayerProfileManager.Instance != null 
                    ? PlayerProfileManager.Instance.SelectedModelIndex 
                    : PlayerPrefs.GetInt("Shootopia_SelectedModelIndex", 0);

                SetModelServerRpc(localChoice);
            }
        }

        public override void OnNetworkDespawn()
        {
            _activeModelIndex.OnValueChanged -= OnModelIndexChanged;
        }

        [Rpc(SendTo.Server)]
        private void SetModelServerRpc(int index)
        {
            _activeModelIndex.Value = Mathf.Clamp(index, 0, 1);
        }

        private void OnModelIndexChanged(int previous, int current)
        {
            ApplyModelVisuals(current);
        }

        public void ApplyModelVisuals(int modelIndex)
        {
            FindRenderersIfNull();
            bool isValkyrie = (modelIndex == 1);

            if (maleBody != null) maleBody.SetActive(!isValkyrie);
            if (maleHead != null) maleHead.SetActive(!isValkyrie);
            if (femaleBody != null) femaleBody.SetActive(isValkyrie);
            if (femaleHead != null) femaleHead.SetActive(isValkyrie);
        }
    }
}
