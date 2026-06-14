using UnityEngine;
using StarterAssets;
using VelocityWars;

namespace VelocityWars
{
    public class TrickShotWeapon : Weapon
    {
        [Header("Trick Shot Preview")]
        [SerializeField] private LineRenderer _previewLine;
        [SerializeField] private LayerMask _wallAndFloorLayer;
        [SerializeField] private float _previewDistance = 8f;
        [SerializeField] private float _scrollSpeed = 0.5f;

        private StarterAssetsInputs _inputs;
        private Material _lineMaterial;
        private float _offset;

        void Start()
        {
            _inputs = GetComponentInParent<StarterAssetsInputs>();
            if (_previewLine != null)
            {
                _previewLine.enabled = false;
                // Get the material instance to modify offset
                _lineMaterial = _previewLine.material;
            }
        }

        void Update()
        {
            if (_inputs == null || _previewLine == null) return;

            if (_inputs.zoom)
            {
                UpdatePreview();
            }
            else if (_previewLine.enabled)
            {
                _previewLine.enabled = false;
            }
        }

        private void UpdatePreview()
        {
            _previewLine.enabled = true;
            
            // Animated scrolling UV for dashed texture
            _offset -= Time.deltaTime * _scrollSpeed;
            if (_lineMaterial != null)
            {
                _lineMaterial.SetTextureOffset("_MainTex", new Vector2(_offset, 0));
            }

            Transform camTransform = Camera.main.transform;
            Ray ray = new Ray(camTransform.position, camTransform.forward);
            
            if (Physics.Raycast(ray, out RaycastHit hit, _previewDistance, _wallAndFloorLayer))
            {
                Vector3 reflected = Vector3.Reflect(camTransform.forward, hit.normal);
                _previewLine.positionCount = 3;
                _previewLine.SetPosition(0, ray.origin);
                _previewLine.SetPosition(1, hit.point);
                _previewLine.SetPosition(2, hit.point + reflected * (_previewDistance - hit.distance));
            }
            else
            {
                _previewLine.positionCount = 2;
                _previewLine.SetPosition(0, ray.origin);
                _previewLine.SetPosition(1, ray.origin + camTransform.forward * _previewDistance);
            }
        }
    }
}
