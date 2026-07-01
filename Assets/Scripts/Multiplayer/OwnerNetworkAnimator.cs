using Unity.Netcode.Components;
using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// A client/owner-authoritative version of NetworkAnimator.
    /// Overrides OnIsServerAuthoritative to return false, allowing the owning client
    /// to trigger and drive animator state replication to other clients.
    /// </summary>
    [DisallowMultipleComponent]
    public class OwnerNetworkAnimator : NetworkAnimator
    {
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}
