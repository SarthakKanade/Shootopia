using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// Per-frame snapshot of player intent. This is the boundary between raw
    /// device input and gameplay. A future netcode layer can serialize/replicate
    /// this struct instead of reading devices directly.
    /// </summary>
    public struct PlayerInputIntent
    {
        public Vector2 Move;     // normalized movement axis (camera-relative)
        public bool Fire;
        public bool AltFire;
        public bool Reload;
        public bool Dash;        // edge-triggered (true only on the press frame)
        public bool Roll;        // edge-triggered
        public bool Jump;        // edge-triggered
        public bool SwapWeapon;  // edge-triggered
        public bool ToggleShield; // edge-triggered (Y key for testing)
    }
}
