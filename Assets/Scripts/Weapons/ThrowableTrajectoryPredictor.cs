using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// Predicts and renders physics trajectory paths and landing impact markers for throwables in real time.
    /// Uses exact launch origin, velocity vectors, gravity, and step raycasts.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class ThrowableTrajectoryPredictor : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int maxSteps = 60;
        [SerializeField] private float timeStep = 0.05f;
        [SerializeField] private LayerMask collisionMask = ~0;
        [SerializeField] private GameObject impactMarkerPrefab;

        private LineRenderer _lineRenderer;
        private GameObject _impactMarkerInstance;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.useWorldSpace = true;

            // Configure LineRenderer visuals if not pre-configured
            _lineRenderer.startWidth = 0.08f;
            _lineRenderer.endWidth = 0.04f;
            _lineRenderer.positionCount = 0;
            _lineRenderer.enabled = false;

            if (impactMarkerPrefab != null)
            {
                _impactMarkerInstance = Instantiate(impactMarkerPrefab);
                _impactMarkerInstance.SetActive(false);
            }
            else
            {
                // Create fallback impact marker if prefab is not set
                CreateFallbackImpactMarker();
            }
        }

        private void CreateFallbackImpactMarker()
        {
            _impactMarkerInstance = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _impactMarkerInstance.name = "ImpactMarker_Fallback";
            Destroy(_impactMarkerInstance.GetComponent<Collider>());
            _impactMarkerInstance.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

            var mr = _impactMarkerInstance.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Material mat = new Material(Shader.Find("Sprites/Default"));
                mat.color = new Color(1f, 0.2f, 0.2f, 0.75f);
                mr.material = mat;
            }
            _impactMarkerInstance.SetActive(false);
        }

        /// <summary>
        /// Updates line renderer points and impact marker position based on origin position and launch velocity vector.
        /// </summary>
        public void UpdateTrajectory(Vector3 launchOrigin, Vector3 initialVelocity)
        {
            if (_lineRenderer == null) return;

            _lineRenderer.enabled = true;

            Vector3 currentPos = launchOrigin;
            Vector3 currentVel = initialVelocity;
            Vector3 gravity = Physics.gravity;

            var points = new System.Collections.Generic.List<Vector3> { currentPos };
            bool hitFound = false;

            // Ensure collision mask ignores Layer 2 (Ignore Raycast) and UI
            int effectiveMask = collisionMask & ~(1 << 2) & ~(1 << 5);

            for (int i = 0; i < maxSteps; i++)
            {
                Vector3 nextPos = currentPos + currentVel * timeStep + 0.5f * gravity * (timeStep * timeStep);
                Vector3 stepDelta = nextPos - currentPos;
                float stepDist = stepDelta.magnitude;

                if (stepDist > 0.001f)
                {
                    RaycastHit[] hits = Physics.RaycastAll(currentPos, stepDelta.normalized, stepDist, effectiveMask, QueryTriggerInteraction.Ignore);
                    if (hits != null && hits.Length > 0)
                    {
                        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                        for (int h = 0; h < hits.Length; h++)
                        {
                            var hit = hits[h];

                            // Ignore self/owner hierarchy (body colliders, weapons, attached shield instance)
                            if (hit.transform.root == transform.root || hit.collider.transform.IsChildOf(transform))
                                continue;

                            // Ignore any protection shield colliders
                            if (hit.collider.GetComponent<ShieldColliderReceiver>() != null ||
                                hit.collider.GetComponentInParent<PlayerShield>() != null ||
                                hit.collider.name.Contains("Shield"))
                                continue;

                            points.Add(hit.point);

                            if (_impactMarkerInstance != null)
                            {
                                _impactMarkerInstance.SetActive(true);
                                _impactMarkerInstance.transform.position = hit.point + hit.normal * 0.02f;
                                _impactMarkerInstance.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                            }

                            hitFound = true;
                            break;
                        }

                        if (hitFound) break;
                    }
                }

                currentPos = nextPos;
                currentVel += gravity * timeStep;
                points.Add(currentPos);
            }

            if (!hitFound && _impactMarkerInstance != null)
            {
                _impactMarkerInstance.SetActive(false);
            }

            _lineRenderer.positionCount = points.Count;
            _lineRenderer.SetPositions(points.ToArray());
        }

        /// <summary>
        /// Hides trajectory line and impact marker.
        /// </summary>
        public void HideTrajectory()
        {
            if (_lineRenderer != null) _lineRenderer.enabled = false;
            if (_impactMarkerInstance != null) _impactMarkerInstance.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_impactMarkerInstance != null)
            {
                Destroy(_impactMarkerInstance);
            }
        }
    }
}
