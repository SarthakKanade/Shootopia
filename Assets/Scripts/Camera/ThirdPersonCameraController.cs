using UnityEngine;
using Unity.Cinemachine;

namespace Shootopia
{
    /// <summary>
    /// Manages the third-person camera view transitions between Walking and Combat
    /// presets. Sits on the Cinemachine camera GameObject as a CinemachineExtension
    /// and drives <see cref="CinemachineThirdPersonFollow"/> distance/offset,
    /// lens FOV, and downward tilt angle.
    /// 
    /// Walking <-> Combat transitions happen automatically based on the local
    /// player's <see cref="CharacterMotor.IsInCombat"/> flag. The sniper ADS
    /// system (<see cref="WeaponADSHandler"/>) takes priority when active —
    /// this controller yields FOV/distance control and resumes once ADS exits.
    /// 
    /// The camera never auto-rotates on player movement; rotation is entirely
    /// user-controlled via <see cref="PlayerAimRig"/>.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(CinemachineCamera))]
    public class ThirdPersonCameraController : CinemachineExtension
    {
        public static ThirdPersonCameraController Instance { get; private set; }

        [Header("Walking View")]
        [Tooltip("Camera distance behind player in units.")]
        [SerializeField] private float walkingDistance = 3.2f;
        [Tooltip("Camera height (shoulder Y) above pivot in units.")]
        [SerializeField] private float walkingHeight = 1.7f;
        [Tooltip("Field of view in degrees.")]
        [SerializeField] private float walkingFOV = 68f;
        [Tooltip("Downward tilt angle in degrees.")]
        [SerializeField] private float walkingDownwardAngle = 15f;
        [Tooltip("Horizontal shoulder offset (positive = camera right, player left).")]
        [SerializeField] private float walkingShoulderX = 0.48f;

        [Header("Combat View")]
        [Tooltip("Camera distance behind player in units.")]
        [SerializeField] private float combatDistance = 2.7f;
        [Tooltip("Camera height (shoulder Y) above pivot in units.")]
        [SerializeField] private float combatHeight = 1.6f;
        [Tooltip("Field of view in degrees.")]
        [SerializeField] private float combatFOV = 65f;
        [Tooltip("Downward tilt angle in degrees.")]
        [SerializeField] private float combatDownwardAngle = 14.5f;
        [Tooltip("Horizontal shoulder offset (positive = camera right, player left).")]
        [SerializeField] private float combatShoulderX = 0.48f;

        [Header("Transition")]
        [Tooltip("Approximate transition time in seconds (SmoothDamp).")]
        [SerializeField] private float transitionTime = 0.30f;

        // --- Active (interpolated) values exposed for WeaponADSHandler & inspection ---
        public float ActiveFOV { get; private set; }
        public float ActiveDistance { get; private set; }
        public float ActiveHeight { get; private set; }
        public float ActiveDownwardAngle { get; private set; }
        public Vector3 ActiveShoulderOffset { get; private set; }
        public float ActiveVerticalArm { get; private set; }

        /// <summary>True while the controller is driving the combat preset.</summary>
        public bool IsInCombatView { get; private set; }

        private CinemachineCamera _cmCamera;
        private CinemachineThirdPersonFollow _tpFollow;

        // SmoothDamp velocities
        private float _fovVel;
        private float _distVel;
        private float _heightVel;
        private float _shoulderXVel;
        private float _angleVel;

        // Combat detection cache
        private CharacterMotor _localMotor;
        private WeaponController _localWeaponController;
        private float _cacheTimer;

        // ADS gate — when true, we stop applying FOV/distance/offset so WeaponADSHandler has full control.
        private bool _adsActive;

        public const string PREFS_KEY_FOV = "Shootopia_CameraFOV";
        public const float MIN_FOV = 60f;
        public const float MAX_FOV = 105f;
        public const float DEFAULT_FOV = 75f;

        protected override void Awake()
        {
            base.Awake();

            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            _cmCamera = GetComponent<CinemachineCamera>();
            _tpFollow = GetComponentInChildren<CinemachineThirdPersonFollow>(true);

            // Load saved user FOV preference (defaulting to 75)
            float savedFOV = PlayerPrefs.GetFloat(PREFS_KEY_FOV, DEFAULT_FOV);
            if (Mathf.Approximately(savedFOV, 80f) && !PlayerPrefs.HasKey("Shootopia_UserSetFOV"))
            {
                savedFOV = DEFAULT_FOV;
            }
            ApplyUserFOV(savedFOV);

            // Initialise to walking view
            SnapToWalkingView();
        }

        /// <summary>
        /// Updates the user's base Field of View preference (clamped 60° - 105°) and immediately applies it.
        /// </summary>
        public void SetUserFOV(float fov)
        {
            fov = Mathf.Clamp(fov, MIN_FOV, MAX_FOV);
            ApplyUserFOV(fov);
            PlayerPrefs.SetFloat(PREFS_KEY_FOV, fov);
            PlayerPrefs.SetInt("Shootopia_UserSetFOV", 1);
            PlayerPrefs.Save();
        }

