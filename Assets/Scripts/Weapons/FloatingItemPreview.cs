using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// Animates a 3D item visual floating and rotating continuously above a Loot Box.
    /// Robustly normalizes model bounds, centers pivots, and strips gameplay components/particles.
    /// </summary>
    public class FloatingItemPreview : MonoBehaviour
    {
        [Header("Animation Configuration")]
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private float bobSpeed = 3.0f;
        [SerializeField] private float bobHeight = 0.08f;
        [SerializeField] private float targetWorldSize = 0.32f; // Scaled down so weapon previews float neatly above the chest

        private Vector3 _initialLocalPos;
        private GameObject _currentPreviewInstance;
        private float _bobTimer;

        private void Awake()
        {
            if (_initialLocalPos == Vector3.zero)
                _initialLocalPos = transform.localPosition;
        }

        private void Start()
        {
            if (_initialLocalPos == Vector3.zero)
                _initialLocalPos = transform.localPosition;
        }

        private void Update()
        {
            _bobTimer += Time.deltaTime * bobSpeed;
            float newY = _initialLocalPos.y + Mathf.Sin(_bobTimer) * bobHeight;

            // Apply floating bobbing and smooth rotation
            transform.localPosition = new Vector3(_initialLocalPos.x, newY, _initialLocalPos.z);
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }

        /// <summary>
        /// Clears previous preview and instantiates a cosmetic 3D visual preview model.
        /// </summary>
        public void SetPreviewVisual(GameObject visualPrefab)
        {
            ClearPreview();

            if (visualPrefab == null) return;

            _currentPreviewInstance = Instantiate(visualPrefab, transform);
            _currentPreviewInstance.transform.localPosition = Vector3.zero;
            _currentPreviewInstance.transform.localRotation = Quaternion.identity;
            _currentPreviewInstance.transform.localScale = Vector3.one;

            // Strip physics, particles, lights, audio, & interaction scripts from cosmetic copy
            var colliders = _currentPreviewInstance.GetComponentsInChildren<Collider>(true);
            foreach (var col in colliders) col.enabled = false;

            var rbs = _currentPreviewInstance.GetComponentsInChildren<Rigidbody>(true);
            foreach (var rb in rbs) rb.isKinematic = true;

            var particleSystems = _currentPreviewInstance.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in particleSystems) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var particleRenderers = _currentPreviewInstance.GetComponentsInChildren<ParticleSystemRenderer>(true);
            foreach (var pr in particleRenderers) pr.enabled = false;

            var lights = _currentPreviewInstance.GetComponentsInChildren<Light>(true);
            foreach (var l in lights) l.enabled = false;

            var audioSources = _currentPreviewInstance.GetComponentsInChildren<AudioSource>(true);
            foreach (var a in audioSources) a.enabled = false;

            var traps = _currentPreviewInstance.GetComponentsInChildren<TrapBase>(true);
            foreach (var t in traps) t.enabled = false;

            var scripts = _currentPreviewInstance.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var s in scripts)
            {
                if (s != null && s != this) s.enabled = false;
            }

            // Gather valid mesh renderers to calculate unscaled mesh bounds
            var meshRenderers = _currentPreviewInstance.GetComponentsInChildren<MeshRenderer>(true);
            var skinnedRenderers = _currentPreviewInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            Bounds localBounds = new Bounds();
            bool hasBounds = false;

            foreach (var mr in meshRenderers)
            {
                var mf = mr.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    Bounds b = mf.sharedMesh.bounds;
                    Vector3 localCenter = _currentPreviewInstance.transform.InverseTransformPoint(mr.transform.TransformPoint(b.center));
                    Vector3 localSize = Vector3.Scale(b.size, mr.transform.localScale);

                    Bounds scaledLocalBounds = new Bounds(localCenter, localSize);
                    if (!hasBounds)
                    {
                        localBounds = scaledLocalBounds;
                        hasBounds = true;
                    }
                    else
                    {
                        localBounds.Encapsulate(scaledLocalBounds);
                    }
                }
            }

            foreach (var smr in skinnedRenderers)
            {
                if (smr != null && smr.sharedMesh != null)
                {
                    Bounds b = smr.sharedMesh.bounds;
                    Vector3 localCenter = _currentPreviewInstance.transform.InverseTransformPoint(smr.transform.TransformPoint(b.center));
                    Vector3 localSize = Vector3.Scale(b.size, smr.transform.localScale);

                    Bounds scaledLocalBounds = new Bounds(localCenter, localSize);
                    if (!hasBounds)
                    {
                        localBounds = scaledLocalBounds;
                        hasBounds = true;
                    }
                    else
                    {
                        localBounds.Encapsulate(scaledLocalBounds);
                    }
                }
            }

            // Enable mesh renderers
            foreach (var mr in meshRenderers) mr.enabled = true;
            foreach (var smr in skinnedRenderers) smr.enabled = true;

            if (hasBounds)
            {
                // Center the local mesh pivot
                _currentPreviewInstance.transform.localPosition = -localBounds.center;

                float maxDimension = Mathf.Max(localBounds.size.x, localBounds.size.y, localBounds.size.z);
                if (maxDimension > 0.001f)
                {
                    float scaleFactor = targetWorldSize / maxDimension;
                    _currentPreviewInstance.transform.localScale = Vector3.one * scaleFactor;
                    _currentPreviewInstance.transform.localPosition = -localBounds.center * scaleFactor;
                }
            }
        }

        /// <summary>
        /// Removes current floating preview.
        /// </summary>
        public void ClearPreview()
        {
            if (_currentPreviewInstance != null)
            {
                Destroy(_currentPreviewInstance);
                _currentPreviewInstance = null;
            }
        }
    }
}
