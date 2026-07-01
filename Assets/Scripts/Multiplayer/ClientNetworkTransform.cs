using Unity.Netcode.Components;
using UnityEngine;

namespace Unity.Netcode.Components
{
    /// <summary>
    /// Custom NetworkTransform that overrides authority to allow client-authoritative
    /// movement and synchronization.
    /// </summary>
    [DisallowMultipleComponent]
    public class ClientNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}
