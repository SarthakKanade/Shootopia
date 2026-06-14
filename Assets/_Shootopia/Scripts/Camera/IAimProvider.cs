using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// Abstraction that supplies aiming information to gameplay logic WITHOUT
    /// referencing Camera.main directly. This keeps the character controller
    /// camera-agnostic and ready for a future multiplayer layer (each client/
    /// server proxy can provide its own aim source).
    /// </summary>
    public interface IAimProvider
    {
        /// <summary>World-space point the player is aiming at.</summary>
        Vector3 AimPoint { get; }

        /// <summary>Horizontal (XZ) camera forward, normalized. Used for camera-relative movement.</summary>
        Vector3 CameraPlanarForward { get; }

        /// <summary>Horizontal (XZ) camera right, normalized.</summary>
        Vector3 CameraPlanarRight { get; }
    }
}
