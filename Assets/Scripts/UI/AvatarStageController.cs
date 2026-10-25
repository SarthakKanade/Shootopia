using UnityEngine;
#if UNITY_URP_ACTIVE || true
using UnityEngine.Rendering.Universal;
#endif

namespace Shootopia
{
    /// <summary>
    /// Renders the authentic 3D player avatar directly in the Main Menu scene.
    /// Manages hero placement standing straight facing the camera with the idle animation,
    /// pedestal framing, spotlighting, and real-time skin switching (Default Agent / Robot).
    /// </summary>
    public class AvatarStageController : MonoBehaviour
    {
        public static AvatarStageController Instance { get; private set; }

        [Header("Stage References")]
        [SerializeField] private Camera stageCamera;
        [SerializeField] private Transform avatarHolder;
        [SerializeField] private GameObject defaultAvatarPrefab;
        [SerializeField] private GameObject robotAvatarPrefab;
        [SerializeField] private RuntimeAnimatorController menuIdleAnimator;
        [SerializeField] private Transform stagePedestal;
        [SerializeField] private Light stageLight;

        [Header("Framing & Stance")]
        [SerializeField] private float baseFacingAngle = 180f; // Faces straight towards camera
        [SerializeField] private float avatarScale = 0.48f; // Stylized compact proportion
        [SerializeField] private bool allowSway = false; // Hero standing straight
        [SerializeField] private float swayAngle = 6f;
        [SerializeField] private float swaySpeed = 0.6f;
        [SerializeField] private float bobAmplitude = 0.01f;
        [SerializeField] private float bobFrequency = 1.0f;

        private GameObject _spawnedAvatar;
        private Vector3 _initialHolderPosition;
        private int _currentSkinIndex = 0;

        private void Awake()
        {
            Instance = this;

            if (avatarHolder != null)
            {
                _initialHolderPosition = avatarHolder.localPosition;
            }

            SetupFraming();
        }

        private void Start()
        {
            SpawnAvatar();
        }

        public void SetupFraming()
        {
            // Position camera for hero framing (crisp, full-body + pedestal in center stage with generous headroom and no border clipping)
            if (stageCamera != null)
            {
                stageCamera.transform.localPosition = new Vector3(0f, 0.62f, -4.5f);
                stageCamera.transform.localEulerAngles = new Vector3(2.0f, 0f, 0f);
                stageCamera.fieldOfView = 28f;
                stageCamera.nearClipPlane = 0.1f;
                stageCamera.farClipPlane = 50f;

                var urpCam = stageCamera.GetComponent<UniversalAdditionalCameraData>();
                if (urpCam == null)
                {
                    urpCam = stageCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                }
                urpCam.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                urpCam.renderShadows = true;
            }

            if (avatarHolder != null)
            {
                avatarHolder.localPosition = new Vector3(0f, 0.08f, 0f);
                avatarHolder.localEulerAngles = new Vector3(0f, baseFacingAngle, 0f);
                avatarHolder.localScale = Vector3.one * avatarScale;
                _initialHolderPosition = avatarHolder.localPosition;
            }

            if (stagePedestal != null)
            {
                stagePedestal.localPosition = new Vector3(0f, 0.07f, 0f);
                stagePedestal.localScale = new Vector3(0.78f, 0.025f, 0.78f);
            }

            if (stageLight != null)
            {
                stageLight.transform.localPosition = new Vector3(0.8f, 3.2f, -2.2f);
                stageLight.transform.localEulerAngles = new Vector3(45f, -20f, 0f);
                stageLight.intensity = 2.4f;
                stageLight.color = new Color(0.98f, 0.98f, 1.0f);
            }
        }

        private void Update()
        {
            if (avatarHolder != null && allowSway)
            {
                float currentSway = Mathf.Sin(Time.time * swaySpeed) * swayAngle;
                avatarHolder.localEulerAngles = new Vector3(0f, baseFacingAngle + currentSway, 0f);

                float bobOffset = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
                avatarHolder.localPosition = _initialHolderPosition + new Vector3(0f, bobOffset, 0f);
            }
        }

        public void SetSkin(int skinIndex)
        {
            _currentSkinIndex = skinIndex;
            SpawnAvatar();
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

                // Strip physics, gameplay controllers, and netcode components from the preview mesh
                var monoBehaviours = _spawnedAvatar.GetComponentsInChildren<MonoBehaviour>();
                foreach (var mb in monoBehaviours)
                {
                    if (mb != null && mb != this)
                    {
                        Destroy(mb);
                    }
                }

                var colliders = _spawnedAvatar.GetComponentsInChildren<Collider>();
                foreach (var col in colliders) col.enabled = false;

                var rigidbodies = _spawnedAvatar.GetComponentsInChildren<Rigidbody>();
                foreach (var rb in rigidbodies) rb.isKinematic = true;

                // Apply active model variant (0 = Vanguard [Body01/Head01], 1 = Valkyrie [Body02/Head02])
                var body01 = _spawnedAvatar.transform.Find("ModelRoot/DefaultMalePolyart/Body01");
                var head01 = _spawnedAvatar.transform.Find("ModelRoot/DefaultMalePolyart/Head01");
                var body02 = _spawnedAvatar.transform.Find("ModelRoot/DefaultMalePolyart/Body02");
                var head02 = _spawnedAvatar.transform.Find("ModelRoot/DefaultMalePolyart/Head02");

                bool isValkyrie = (_currentSkinIndex == 1);
                if (body01 != null) body01.gameObject.SetActive(!isValkyrie);
                if (head01 != null) head01.gameObject.SetActive(!isValkyrie);
                if (body02 != null) body02.gameObject.SetActive(isValkyrie);
                if (head02 != null) head02.gameObject.SetActive(isValkyrie);

                // Ensure animator plays clean idle loop
                var anim = _spawnedAvatar.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    anim.applyRootMotion = false;
                    if (menuIdleAnimator != null)
                    {
                        anim.runtimeAnimatorController = menuIdleAnimator;
                    }
                    anim.Play("Idle", 0, 0f);
                }
            }
        }
    }
}
