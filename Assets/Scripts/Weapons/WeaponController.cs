using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using Unity.Netcode;

namespace Shootopia
{
    /// <summary>
    /// Master third-person weapon controller.
    /// Manages weapon loadouts, equipping, sockets, and animation states.
    /// </summary>
    public partial class WeaponController : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform weaponSocket;
        [SerializeField] private Animator animator;
        [SerializeField] private CharacterMotor motor;
        [SerializeField] private PlayerInputRouter input;

        [Header("Loadout")]
        [SerializeField] private WeaponAttachSO[] loadout;

        [Header("Durability Events")]
        [SerializeField] private GameEvent onWeaponExpired;
        [SerializeField] private FloatGameEvent onDurabilityChanged;
        [SerializeField] private FloatGameEvent onMaxDurabilityChanged;

        [Header("Hitscan Settings")]
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField] private float hitRange = 200f;
        [SerializeField] private GameObject fallbackHitscanImpactVFX;
        [SerializeField] private AudioClip hitscanImpactSound;

        [Header("VFX & Prefabs")]
        [SerializeField] private GameObject vfxImplosionPrefab;
        [SerializeField] private GameObject scopeOverlay;
        [SerializeField] private CinemachineCamera playerFollowCamera;

        // Modular Handlers
        private WeaponADSHandler _adsHandler;
        private WeaponChargeHandler _chargeHandler;
        private WeaponAimHandler _aimHandler;
        private ThrowableTrajectoryPredictor _trajectoryPredictor;
        private PlayerHealth _playerHealth;

        // Animator Hashes
        private static readonly int HashStance = Animator.StringToHash("Stance");
        private static readonly int HashAimPitch = Animator.StringToHash("AimPitch");
        private static readonly int HashIsFiring = Animator.StringToHash("IsFiring");
        private static readonly int HashShootSpeed = Animator.StringToHash("ShootSpeed");
        private static readonly int HashIsThrowAiming = Animator.StringToHash("IsThrowAiming");
        private static readonly int HashThrowRelease = Animator.StringToHash("ThrowRelease");

        private readonly HashSet<int> _validAnimParams = new HashSet<int>();

        // Runtime State
        private int _currentIndex = -1;
        private GameObject _currentVisual;
        private ParticleSystem _muzzleFlash;
        private Transform _headTransform;
        private Transform _neckTransform;
        private Quaternion _initialHeadRotationRelModel;
        private Quaternion _initialNeckRotationRelModel;
        private float _timeSinceLastShot;
        private bool _lastFire;
        private int _currentAmmo;
        private int _currentUses;
        private float _timeRemaining;
        private Coroutine _timeRoutine;
        private bool _isAimingTrap;
        private bool _isAimingBounce;
        private bool _inTrapCooldown;
        private Coroutine _trapCooldownRoutine;
        private Collider[] _ownerColliders;

        private static Dictionary<string, WeaponAttachSO> _attachCache;

        // Public Properties
        public WeaponAttachSO CurrentAttach { get; private set; }
        public Transform Muzzle { get; private set; }
        public Transform GripR { get; private set; }
        public Transform GripL { get; private set; }
        public bool IsArmed => CurrentAttach != null && _currentVisual != null;
        public bool IsADSActive => _adsHandler != null && _adsHandler.IsADSActive;
        public int CurrentAmmo => _currentAmmo;
        public int CurrentUses => _currentUses;
        public float TimeRemaining => _timeRemaining;
        public float TimeSinceLastShot => _timeSinceLastShot;
        public Transform WeaponSocket => weaponSocket;
        public GameObject CurrentVisual => _currentVisual;

        public float CurrentDurabilityValue
        {
            get
            {
                if (CurrentAttach == null || CurrentAttach.gameplayData == null) return 0f;
                return CurrentAttach.gameplayData.durabilityType switch
                {
                    DurabilityType.Ammo => _currentAmmo,
                    DurabilityType.Uses => _currentUses,
                    DurabilityType.TimeBased => _timeRemaining,
                    _ => 0f
                };
            }
        }

        private IAimProvider Aim => motor != null ? motor.Aim : null;

        public static void RegisterAttach(WeaponAttachSO attach)
        {
            if (attach == null || string.IsNullOrEmpty(attach.name)) return;
            _attachCache ??= new Dictionary<string, WeaponAttachSO>(System.StringComparer.OrdinalIgnoreCase);
            _attachCache[attach.name] = attach;
        }

        public static WeaponAttachSO GetAttachByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (_attachCache == null)
            {
                _attachCache = new Dictionary<string, WeaponAttachSO>(System.StringComparer.OrdinalIgnoreCase);
                foreach (var a in Resources.LoadAll<WeaponAttachSO>("Weapons"))
                    if (a != null && !string.IsNullOrEmpty(a.name)) _attachCache[a.name] = a;
                foreach (var a in Resources.LoadAll<WeaponAttachSO>(""))
                    if (a != null && !string.IsNullOrEmpty(a.name) && !_attachCache.ContainsKey(a.name)) _attachCache[a.name] = a;
            }
            _attachCache.TryGetValue(name, out var result);
            return result;
        }

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            if (motor == null) motor = GetComponent<CharacterMotor>();
            if (input == null) input = GetComponent<PlayerInputRouter>();

            CacheAnimParameters();
            _headTransform = FindChild(transform, "head");
            _neckTransform = FindChild(transform, "neck_01");

            Transform modelRoot = motor != null ? motor.ModelRoot : transform;
            if (loadout != null)
            {
                foreach (var a in loadout)
                {
                    if (a != null)
                    {
                        RegisterAttach(a);
                        if (GlobalPoolManager.Instance != null && a.gameplayData != null)
                        {
                            var d = a.gameplayData;
                            if (d.projectilePrefab != null) GlobalPoolManager.Instance.Prewarm(d.projectilePrefab, 15);
                        }
                    }
                }
            }

            if (_headTransform != null) _initialHeadRotationRelModel = Quaternion.Inverse(modelRoot.rotation) * _headTransform.rotation;
            if (_neckTransform != null) _initialNeckRotationRelModel = Quaternion.Inverse(modelRoot.rotation) * _neckTransform.rotation;

            var aimRig = GetComponent<PlayerAimRig>();
            _trajectoryPredictor = GetComponent<ThrowableTrajectoryPredictor>() ?? gameObject.AddComponent<ThrowableTrajectoryPredictor>();
            _adsHandler = new WeaponADSHandler(transform, motor, playerFollowCamera, scopeOverlay);
            _chargeHandler = new WeaponChargeHandler(vfxImplosionPrefab);
            _aimHandler = new WeaponAimHandler(transform, animator, motor, aimRig);
            _playerHealth = GetComponent<PlayerHealth>() ?? GetComponentInParent<PlayerHealth>();
            _ownerColliders = GetComponentsInChildren<Collider>(true);
        }

        private void LateUpdate()
        {
            bool isMelee = CurrentAttach == null || CurrentAttach.stanceId == 3 ||
                           (CurrentAttach.gameplayData != null && (CurrentAttach.gameplayData.category == WeaponCategory.Melee || CurrentAttach.gameplayData.name.Contains("Knife")));
            if (isMelee) return;

            bool isChargingOrAiming = (_chargeHandler != null && _chargeHandler.IsCharging) || _isAimingTrap || _isAimingBounce;
            bool isCombatStanceOrRetreating = (_aimHandler != null && _aimHandler.IsInCombatStance) || (motor != null && motor.IsInCombat);
            bool isAnimatorFiring = animator != null && (animator.GetBool(HashIsFiring) || animator.GetCurrentAnimatorStateInfo(1).IsName("Shoot") || animator.IsInTransition(1));

            if (isChargingOrAiming || isCombatStanceOrRetreating || isAnimatorFiring)
            {
                Transform model = motor != null ? motor.ModelRoot : transform;
                if (_neckTransform != null) _neckTransform.rotation = model.rotation * _initialNeckRotationRelModel;
                if (_headTransform != null) _headTransform.rotation = model.rotation * _initialHeadRotationRelModel;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _timeSinceLastShot += dt;

            if (!IsOwner)
            {
                if (_chargeHandler.IsCharging && CurrentAttach?.gameplayData != null)
                    _chargeHandler.UpdateCharge(dt, CurrentAttach.gameplayData);
                return;
            }

            if (_playerHealth != null && _playerHealth.IsDead)
            {
                OnPlayerDied();
                return;
            }

            if (input == null) return;

            bool isFrozen = WaitingRoomController.IsLobbyMenuOpen || PauseMenuController.IsMenuPaused ||
                            (GameModeBase.Instance != null && GameModeBase.Instance.PreMatchCountdown.Value > 0);

            if (isFrozen)
            {
                _adsHandler.ExitADS();
                _aimHandler.ExitCombatStance();
                _isAimingBounce = false;
                _aimHandler.DisableBouncePreview();
                _lastFire = false;
                return;
            }

            if (input.Intent.SwapWeapon) CycleWeapon();

            var currentData = CurrentAttach?.gameplayData;
            bool isSniperAutoADS = currentData != null && (currentData.requiresADSToFire || (currentData.hasADS && CurrentAttach != null && CurrentAttach.name.Contains("Sniper"))) && input.Intent.Fire;

            HandleFire(dt);
            _adsHandler.UpdateADS(_chargeHandler.IsCharging || _isAimingTrap || isSniperAutoADS, input.Intent.AltFire, CurrentAttach);
            _aimHandler.UpdateAimState(dt, input.Intent.Fire, CurrentAttach, _chargeHandler.IsCharging || isSniperAutoADS, _isAimingTrap, _isAimingBounce);
            _aimHandler.UpdateBouncePreview(IsOwner, CurrentAttach, Muzzle, Aim, _isAimingBounce);
        }
    }
}
