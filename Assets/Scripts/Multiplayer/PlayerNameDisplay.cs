using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shootopia
{
    [RequireComponent(typeof(NetworkObject))]
    public class PlayerNameDisplay : NetworkBehaviour
    {
        public NetworkVariable<FixedString64Bytes> DisplayName { get; private set; } =
            new NetworkVariable<FixedString64Bytes>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public override void OnNetworkSpawn()
        {
            DisplayName.OnValueChanged += OnDisplayNameChanged;
            if (!DisplayName.Value.IsEmpty)
            {
                RegisterNameToRegistry(DisplayName.Value.ToString());
            }
        }

        public override void OnNetworkDespawn()
        {
            DisplayName.OnValueChanged -= OnDisplayNameChanged;
        }

        private void OnDisplayNameChanged(FixedString64Bytes previousValue, FixedString64Bytes newValue)
        {
            if (!newValue.IsEmpty)
            {
                RegisterNameToRegistry(newValue.ToString());
            }
        }

        private void RegisterNameToRegistry(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                PlayerNameRegistry.Register(OwnerClientId, name);
            }
        }
    }
}
