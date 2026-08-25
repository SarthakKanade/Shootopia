using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

namespace Shootopia
{
    /// <summary>
    /// Reads raw devices (New Input System) and produces a per-frame
    /// <see cref="PlayerInputIntent"/>. Gameplay code consumes the intent, never
    /// the devices directly. Touch on-screen controls (mobile) can be added later
    /// by writing into <see cref="ExternalIntent"/> without changing consumers.
    /// </summary>
    public class PlayerInputRouter : NetworkBehaviour
    {
        /// <summary>The resolved intent for the current frame.</summary>
        public PlayerInputIntent Intent { get; private set; }

        /// <summary>Screen-space aim position (mouse on PC). Used by the aim provider.</summary>
        public Vector2 ScreenAimPosition { get; private set; }

        /// <summary>True when aiming should be cursor/screen based (PC). False = stick/touch directional.</summary>
        public bool HasScreenAim { get; private set; }

        /// <summary>Per-frame look delta (mouse delta / right-stick) used to drive the TPS aim rig.</summary>
        public Vector2 LookDelta { get; private set; }

        /// <summary>Optional injected intent (e.g. mobile touch UI). When set, OR-ed with device input.</summary>
        public PlayerInputIntent? ExternalIntent { get; set; }

        /// <summary>Optional injected look delta (e.g. mobile right-side drag). Added to device look.</summary>
        public Vector2 ExternalLookDelta { get; set; }

        /// <summary>Disables all player controls (e.g., when sinking in lava).</summary>
        public bool DisableControls { get; set; } = false;

        [Header("Cursor")]
        [SerializeField] private bool lockCursor = true;
        [SerializeField] private float gamepadLookSpeed = 12f; // right-stick deg-ish per frame scale

        private void OnEnable()
        {
            UpdateCursorLock();
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void UpdateCursorLock()
        {
            if (CursorManager.IsCursorUnlocked || WaitingRoomController.IsLobbyMenuOpen || PauseMenuController.IsMenuPaused)
            {
                if (Cursor.lockState != CursorLockMode.None)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                return;
            }

            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            bool shouldLock = lockCursor && MapRegistry.IsGameplayMap(activeScene);

            if (shouldLock)
            {
                if (Cursor.lockState != CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
            else
            {
                if (Cursor.lockState != CursorLockMode.None)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
        }

        void Update()
        {
            if (!IsOwner) return;

            UpdateCursorLock();

            var intent = new PlayerInputIntent();

            // Gating input: freeze when lobby menu is open or match is in pre-match countdown hold
            bool isFrozen = WaitingRoomController.IsLobbyMenuOpen || PauseMenuController.IsMenuPaused;
            if (GameModeBase.Instance != null && GameModeBase.Instance.PreMatchCountdown.Value > 0)
            {
                isFrozen = true;
            }

            if (isFrozen || DisableControls)
            {
                LookDelta = Vector2.zero;
                if (Mouse.current != null)
                {
                    ScreenAimPosition = Mouse.current.position.ReadValue();
                    HasScreenAim = true;
                }
                Intent = intent;
                return;
            }

            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;
            Gamepad pad = Gamepad.current;

            // --- Movement ---
            Vector2 move = Vector2.zero;
            if (kb != null)
            {
                if (kb.wKey.isPressed) move.y += 1f;
                if (kb.sKey.isPressed) move.y -= 1f;
                if (kb.aKey.isPressed) move.x -= 1f;
                if (kb.dKey.isPressed) move.x += 1f;
            }
            if (pad != null) move += pad.leftStick.ReadValue();
            intent.Move = Vector2.ClampMagnitude(move, 1f);

            // --- Aim source ---
            if (mouse != null)
            {
                ScreenAimPosition = mouse.position.ReadValue();
                HasScreenAim = true;
            }
            else
            {
                HasScreenAim = false;
            }

            // --- Look delta (TPS mouse-look) ---
            Vector2 look = Vector2.zero;
            if (mouse != null) look += mouse.delta.ReadValue();
            if (pad != null) look += pad.rightStick.ReadValue() * gamepadLookSpeed;
            look += ExternalLookDelta;
            ExternalLookDelta = Vector2.zero;
            LookDelta = look;

            // --- Buttons (held) ---
            if (mouse != null && mouse.leftButton.isPressed) intent.Fire = true;
            if (pad != null && pad.rightTrigger.isPressed) intent.Fire = true;
            if (mouse != null && mouse.rightButton.isPressed) intent.AltFire = true;
            if (pad != null && pad.leftTrigger.isPressed) intent.AltFire = true;

            // --- Buttons (edge-triggered) ---
            if (kb != null)
            {
                if (kb.rKey.wasPressedThisFrame) intent.Reload = true;
                if (kb.leftShiftKey.wasPressedThisFrame) intent.Dash = true;
                if (kb.spaceKey.wasPressedThisFrame) intent.Jump = true;
                if (kb.oKey.wasPressedThisFrame) intent.Roll = true;
                if (kb.qKey.wasPressedThisFrame) intent.SwapWeapon = true;
                if (kb.yKey.wasPressedThisFrame) intent.ToggleShield = true;
            }
            if (pad != null)
            {
                if (pad.buttonWest.wasPressedThisFrame) intent.Reload = true;
                if (pad.buttonEast.wasPressedThisFrame) intent.Roll = true;
                if (pad.buttonSouth.wasPressedThisFrame) intent.Jump = true;
                if (pad.leftShoulder.wasPressedThisFrame) intent.Dash = true;
                if (pad.buttonNorth.wasPressedThisFrame) intent.SwapWeapon = true;
            }

            // --- Merge external (touch) intent if present ---
            if (ExternalIntent.HasValue)
            {
                var ext = ExternalIntent.Value;
                if (ext.Move.sqrMagnitude > intent.Move.sqrMagnitude) intent.Move = ext.Move;
                intent.Fire |= ext.Fire;
                intent.AltFire |= ext.AltFire;
                intent.Reload |= ext.Reload;
                intent.Dash |= ext.Dash;
                intent.Roll |= ext.Roll;
                intent.Jump |= ext.Jump;
                intent.SwapWeapon |= ext.SwapWeapon;
            }

            Intent = intent;
        }
    }
}