        private void ApplyUserFOV(float fov)
        {
            walkingFOV = fov;
            combatFOV = Mathf.Max(60f, fov - 3f);
            if (_cmCamera != null && !_adsActive)
            {
                _cmCamera.Lens.FieldOfView = IsInCombatView ? combatFOV : walkingFOV;
                ActiveFOV = _cmCamera.Lens.FieldOfView;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this) Instance = null;
        }

        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage == CinemachineCore.Stage.Body)
            {
                if (Mathf.Abs(ActiveDownwardAngle) > 0.001f)
                {
                    state.RawOrientation *= Quaternion.Euler(ActiveDownwardAngle, 0f, 0f);
                }
            }
        }

        private void LateUpdate()
        {
            RefreshLocalPlayerCache();

            // Detect ADS state — yield to WeaponADSHandler when scope is active
            bool adsNow = false;
            if (_localWeaponController != null)
            {
                adsNow = _localWeaponController.IsADSActive;
            }

            if (adsNow)
            {
                _adsActive = true;
                return; // Don't fight ADS lerp for FOV/distance/shoulder; maintain existing camera pitch
            }

            // ADS just ended — reset velocities so we smoothly return, not snap
            if (_adsActive && !adsNow)
            {
                _adsActive = false;
                _fovVel = 0f;
                _distVel = 0f;
                _heightVel = 0f;
                _shoulderXVel = 0f;
                _angleVel = 0f;
            }

            // Determine target view
            bool inCombat = false;
            if (_localMotor != null)
            {
                inCombat = _localMotor.IsInCombat;
            }
            IsInCombatView = inCombat;

            float targetFOV = inCombat ? combatFOV : walkingFOV;
            float targetDist = inCombat ? combatDistance : walkingDistance;
            float targetHeight = inCombat ? combatHeight : walkingHeight;
            float targetAngle = inCombat ? combatDownwardAngle : walkingDownwardAngle;
            float targetShoulderX = inCombat ? combatShoulderX : walkingShoulderX;

            // SmoothDamp each parameter
            ActiveFOV = Mathf.SmoothDamp(ActiveFOV, targetFOV, ref _fovVel, transitionTime);
            ActiveDistance = Mathf.SmoothDamp(ActiveDistance, targetDist, ref _distVel, transitionTime);
            ActiveHeight = Mathf.SmoothDamp(ActiveHeight, targetHeight, ref _heightVel, transitionTime);
            ActiveDownwardAngle = Mathf.SmoothDamp(ActiveDownwardAngle, targetAngle, ref _angleVel, transitionTime);
            float activeShoulderX = Mathf.SmoothDamp(ActiveShoulderOffset.x, targetShoulderX, ref _shoulderXVel, transitionTime);

            ActiveShoulderOffset = new Vector3(activeShoulderX, ActiveHeight, 0f);
            ActiveVerticalArm = 0f;

            // Apply to Cinemachine
            if (_cmCamera != null)
            {
                _cmCamera.Lens.FieldOfView = ActiveFOV;
            }

            if (_tpFollow != null)
            {
                _tpFollow.CameraDistance = ActiveDistance;
                _tpFollow.ShoulderOffset = ActiveShoulderOffset;
                _tpFollow.VerticalArmLength = 0f;
            }
        }

        /// <summary>
        /// Immediately snaps to the walking view preset (no transition). Used at spawn.
        /// </summary>
        public void SnapToWalkingView()
        {
            ApplyImmediate(walkingFOV, walkingDistance, walkingHeight, walkingDownwardAngle, walkingShoulderX);
            _fovVel = _distVel = _heightVel = _shoulderXVel = _angleVel = 0f;
            IsInCombatView = false;
        }

        private void ApplyImmediate(float fov, float dist, float height, float angle, float shoulderX)
        {
            ActiveFOV = fov;
            ActiveDistance = dist;
            ActiveHeight = height;
            ActiveDownwardAngle = angle;
            ActiveShoulderOffset = new Vector3(shoulderX, height, 0f);
            ActiveVerticalArm = 0f;

            if (_cmCamera != null) _cmCamera.Lens.FieldOfView = fov;
            if (_tpFollow != null)
            {
                _tpFollow.CameraDistance = dist;
                _tpFollow.ShoulderOffset = ActiveShoulderOffset;
                _tpFollow.VerticalArmLength = 0f;
            }
        }

        private void RefreshLocalPlayerCache()
        {
            _cacheTimer -= Time.deltaTime;
            if (_localMotor != null && _cacheTimer > 0f) return;
            _cacheTimer = 2.0f;

            if (Unity.Netcode.NetworkManager.Singleton != null &&
                Unity.Netcode.NetworkManager.Singleton.LocalClient != null &&
                Unity.Netcode.NetworkManager.Singleton.LocalClient.PlayerObject != null)
            {
                var playerObj = Unity.Netcode.NetworkManager.Singleton.LocalClient.PlayerObject;
                _localMotor = playerObj.GetComponent<CharacterMotor>();
                _localWeaponController = playerObj.GetComponent<WeaponController>();
            }
        }
    }
}
