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

            // Configure URP LineRenderer materials & visuals
            if (_lineRenderer.sharedMaterial == null || 
                _lineRenderer.sharedMaterial.shader.name.Contains("Default") || 
                _lineRenderer.sharedMaterial.shader.name.Contains("InternalErrorShader") ||
                _lineRenderer.sharedMaterial.shader.name.Contains("Standard"))
            {
                var mat = Resources.Load<Material>("DashedLineMat");
                if (mat != null)
                {
                    _lineRenderer.material = mat;
                }
                else
                {
                    var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Universal Render Pipeline/Particles/Unlit");
                    if (shader != null)
                    {
                        var m = new Material(shader);
                        m.color = new Color(0f, 0.95f, 1f, 0.9f);
                        _lineRenderer.material = m;
                    }
                }
            }

            _lineRenderer.startWidth = 0.09f;
            _lineRenderer.endWidth = 0.05f;
            _lineRenderer.textureMode = LineTextureMode.Tile;
            _lineRenderer.alignment = LineAlignment.View;
            _lineRenderer.numCapVertices = 4;
            _lineRenderer.numCornerVertices = 4;
            _lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _lineRenderer.receiveShadows = false;
            _lineRenderer.positionCount = 0;
            _lineRenderer.enabled = false;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(new Color(0.0f, 0.95f, 1.0f), 0.0f), 
                    new GradientColorKey(new Color(0.2f, 0.6f, 1.0f), 1.0f) 
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0.95f, 0.0f), 
                    new GradientAlphaKey(0.80f, 1.0f) 
                }
            );
            _lineRenderer.colorGradient = gradient;

            if (impactMarkerPrefab != null)
            {
                _impactMarkerInstance = Instantiate(impactMarkerPrefab);
                _impactMarkerInstance.SetActive(false);
            }
            else
            {
                CreateFallbackImpactMarker();
            }
        }

        private void CreateFallbackImpactMarker()
        {
            _impactMarkerInstance = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _impactMarkerInstance.name = "ImpactMarker_Fallback";
            Destroy(_impactMarkerInstance.GetComponent<Collider>());
            _impactMarkerInstance.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);

            var mr = _impactMarkerInstance.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                var reticleMat = Resources.Load<Material>("TrajectoryImpactReticle");
                if (reticleMat != null)
                {
                    mr.material = reticleMat;
                }
                else
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Universal Render Pipeline/Particles/Unlit");
                    if (shader != null)
                    {
                        Material mat = new Material(shader);
                        mat.SetFloat("_Surface", 1f);
                        mat.SetFloat("_Blend", 0f);
                        mat.color = new Color(0f, 0.95f, 1f, 0.85f);
                        mr.material = mat;
                    }
                }
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
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
                            if (hit.collider == null || hit.transform == null) continue;

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
                                _impactMarkerInstance.transform.position = hit.point + hit.normal * 0.025f;
                                _impactMarkerInstance.transform.rotation = Quaternion.LookRotation(-hit.normal);
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

        private void OnDisable()
        {
            HideTrajectory();
        }

        /// <summary>
        /// Hides trajectory line and impact marker.
        /// </summary>
        public void HideTrajectory()
        {
            if (_lineRenderer != null)
            {
                _lineRenderer.positionCount = 0;
                _lineRenderer.enabled = false;
            }
            if (_impactMarkerInstance != null)
            {
                _impactMarkerInstance.SetActive(false);
            }
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
