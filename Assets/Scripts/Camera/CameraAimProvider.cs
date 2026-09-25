using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// Supplies aim data to gameplay via <see cref="IAimProvider"/>. In the TPS
    /// over-the-shoulder model the aim point is a raycast from SCREEN CENTRE
    /// (the reticle) into the world. The camera planar basis is derived from the
    /// assigned render camera — never Camera.main.
    /// </summary>
    public class CameraAimProvider : MonoBehaviour, IAimProvider
    {
        [SerializeField] private Camera renderCamera;
        [SerializeField] private Transform player;
        [Tooltip("Max distance for the reticle aim raycast.")]
        [SerializeField] private float aimRayDistance = 200f;
        [Tooltip("Layers the reticle raycast can hit (exclude the player layer).")]
        [SerializeField] private LayerMask aimMask = ~0;
        [Tooltip("Viewport position of the crosshair (0-1). Default centre = (0.5, 0.5).")]
        [SerializeField] private Vector2 crosshairViewportPosition = new Vector2(0.5f, 0.5f);

        private Vector3 _aimPoint;

        public Vector3 AimPoint => _aimPoint;

        public Vector3 CameraPlanarForward
        {
            get
            {
                if (renderCamera == null) return Vector3.forward;
                Vector3 f = renderCamera.transform.forward;
                f.y = 0f;
                return f.sqrMagnitude > 0.0001f ? f.normalized : Vector3.forward;
            }
        }

        public Vector3 CameraPlanarRight
        {
            get
            {
                if (renderCamera == null) return Vector3.right;
                Vector3 r = renderCamera.transform.right;
                r.y = 0f;
                return r.sqrMagnitude > 0.0001f ? r.normalized : Vector3.right;
            }
        }

        private void Awake()
        {
            if (renderCamera == null) renderCamera = GetComponent<Camera>();
            if (renderCamera == null) renderCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (renderCamera == null)
            {
                renderCamera = Camera.main;
                if (renderCamera == null) return;
            }

            // Reticle = crosshair viewport position (default: centre of screen)
            Ray ray = renderCamera.ViewportPointToRay(new Vector3(crosshairViewportPosition.x, crosshairViewportPosition.y, 0f));

            if (Physics.Raycast(ray, out RaycastHit hit, aimRayDistance, aimMask, QueryTriggerInteraction.Ignore))
            {
                // Ensure hit is not on the player or behind the player's plane
                if (player != null && (hit.transform == player || hit.transform.IsChildOf(player) || Vector3.Dot(hit.point - player.position, ray.direction) < 0f))
                {
                    _aimPoint = ray.origin + ray.direction * aimRayDistance;
                }
                else
                {
                    _aimPoint = hit.point;
                }
            }
            else
            {
                _aimPoint = ray.origin + ray.direction * aimRayDistance;
            }
        }
    }
}
