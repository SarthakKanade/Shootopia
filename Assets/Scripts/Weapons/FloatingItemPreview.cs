using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// Displays a floating and continuously rotating 3D preview model above a loot box.
    /// Handles centering, proper bounds normalization, and stripping non-visual components.
    /// </summary>
    public class FloatingItemPreview : MonoBehaviour
    {
        [Header("Animation Settings")]
        [Tooltip("Degrees of rotation per second around the vertical axis.")]
        [SerializeField] private float rotationSpeed = 65f;

        [Tooltip("Vertical bobbing oscillation frequency.")]
        [SerializeField] private float bobSpeed = 2.2f;

        [Tooltip("Vertical bobbing oscillation height amplitude.")]
        [SerializeField] private float bobHeight = 0.07f;

        [Tooltip("Target visual scale dimension relative to the loot box.")]
        [SerializeField] private float targetWorldSize = 0.85f;

        [Tooltip("Vertical offset from the pivot anchor.")]
        [SerializeField] private Vector3 heightOffset = new Vector3(0f, 0.25f, 0f);

        private Vector3 _baseLocalPos;
        private GameObject _previewInstance;
        private float _timer;

        private void Awake()
        {
            InitializeBasePosition();
        }

        private void Start()
        {
            InitializeBasePosition();
        }

        private void InitializeBasePosition()
        {
            if (_baseLocalPos == Vector3.zero || _baseLocalPos.y < 1.35f)
            {
                float targetY = transform.localPosition.y < 1.35f ? 1.7f : transform.localPosition.y;
                _baseLocalPos = new Vector3(transform.localPosition.x, targetY, transform.localPosition.z);
            }
        }

        private void Update()
        {
            _timer += Time.deltaTime * bobSpeed;
            float yOffset = Mathf.Sin(_timer) * bobHeight;

            transform.localPosition = _baseLocalPos + new Vector3(0f, yOffset, 0f);
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
        }

        /// <summary>
        /// Clears any existing preview and instantiates a normalized, centered 3D preview.
        /// </summary>
        public void SetPreviewVisual(GameObject visualPrefab)
        {
            ClearPreview();

            if (visualPrefab == null) return;

            // Instantiate cosmetic copy under this pivot
            _previewInstance = Instantiate(visualPrefab, transform);
            _previewInstance.transform.localPosition = Vector3.zero;
            _previewInstance.transform.localRotation = Quaternion.identity;
            _previewInstance.transform.localScale = Vector3.one;

            // Disable physics, colliders, and gameplay scripts
            var colliders = _previewInstance.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = false;

            var rigidbodies = _previewInstance.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < rigidbodies.Length; i++) rigidbodies[i].isKinematic = true;

            var animators = _previewInstance.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++) animators[i].enabled = false;

            var lineRenderers = _previewInstance.GetComponentsInChildren<LineRenderer>(true);
            for (int i = 0; i < lineRenderers.Length; i++) lineRenderers[i].enabled = false;

            var trailRenderers = _previewInstance.GetComponentsInChildren<TrailRenderer>(true);
            for (int i = 0; i < trailRenderers.Length; i++) trailRenderers[i].enabled = false;

            var particles = _previewInstance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var pr = particles[i].GetComponent<ParticleSystemRenderer>();
                if (pr != null) pr.enabled = false;
            }

            var lights = _previewInstance.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < lights.Length; i++) lights[i].enabled = false;

            var audios = _previewInstance.GetComponentsInChildren<AudioSource>(true);
            for (int i = 0; i < audios.Length; i++) audios[i].enabled = false;

            var scripts = _previewInstance.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < scripts.Length; i++)
            {
                if (scripts[i] != null && scripts[i] != this)
                {
                    scripts[i].enabled = false;
                }
            }

            // Calculate precise composite bounds across only visual mesh renderers in preview local space
            var renderers = _previewInstance.GetComponentsInChildren<Renderer>(true);
            Bounds localBounds = new Bounds();
            bool hasBounds = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null || !(r is MeshRenderer || r is SkinnedMeshRenderer)) continue;
                r.enabled = true;

                Bounds rb = r.bounds;
                Vector3 center = rb.center;
                Vector3 extents = rb.extents;

                // Transform all 8 world bounding corners into local space to handle nested rotations
                Vector3[] corners = new Vector3[]
                {
                    center + new Vector3( extents.x,  extents.y,  extents.z),
                    center + new Vector3( extents.x,  extents.y, -extents.z),
                    center + new Vector3( extents.x, -extents.y,  extents.z),
                    center + new Vector3( extents.x, -extents.y, -extents.z),
                    center + new Vector3(-extents.x,  extents.y,  extents.z),
                    center + new Vector3(-extents.x,  extents.y, -extents.z),
                    center + new Vector3(-extents.x, -extents.y,  extents.z),
                    center + new Vector3(-extents.x, -extents.y, -extents.z),
                };

                for (int c = 0; c < corners.Length; c++)
                {
                    Vector3 localCorner = _previewInstance.transform.InverseTransformPoint(corners[c]);
                    if (!hasBounds)
                    {
                        localBounds = new Bounds(localCorner, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        localBounds.Encapsulate(localCorner);
                    }
                }
            }

            if (hasBounds)
            {
                float maxDim = Mathf.Max(localBounds.size.x, localBounds.size.y, localBounds.size.z);
                if (maxDim > 0.001f)
                {
                    // Scale factor: maintain comparable size to chest (~0.85m), with soft clamp for cubic items
                    float minDim = Mathf.Min(localBounds.size.x, localBounds.size.y, localBounds.size.z);
                    float aspect = minDim / maxDim;
                    float effectiveTarget = Mathf.Lerp(targetWorldSize, targetWorldSize * 0.9f, aspect);
                    float scaleFactor = effectiveTarget / maxDim;

                    _previewInstance.transform.localScale = Vector3.one * scaleFactor;
                    _previewInstance.transform.localPosition = heightOffset - (localBounds.center * scaleFactor);
                }
            }

            // Ensure emission keyword is active on all materials with emission color for maximum visibility from afar
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null || !r.enabled) continue;
                var mats = r.materials;
                for (int m = 0; m < mats.Length; m++)
                {
                    var mat = mats[m];
                    if (mat != null && mat.HasProperty("_EmissionColor"))
                    {
                        Color ec = mat.GetColor("_EmissionColor");
                        if (ec.r > 0.02f || ec.g > 0.02f || ec.b > 0.02f)
                        {
                            mat.EnableKeyword("_EMISSION");
                        }
                    }
                }
            }
        }

        public void ClearPreview()
        {
            if (_previewInstance != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(_previewInstance);
                else
                    Destroy(_previewInstance);
#else
                Destroy(_previewInstance);
#endif
                _previewInstance = null;
            }

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(child);
                }
                else
                {
                    child.transform.SetParent(null);
                    Destroy(child);
                }
#else
                child.transform.SetParent(null);
                Destroy(child);
#endif
            }
        }
    }
}
