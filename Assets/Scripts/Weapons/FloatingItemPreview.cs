using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// Animates a 3D item visual floating and rotating continuously above a Loot Box.
    /// </summary>
    public class FloatingItemPreview : MonoBehaviour
    {
        [Header("Animation Configuration")]
        [SerializeField] private float rotationSpeed = 60f;
        [SerializeField] private float bobSpeed = 2.5f;
        [SerializeField] private float bobHeight = 0.2f;
        [SerializeField] private Vector3 previewScale = new Vector3(1.5f, 1.5f, 1.5f);

        private Vector3 _initialLocalPos;
        private GameObject _currentPreviewInstance;

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
            // Continuous Y-axis rotation in fixed place (no vertical bobbing/hovering)
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            if (_initialLocalPos != Vector3.zero)
            {
                transform.localPosition = _initialLocalPos;
            }
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

            var renderers = _currentPreviewInstance.GetComponentsInChildren<Renderer>(true);
            foreach (var rend in renderers) rend.enabled = true;

            // Calculate bounding box of item preview to scale it comparable to the lootbox size (~1.3 world units)
            Bounds combinedBounds = new Bounds();
            bool hasBounds = false;
            foreach (var rend in renderers)
            {
                if (!hasBounds)
                {
                    combinedBounds = rend.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(rend.bounds);
                }
            }

            if (hasBounds)
            {
                float maxExtent = Mathf.Max(combinedBounds.size.x, combinedBounds.size.y, combinedBounds.size.z);
                if (maxExtent > 0.001f)
                {
                    float targetWorldSize = 1.3f; // Desired max dimension in world units
                    Vector3 parentLossy = transform.lossyScale;
                    float avgParentScale = (parentLossy.x + parentLossy.y + parentLossy.z) / 3f;
                    if (avgParentScale < 0.001f) avgParentScale = 1f;

                    float finalLocalScale = targetWorldSize / (maxExtent * avgParentScale);
                    _currentPreviewInstance.transform.localScale = Vector3.one * finalLocalScale;
                }
            }

            // Strip physics & interaction scripts from the display visual copy
            var colliders = _currentPreviewInstance.GetComponentsInChildren<Collider>(true);
            foreach (var col in colliders) col.enabled = false;

            var rbs = _currentPreviewInstance.GetComponentsInChildren<Rigidbody>(true);
            foreach (var rb in rbs) rb.isKinematic = true;

            var traps = _currentPreviewInstance.GetComponentsInChildren<TrapBase>(true);
            foreach (var t in traps) t.enabled = false;
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
