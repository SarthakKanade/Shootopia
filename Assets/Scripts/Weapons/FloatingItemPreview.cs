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
            // Continuous Y-axis rotation
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

            // Smooth sine wave vertical bobbing
            float newY = _initialLocalPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.localPosition = new Vector3(_initialLocalPos.x, newY, _initialLocalPos.z);
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
            _currentPreviewInstance.transform.localScale = previewScale;

            var renderers = _currentPreviewInstance.GetComponentsInChildren<Renderer>(true);
            foreach (var rend in renderers) rend.enabled = true;

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
