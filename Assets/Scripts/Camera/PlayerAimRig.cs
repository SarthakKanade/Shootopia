using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// Drives the camera aim pivot (yaw + pitch) from mouse-look input. The
    /// Cinemachine ThirdPersonFollow camera tracks this pivot, so rotating it
    /// orbits the camera behind the player (classic TPS over-the-shoulder).
    /// The pivot is independent of the body's facing — the body follows the
    /// camera yaw via <see cref="CharacterMotor"/>, not the other way around.
    /// Pivot lives under the (non-rotating) Player root, so localRotation == world aim.
    /// </summary>
    public class PlayerAimRig : MonoBehaviour
    {
        [SerializeField] private PlayerInputRouter input;
        [Tooltip("The pivot transform the camera Follows (e.g. CameraTarget).")]
        [SerializeField] private Transform aimPivot;

        [Header("Sensitivity")]
        [SerializeField] private float yawSensitivity = 0.15f;
        [SerializeField] private float pitchSensitivity = 0.12f;
        [SerializeField] private float minPitch = -35f;  // look down
        [SerializeField] private float maxPitch = 70f;   // look up

        private float _yaw;
        private float _pitch;

        public float Yaw => _yaw;
        public float Pitch => _pitch;

        private void Awake()
        {
            if (aimPivot != null)
            {
                Vector3 e = aimPivot.localEulerAngles;
                _yaw = e.y;
                _pitch = e.x;
            }
        }

        private void Update()
        {
            if (input == null || aimPivot == null) return;

            Vector2 look = input.LookDelta;
            _yaw += look.x * yawSensitivity;
            _pitch -= look.y * pitchSensitivity;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

            aimPivot.localRotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }
    }
}
