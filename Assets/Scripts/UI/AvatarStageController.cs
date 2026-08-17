using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_URP_ACTIVE || true
using UnityEngine.Rendering.Universal;
#endif

namespace Shootopia
{
    /// <summary>
    /// Renders an ultra-sharp, anti-aliased 3D preview of the player avatar onto a high-res RenderTexture.
    /// Manages hero diorama framing, front-facing orientation, idle breathing bob, and real-time skin switching.
    /// </summary>
    public class AvatarStageController : MonoBehaviour
    {
        public static AvatarStageController Instance { get; private set; }

        [Header("Stage References")]
        [SerializeField] private Camera stageCamera;
        [SerializeField] private Transform avatarHolder;
        [SerializeField] private GameObject defaultAvatarPrefab;
        [SerializeField] private GameObject robotAvatarPrefab;
        [SerializeField] private Transform stagePedestal;
        [SerializeField] private Light stageLight;

        [Header("Render Texture Settings (High-DPI Sharpness)")]
        [SerializeField] private int textureWidth = 2048;
        [SerializeField] private int textureHeight = 2048;

        [Header("Animation & Stance")]
        [SerializeField] private float baseFacingAngle = 180f; // 180° = Faces front towards camera
        [SerializeField] private bool continuousRotation = false; // False = Hero front-facing with subtle sway
        [SerializeField] private float swayAngle = 14f;
        [SerializeField] private float swaySpeed = 0.85f;
        [SerializeField] private float bobAmplitude = 0.015f;
        [SerializeField] private float bobFrequency = 1.3f;

        private RenderTexture _stageRenderTexture;
        private GameObject _spawnedAvatar;
        private Vector3 _initialHolderPosition;
        private int _currentSkinIndex = 0;

        private void Awake()
        {
            Instance = this;

            // Setup High-Res Anti-Aliased RenderTexture matching pipeline MSAA
            if (stageCamera != null)
            {
                int msaaSamples = 1;
                var urpAsset = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                if (urpAsset != null && urpAsset.msaaSampleCount > 1)
                {
                    msaaSamples = urpAsset.msaaSampleCount;
                }

                _stageRenderTexture = new RenderTexture(textureWidth, textureHeight, 24, RenderTextureFormat.ARGB32)
                {
                    name = "AvatarStage_RT_2K",
                    filterMode = FilterMode.Bilinear,
                    antiAliasing = msaaSamples,
                    useMipMap = false,
                    autoGenerateMips = false
                };
                _stageRenderTexture.Create();
                stageCamera.targetTexture = _stageRenderTexture;
                stageCamera.allowMSAA = msaaSamples > 1;
                stageCamera.allowHDR = true;

                // Ensure URP Camera has clean FXAA anti-aliasing without render pass sample mismatch
                var urpCam = stageCamera.GetComponent<UniversalAdditionalCameraData>();
                if (urpCam == null)
                {
                    urpCam = stageCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                }
                urpCam.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
                urpCam.renderShadows = true;
            }

            if (avatarHolder != null)
            {
                _initialHolderPosition = avatarHolder.localPosition;
            }
        }

        private void Start()
        {
            SetupFraming();
            SpawnAvatar();
            BindToUIDocument();
        }

        private void SetupFraming()
        {
            // Position camera for hero framing (entire body + pedestal in view)
            if (stageCamera != null)
            {
                stageCamera.transform.localPosition = new Vector3(0f, 0.65f, -3.85f);
                stageCamera.transform.localEulerAngles = new Vector3(3.0f, 0f, 0f);
                stageCamera.fieldOfView = 36f;
                stageCamera.nearClipPlane = 0.1f;
                stageCamera.farClipPlane = 20f;
            }

            if (avatarHolder != null)
            {
                avatarHolder.localPosition = new Vector3(0f, 0.05f, 0f);
                avatarHolder.localEulerAngles = new Vector3(0f, baseFacingAngle, 0f);
                _initialHolderPosition = avatarHolder.localPosition;
            }

            if (stagePedestal != null)
            {
                stagePedestal.localPosition = new Vector3(0f, 0.0f, 0f);
                stagePedestal.localScale = new Vector3(1.5f, 0.05f, 1.5f);
            }

            if (stageLight != null)
            {
                stageLight.transform.localPosition = new Vector3(1.2f, 3.0f, -2.5f);
                stageLight.transform.localEulerAngles = new Vector3(40f, -25f, 0f);
                stageLight.intensity = 1.8f;
                stageLight.color = new Color(0.96f, 0.98f, 1.0f);
            }
        }

        private void Update()
        {
            if (avatarHolder != null)
            {
                if (continuousRotation)
                {
                    avatarHolder.Rotate(Vector3.up, 12f * Time.deltaTime, Space.World);
                }
                else
                {
                    // Hero front-facing with subtle natural sway
                    float currentSway = Mathf.Sin(Time.time * swaySpeed) * swayAngle;
                    avatarHolder.localEulerAngles = new Vector3(0f, baseFacingAngle + currentSway, 0f);
                }

                // Gentle vertical breathing bob
                float bobOffset = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
                avatarHolder.localPosition = _initialHolderPosition + new Vector3(0f, bobOffset, 0f);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            if (_stageRenderTexture != null)
            {
                if (stageCamera != null && stageCamera.targetTexture == _stageRenderTexture)
                {
                    stageCamera.targetTexture = null;
                }
                _stageRenderTexture.Release();
                Destroy(_stageRenderTexture);
            }
        }

        public void SetSkin(int skinIndex)
        {
            _currentSkinIndex = skinIndex;
            GameObject prefabToUse = (skinIndex == 1 && robotAvatarPrefab != null) ? robotAvatarPrefab : defaultAvatarPrefab;
            SpawnAvatar(prefabToUse);
        }

        public void SpawnAvatar(GameObject prefab = null)
        {
            if (_spawnedAvatar != null)
            {
                Destroy(_spawnedAvatar);
            }

            var toSpawn = prefab != null ? prefab : defaultAvatarPrefab;
            if (toSpawn != null && avatarHolder != null)
            {
                _spawnedAvatar = Instantiate(toSpawn, avatarHolder);
                _spawnedAvatar.transform.localPosition = Vector3.zero;
                _spawnedAvatar.transform.localRotation = Quaternion.identity;
                _spawnedAvatar.transform.localScale = Vector3.one;

                // Disable physics, colliders, and gameplay logic on preview model
                var colliders = _spawnedAvatar.GetComponentsInChildren<Collider>();
                foreach (var col in colliders) col.enabled = false;

                var rigidbodies = _spawnedAvatar.GetComponentsInChildren<Rigidbody>();
                foreach (var rb in rigidbodies) rb.isKinematic = true;

                // Ensure animator idles cleanly without root motion displacing the hero
                var anim = _spawnedAvatar.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    anim.applyRootMotion = false;
                }
            }
        }

        public void BindToUIDocument()
        {
            if (_stageRenderTexture == null) return;

            var uiDocs = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
            foreach (var doc in uiDocs)
            {
                if (doc != null && doc.rootVisualElement != null)
                {
                    var stageDisplay = doc.rootVisualElement.Q<VisualElement>("AvatarStageDisplay");
                    if (stageDisplay != null)
                    {
                        stageDisplay.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(_stageRenderTexture));
                        Debug.Log("[AvatarStageController] Successfully bound 2K stage RenderTexture to AvatarStageDisplay.");
                        break;
                    }
                }
            }
        }
    }
}
