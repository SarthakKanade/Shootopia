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
    }
}
