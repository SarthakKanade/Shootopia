using UnityEngine;

namespace VelocityWars.Core
{
    /// <summary>
    /// Implement on all pickup objects.
    /// Replace direct subclass overrides with this contract.
    /// </summary>
    public interface IPickupable
    {
        /// <summary>
        /// The GameObject that triggered the pickup
        /// </summary>
        void OnPickup(GameObject picker);
    }
}
