using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using Shootopia;
using Unity.Netcode;

namespace Shootopia
{
    /// <summary>
    /// Third-person weapon controller. Equips a <see cref="WeaponAttachSO"/> by
    /// instantiating its visual under the right-hand socket, applies offsets, and
    /// exposes the resolved Muzzle/GripR/GripL transforms for firing (Phase 6)
    /// and IK (Phase 7). MP-friendly: consumes <see cref="PlayerInputRouter"/>
    /// intent and <see cref="IAimProvider"/> — never Camera.main.
    /// </summary>
    public class WeaponController : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform weaponSocket;     // WeaponSocket_R under hand_r
        [SerializeField] private Animator animator;
        [SerializeField] private CharacterMotor motor;       // supplies IAimProvider + intent
        [SerializeField] private PlayerInputRouter input;

        [Header("Loadout (AR, Pistol)")]
        [SerializeField] private WeaponAttachSO[] loadout;

        [Header("Durability Events (reused from existing UI system)")]
        [SerializeField] private GameEvent onWeaponExpired;
        [SerializeField] private FloatGameEvent onDurabilityChanged;
        [SerializeField] private FloatGameEvent onMaxDurabilityChanged;

        [Header("Hitscan (for weapons without a projectilePrefab)")]
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField] private float hitRange = 200f;
        [SerializeField] private GameObject _fallbackHitscanImpactVFX;
        [SerializeField] private AudioClip _hitscanImpactSound;

        [Header("Charging VFX")]
        [SerializeField] private GameObject defaultChargeVFXPrefab;

        [Header("UI & ADS")]
        [SerializeField] private GameObject scopeOverlay;
        [SerializeField] private CinemachineCamera playerFollowCamera;
        private GameObject _scopeOverlayInstance;
        private float _defaultFOV;
        private float _originalMoveSpeed;
        private bool _isADSActive;

        private static readonly int HashStance = Animator.StringToHash("Stance");
        private static readonly int HashAimPitch = Animator.StringToHash("AimPitch");
        private static readonly int HashIsFiring = Animator.StringToHash("IsFiring");
        private static readonly int HashShootSpeed = Animator.StringToHash("ShootSpeed");
        private static readonly int HashIsThrowAiming = Animator.StringToHash("IsThrowAiming");
        private static readonly int HashThrowRelease = Animator.StringToHash("ThrowRelease");

        // Charging / Throwing state
        private bool _isCharging;
        private float _chargeTimer;
        private float _punchTimer;
        private GameObject _activeChargeVFX;
        private AudioSource _activeChargeAudioSource;
        [SerializeField] private GameObject vfxImplosionPrefab;
        private bool _isAimingTrap;
        private bool _inTrapCooldown;
        private Coroutine _cooldownRoutine;
        private ThrowableTrajectoryPredictor _trajectoryPredictor;

        private readonly NetworkVariable<Unity.Collections.FixedString64Bytes> _equippedAttachName = new NetworkVariable<Unity.Collections.FixedString64Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private static System.Collections.Generic.Dictionary<string, WeaponAttachSO> _attachCache;

        public static WeaponAttachSO GetAttachByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (_attachCache == null)
            {
                _attachCache = new System.Collections.Generic.Dictionary<string, WeaponAttachSO>();
                var allAttaches = Resources.LoadAll<WeaponAttachSO>("");
                foreach (var a in allAttaches)
                {
                    if (a != null && !string.IsNullOrEmpty(a.name) && !_attachCache.ContainsKey(a.name))
                    {
                        _attachCache[a.name] = a;
                    }
                }
#if UNITY_EDITOR
                var guids = UnityEditor.AssetDatabase.FindAssets("t:Shootopia.WeaponAttachSO");
                foreach (var g in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                    var a = UnityEditor.AssetDatabase.LoadAssetAtPath<Shootopia.WeaponAttachSO>(path);
                    if (a != null && !string.IsNullOrEmpty(a.name) && !_attachCache.ContainsKey(a.name))
                    {
                        _attachCache[a.name] = a;
                    }
                }
#endif
            }

            _attachCache.TryGetValue(name, out var result);
            return result;
        }

        private int _currentIndex = -1;
        private GameObject _currentVisual;
        private PlayerAimRig _aimRig;
        private ParticleSystem _muzzleFlash;
        private Transform _headTransform;
        private Quaternion _initialHeadRotation;

        // Firing / durability runtime
        private float _timeSinceLastShot;
        private bool _lastFire;
        private int _currentAmmo;
        private int _currentUses;
        private float _timeRemaining;
        private Coroutine _timeRoutine;
        private float _combatStanceTimer = 0f;
        private const float COMBAT_STANCE_DURATION = 1.5f;

        // Resolved attach points (used by firing + IK)
        public WeaponAttachSO CurrentAttach { get; private set; }
        public Transform Muzzle { get; private set; }
        public Transform GripR { get; private set; }
        public Transform GripL { get; private set; }
        public bool IsArmed => CurrentAttach != null && _currentVisual != null;

        /// <summary>Current durability value (ammo / uses / seconds) for UI and tests.</summary>
        public float CurrentDurabilityValue
        {
            get
            {
                if (CurrentAttach == null || CurrentAttach.gameplayData == null) return 0f;
                switch (CurrentAttach.gameplayData.durabilityType)
                {
                    case DurabilityType.Ammo: return _currentAmmo;
                    case DurabilityType.Uses: return _currentUses;
                    case DurabilityType.TimeBased: return _timeRemaining;
                    default: return 0f;
                }
            }
        }

        public int CurrentAmmo => _currentAmmo;
        public int CurrentUses => _currentUses;
        public float TimeRemaining => _timeRemaining;
        public float TimeSinceLastShot => _timeSinceLastShot;
        private readonly HashSet<int> _validAnimParams = new HashSet<int>();

        private void InitAnimatorParams()
        {
            _validAnimParams.Clear();
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                foreach (var p in animator.parameters)
                {
                    _validAnimParams.Add(p.nameHash);
                }
            }
        }

        private void SafeSetBool(int hash, bool val)
        {
            if (animator != null && _validAnimParams.Contains(hash))
            {
                animator.SetBool(hash, val);
            }
        }

        private void SafeSetFloat(int hash, float val)
        {
            if (animator != null && _validAnimParams.Contains(hash))
            {
                animator.SetFloat(hash, val);
            }
        }

        private void SafeSetFloat(int hash, float val, float dampTime, float deltaTime)
        {
            if (animator != null && _validAnimParams.Contains(hash))
            {
                animator.SetFloat(hash, val, dampTime, deltaTime);
            }
        }

        private void SafeSetTrigger(int hash)
        {
            if (animator != null && _validAnimParams.Contains(hash))
            {
                animator.SetTrigger(hash);
            }
        }

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            InitAnimatorParams();
            if (motor == null) motor = GetComponent<CharacterMotor>();
            if (input == null) input = GetComponent<PlayerInputRouter>();
            _aimRig = GetComponent<PlayerAimRig>();
            _trajectoryPredictor = GetComponent<ThrowableTrajectoryPredictor>();
            if (_trajectoryPredictor == null) _trajectoryPredictor = gameObject.AddComponent<ThrowableTrajectoryPredictor>();

            if (motor != null)
            {
                _originalMoveSpeed = motor.moveSpeed;
            }

            _headTransform = FindChild(transform, "head");
            if (_headTransform != null) _initialHeadRotation = _headTransform.localRotation;
        }

        private void LateUpdate()
        {
            // Do not lock head during melee attacks (punch or knife), allowing natural head orientation
            bool isMelee = CurrentAttach == null || CurrentAttach.stanceId == 3 ||
                           (CurrentAttach.gameplayData != null && (CurrentAttach.gameplayData.category == WeaponCategory.Melee || CurrentAttach.gameplayData.name.Contains("Knife")));
            if (isMelee) return;

            bool isFiringOrCharging = _isCharging || _isAimingTrap || (animator != null && (animator.GetBool(HashIsFiring) || animator.GetCurrentAnimatorStateInfo(1).IsName("Shoot")));

            // Freeze/lock head rotation ONLY during movement while firing or charging ranged weapons
            if (isFiringOrCharging)
            {
                if (_headTransform != null)
                {
                    _headTransform.localRotation = _initialHeadRotation;
                }
            }
        }

        private IAimProvider Aim => motor != null ? motor.Aim : null;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _equippedAttachName.OnValueChanged += OnEquippedAttachNameChanged;

            if (IsOwner)
            {
                // Resolve camera only for the local player.
                if (playerFollowCamera == null)
                    playerFollowCamera = FindAnyObjectByType<CinemachineCamera>();

                if (playerFollowCamera != null)
                    _defaultFOV = playerFollowCamera.Lens.FieldOfView;

                // Create the scope overlay HUD for this local player only.
                if (scopeOverlay != null)
                {
                    _scopeOverlayInstance = Instantiate(scopeOverlay);
                    _scopeOverlayInstance.SetActive(false);
                }

                if (loadout != null && loadout.Length > 0 && loadout[0] != null)
                {
                    EquipNetworkedWeapon(loadout[0].name);
                }
            }

            string currentNetName = _equippedAttachName.Value.ToString();
            if (!string.IsNullOrEmpty(currentNetName))
            {
                EquipAttachByNameLocal(currentNetName, forceFresh: false);
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            _equippedAttachName.OnValueChanged -= OnEquippedAttachNameChanged;
        }

        private void OnEquippedAttachNameChanged(Unity.Collections.FixedString64Bytes previousValue, Unity.Collections.FixedString64Bytes newValue)
        {
            string newName = newValue.ToString();
            if (!string.IsNullOrEmpty(newName))
            {
                EquipAttachByNameLocal(newName, forceFresh: false);
            }
        }

        private void TriggerCombatStance()
        {
            _combatStanceTimer = COMBAT_STANCE_DURATION;
            if (motor != null) motor.IsInCombat = true;
            if (animator != null)
            {
                animator.SetBool(HashIsFiring, true);
                animator.SetFloat(HashShootSpeed, 1f);
            }
        }

        private void ExitCombatStance()
        {
            _combatStanceTimer = 0f;
            if (motor != null) motor.IsInCombat = false;
            if (animator != null)
            {
                animator.SetBool(HashIsFiring, false);
                animator.SetFloat(HashShootSpeed, 1f);
            }
        }

        private void Update()
        {
            _timeSinceLastShot += Time.deltaTime;
            if (_punchTimer > 0f) _punchTimer -= Time.deltaTime;

            // Manage Combat Stance persistence (holds upper-body aim for 1.5s after shooting)
            if (_combatStanceTimer > 0f)
            {
                _combatStanceTimer -= Time.deltaTime;
                if (_combatStanceTimer <= 0f)
                {
                    _combatStanceTimer = 0f;
                    if (motor != null) motor.IsInCombat = false;
                    if (animator != null)
                    {
                        animator.SetBool(HashIsFiring, false);
                        animator.SetFloat(HashShootSpeed, 1f);
                    }
                }
                else if (_combatStanceTimer <= COMBAT_STANCE_DURATION - 0.25f && animator != null)
                {
                    // Settle into steady aim pose after recoil completes
                    if (input != null && !input.Intent.Fire)
                    {
                        animator.SetFloat(HashShootSpeed, 0f);
                    }
                }
            }

            // Update charge visuals for remote players across all clients
            if (!IsOwner)
            {
                if (_isCharging && CurrentAttach != null && CurrentAttach.gameplayData != null)
                {
                    _chargeTimer += Time.deltaTime;
                    UpdateChargeVisual(CurrentAttach.gameplayData);
                }
                return;
            }
            if (input == null) return;

            // Weapon control gating
            bool isFrozen = WaitingRoomController.IsLobbyMenuOpen || PauseMenuController.IsMenuPaused;
            if (GameModeBase.Instance != null && GameModeBase.Instance.PreMatchCountdown.Value > 0)
            {
                isFrozen = true;
            }

            if (isFrozen)
            {
                if (_isADSActive) ExitADS();
                ExitCombatStance();
                _lastFire = false;
                return;
            }

            if (input.Intent.SwapWeapon)
                CycleWeapon();

            HandleFire();
            HandleADS();
            HandleFOVLerp();
            FeedAimPitch();
            UpdateDynamicStance();
        }

        private void HandleFOVLerp()
        {
            if (CurrentAttach == null || CurrentAttach.gameplayData == null || playerFollowCamera == null) return;

            float targetFOV = _isADSActive ? CurrentAttach.gameplayData.adsFOV : _defaultFOV;
            if (Mathf.Abs(playerFollowCamera.Lens.FieldOfView - targetFOV) > 0.1f)
            {
                float newFOV = Mathf.Lerp(playerFollowCamera.Lens.FieldOfView, targetFOV, Time.deltaTime * 10f);
                playerFollowCamera.Lens.FieldOfView = newFOV;
            }
        }

        private void HandleADS()
        {
            if (CurrentAttach == null) return;

            bool isSniper = CurrentAttach.name.Contains("Sniper");
            if (!isSniper)
            {
                if (_isADSActive) ExitADS();
                return;
            }

            // Auto scope if charging/firing the sniper or holding zoom (AltFire)
            bool shouldScope = _isCharging || input.Intent.AltFire;
            
            if (shouldScope && !_isADSActive)
            {
                EnterADS();
            }
            else if (!shouldScope && _isADSActive)
            {
                ExitADS();
            }
        }

        private void EnterADS()
        {
            _isADSActive = true;
            if (_scopeOverlayInstance != null) _scopeOverlayInstance.SetActive(true);

            if (motor != null && CurrentAttach.gameplayData != null)
            {
                motor.moveSpeed = _originalMoveSpeed * CurrentAttach.gameplayData.adsMoveSpeedMult;
            }
        }

        private void ExitADS()
        {
            _isADSActive = false;
            if (_scopeOverlayInstance != null) _scopeOverlayInstance.SetActive(false);

            if (motor != null)
            {
                motor.moveSpeed = _originalMoveSpeed;
            }
        }

        private void UpdateDynamicStance()
        {
            if (animator == null) return;

            if (CurrentAttach == null)
            {
                float cur = animator.GetFloat(HashStance);
                if (cur != 2.0f)
                {
                    animator.SetFloat(HashStance, 2.0f);
                }
                return;
            }

            // Throwable traps use Unarmed stance (2f) for all standard movement states
            if (CurrentAttach.gameplayData != null && (CurrentAttach.gameplayData.isTrap || CurrentAttach.gameplayData.category == WeaponCategory.Trap))
            {
                animator.SetFloat(HashStance, 2f);
                return;
            }

            // Knife / Melee stance (3f): uses Unarmed locomotion (Layer 0) and Knife Slash (Layer 1)
            if (CurrentAttach.stanceId == 3 || (CurrentAttach.gameplayData != null && CurrentAttach.gameplayData.name.Contains("Knife")))
            {
                animator.SetFloat(HashStance, 3f);
                return;
            }

            // Test logic specifically for the Sniper as requested
            if (CurrentAttach.name.Contains("Sniper"))
            {
                // Stance is Handgun (1f) only while the animator is actively in the Shoot state (Layer 1),
                // which includes charging (hold) and recoil playback. This avoids playing the handgun idle.
                bool isFiring = animator != null && animator.GetCurrentAnimatorStateInfo(1).IsName("Shoot");
                
                // Target stance: 0 (AR) when idle/moving, 1 (Handgun) when in the shoot state
                float targetStance = isFiring ? 1f : 0f;
                
                float currentStance = animator.GetFloat(HashStance);
                // Blend quickly but smoothly to avoid popping
                float newStance = Mathf.MoveTowards(currentStance, targetStance, Time.deltaTime * 6f);
                animator.SetFloat(HashStance, newStance);
            }
        }

        private void FeedAimPitch()
        {
            if (animator == null) return;
            // Reserved for future additive aim layer; vertical aim handled by IK head-look (Phase 7).
            float pitch01 = _aimRig != null ? Mathf.Clamp(_aimRig.Pitch / 45f, -1f, 1f) : 0f;
            animator.SetFloat(HashAimPitch, pitch01, 0.1f, Time.deltaTime);
        }

        public void GetTrapLaunchParameters(WeaponSO data, out Vector3 launchOrigin, out Vector3 launchVel)
        {
            float angle = (data != null && data.throwAngle > 0f) ? data.throwAngle : 20f;
            float force = (data != null && data.throwForce > 0f) ? data.throwForce : 14f;

            Transform camTrans = Camera.main != null ? Camera.main.transform : transform;
            launchOrigin = transform.position + Vector3.up * 1.5f + camTrans.forward * 0.5f;

            Vector3 launchDir = Quaternion.AngleAxis(-angle, camTrans.right) * camTrans.forward;
            launchVel = launchDir * force;
        }

        private void CancelTrapAim()
        {
            _isAimingTrap = false;
            if (animator != null)
            {
                SafeSetBool(HashIsThrowAiming, false);
                SafeSetFloat(HashShootSpeed, 1f);
                animator.CrossFade("Empty", 0.15f, -1);
            }
            if (motor != null) motor.MoveSpeedMultiplier = 1.0f;
            if (_trajectoryPredictor != null) _trajectoryPredictor.HideTrajectory();
        }

        private IEnumerator ResetAnimationToIdleRoutine()
        {
            yield return new WaitForSeconds(0.35f);
            if (animator != null)
            {
                animator.SetFloat(HashShootSpeed, 1f);
                animator.CrossFade("Locomotion", 0.15f, 0);
                animator.CrossFade("Empty", 0.15f, 1);
            }
        }

        private IEnumerator TrapCooldownRoutine()
        {
            _inTrapCooldown = true;
            yield return new WaitForSeconds(1.0f); // 1 second cooldown
            _inTrapCooldown = false;

            if (CurrentAttach != null && CurrentAttach.gameplayData != null)
            {
                if (CurrentDurabilityValue > 0)
                {
                    if (_currentVisual != null) _currentVisual.SetActive(true);
                }
                else
                {
                    ExpireWeapon();
                }
            }
        }

        private void HandleFire()
        {
            bool fireHeld = input.Intent.Fire;
            bool firePressed = fireHeld && !_lastFire;
            bool fireReleased = !fireHeld && _lastFire;
            _lastFire = fireHeld;

            if (!IsArmed)
            {
                if (firePressed && _timeSinceLastShot >= 0.4f)
                {
                    _timeSinceLastShot = 0f;
                    _punchTimer = 0.65f;
                    if (animator != null)
                    {
                        // Always use Punch Right (Stance = 2.0f) exclusively for unarmed punches
                        animator.SetFloat(HashStance, 2.0f);
                        animator.Play("Shoot", 1, 0f);
                        StartCoroutine(TriggerMeleeAnimation(0.60f));
                    }
                    // Delay hit infliction to frame 27 (0.45s) of the punch animation
                    StartCoroutine(DelayedMeleeHitRoutine(0.45f, 25f, null));
                }
                return;
            }

            var data = CurrentAttach.gameplayData;
            if (data == null) return;

            bool isBounceGun = data != null && (data.name.Contains("Trick") || data.name.Contains("Ricochet") || data.name.Contains("Bounce") || data.maxBounces > 0);
            if (isBounceGun)
            {
                if (fireHeld)
                {
                    UpdateBounceLinePreview(data);
                }
                else
                {
                    HideBounceLinePreview();
                }
            }
            else
            {
                HideBounceLinePreview();
            }

            bool isKnifeOrMelee = CurrentAttach.stanceId == 3 ||
                                  (data != null && (data.category == WeaponCategory.Melee || data.name.Contains("Knife") || CurrentAttach.name.Contains("Knife")));

            if (isKnifeOrMelee)
            {
                if (firePressed && CanFire(data) && _timeSinceLastShot >= data.FireRate)
                {
                    _timeSinceLastShot = 0f;
                    if (animator != null)
                    {
                        animator.SetFloat(HashStance, 3f);
                        animator.Play("Shoot", 1, 0f);
                        StartCoroutine(TriggerMeleeAnimation(0.55f));
                    }
                    StartCoroutine(DelayedMeleeHitRoutine(0.20f, data.Damage > 0 ? data.Damage : 65f, data));
                    DecrementDurability(data);
                }
                return;
            }

            if (data.isTrap || data.category == WeaponCategory.Trap)
            {
                if (_inTrapCooldown)
                {
                    if (_isAimingTrap) CancelTrapAim();
                    return;
                }

                if (fireHeld && CanFire(data) && _timeSinceLastShot >= data.FireRate)
                {
                    if (!_isAimingTrap)
                    {
                        _isAimingTrap = true;
                        if (animator != null)
                        {
                            SafeSetBool(HashIsThrowAiming, true);
                            SafeSetFloat(HashShootSpeed, 1f);
                            animator.CrossFade("ThrowBall_Prep", 0.05f, -1, 0f);
                        }
                        if (motor != null)
                        {
                            motor.MoveSpeedMultiplier = 0.25f; // 25% speed penalty while aiming
                        }
                    }


                    // Update trajectory line & impact marker in real time
                    if (_trajectoryPredictor != null)
                    {
                        GetTrapLaunchParameters(data, out Vector3 launchOrigin, out Vector3 launchVel);
                        _trajectoryPredictor.UpdateTrajectory(launchOrigin, launchVel);
                    }
                }
                else if (fireReleased && _isAimingTrap)
                {
                    _isAimingTrap = false;
                    if (animator != null)
                    {
                        SafeSetBool(HashIsThrowAiming, false);
                        SafeSetTrigger(HashThrowRelease);
                        SafeSetFloat(HashShootSpeed, 1f);
                        animator.CrossFade("ThrowBall_Release", 0.02f, -1, 0f);
                    }
                    if (motor != null) motor.MoveSpeedMultiplier = 1.0f;
                    if (_trajectoryPredictor != null) _trajectoryPredictor.HideTrajectory();

                    // Hide in-hand visual mesh immediately upon throw
                    if (_currentVisual != null) _currentVisual.SetActive(false);

                    GetTrapLaunchParameters(data, out Vector3 trapOrigin, out Vector3 trapVel);
                    FireProjectile(data, trapOrigin, trapVel);

                    // Reset animation back to idle immediately after throw gesture (0.35s), independent of cooldown
                    StartCoroutine(ResetAnimationToIdleRoutine());

                    // Begin 1-second throwable cooldown
                    if (_cooldownRoutine != null) StopCoroutine(_cooldownRoutine);
                    _cooldownRoutine = StartCoroutine(TrapCooldownRoutine());
                }
                else if (!fireHeld && _isAimingTrap)
                {
                    CancelTrapAim();
                }
            }
            else if (data.isCharged)
            {
                if (fireHeld && CanFire(data) && _timeSinceLastShot >= data.FireRate)
                {
                    if (!_isCharging)
                    {
                        // Start charging across network
                        StartCharge(data);
                    }
                    else
                    {
                        // Progress charge
                        _chargeTimer += Time.deltaTime;
                        UpdateChargeVisual(data);
                    }
                }
                else if (fireReleased && _isCharging)
                {
                    // Fire on release (mouse up) if charged
                    if (_chargeTimer >= data.chargeTime)
                    {
                        TriggerCombatStance();
                        FireProjectile(data);
                        if (animator != null)
                        {
                            animator.Play("Shoot", 1, 0f);
                            animator.SetFloat(HashShootSpeed, 1f); // Play recoil
                        }
                    }
                    CancelCharge();
                }
                else if (!fireHeld && _isCharging)
                {
                    CancelCharge();
                }
            }
            else
            {
                if (CurrentAttach.name.Contains("Flame"))
                {
                    if (fireHeld && CanFire(data))
                    {
                        bool isStartingFire = firePressed || (animator != null && (!animator.GetBool(HashIsFiring) || !animator.GetCurrentAnimatorStateInfo(1).IsName("Shoot")));
                        TriggerCombatStance();
                        if (animator != null)
                        {
                            if (isStartingFire)
                            {
                                animator.Play("Shoot", 1, 0f);
                            }
                            animator.SetFloat(HashShootSpeed, 0f); // Maintain steady forward aim stream stance
                        }
                        if (_timeSinceLastShot >= data.FireRate)
                        {
                            FireProjectile(data);
                        }
                    }
                    else
                    {
                        if (_combatStanceTimer <= 0f && animator != null && animator.GetBool(HashIsFiring))
                        {
                            animator.SetBool(HashIsFiring, false);
                            animator.SetFloat(HashShootSpeed, 1f);
                        }
                    }
                }
                else
                {
                    bool isAuto = data.isAutomatic || data.isAutoFire;
                    bool wantFire = isAuto ? fireHeld : firePressed;

                    if (wantFire && CanFire(data) && _timeSinceLastShot >= data.FireRate)
                    {
                        if (CurrentAttach == null && animator != null)
                        {
                            animator.SetFloat(HashStance, 2.0f);
                        }
                        TriggerCombatStance();
                        FireProjectile(data);
                        if (animator != null)
                        {
                            animator.Play("Shoot", 1, 0f);
                        }
                    }

                    if (isAuto && !fireHeld && _combatStanceTimer <= 0f && animator != null)
                    {
                        animator.SetBool(HashIsFiring, false);
                    }
                }
            }
        }

        private bool CanFire(WeaponSO data)
        {
            switch (data.durabilityType)
            {
                case DurabilityType.Ammo: return _currentAmmo > 0;
                case DurabilityType.Uses: return _currentUses > 0;
                case DurabilityType.TimeBased: return _timeRemaining > 0f;
                default: return true;
            }
        }

        private void FireProjectile(WeaponSO data, Vector3? customOrigin = null, Vector3? customVel = null)
        {
            _timeSinceLastShot = 0f;

            Vector3 origin = customOrigin.HasValue ? customOrigin.Value : (Muzzle != null ? Muzzle.position : transform.position + Vector3.up * 1.2f);
            
            // Ensure first shot from relaxed hip-carry aligns with the shoulder/chest aim height (no low-angle shots)
            if (!customOrigin.HasValue)
            {
                float minFireHeight = transform.position.y + 0.90f;
                if (origin.y < minFireHeight)
                {
                    origin.y = minFireHeight;
                }
            }
            
            // Camera / reticle aim reference
            Camera mainCam = Camera.main;
            Vector3 camForward = mainCam != null ? mainCam.transform.forward : (Aim != null ? Aim.CameraPlanarForward : transform.forward);

            Vector3 aimPoint = Aim != null ? Aim.AimPoint : origin + camForward * 100f;
            Vector3 dir = customVel.HasValue ? customVel.Value.normalized : (aimPoint - origin).normalized;

            // Parallax & Steep Angle Fix: When looking down at the ground close to the character in TPS,
            // the camera raycast hit point can lie behind or too close to the gun muzzle.
            // If the calculated direction points backwards relative to the camera, align it with camera forward.
            if (!customVel.HasValue)
            {
                if (Vector3.Dot(dir, camForward) <= 0.15f || Vector3.Dot(aimPoint - origin, camForward) <= 0.5f)
                {
                    dir = camForward;
                }
            }

            if (dir.sqrMagnitude < 0.0001f) dir = Muzzle != null ? Muzzle.forward : transform.forward;
            Vector3 launchVel = customVel.HasValue ? customVel.Value : (dir * (data.throwForce > 0f ? data.throwForce : 14f));
            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

            // Replicate firing, projectile spawning, and VFX to all clients
            FireProjectileRpc(_currentIndex, origin, rot, launchVel);

            // Locally predicted fire audio for the shooter
            if (data != null && data.audio != null && data.audio.fireClip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFXAtPosition(data.audio.fireClip, origin);
            }

            // Send to server for authoritative validation
            if (IsOwner)
            {
                FireWeaponServerRpc(origin, dir, data.name);
            }

            if (data.projectilePrefab != null)
            {
                var proj = Instantiate(data.projectilePrefab, origin, rot);
                var bp = proj.GetComponent<BaseProjectile>();
                if (bp != null) bp.Init(data, gameObject);
                var tb = proj.GetComponent<TrapBase>();
                if (tb != null)
                {
                    tb.Initialise((int)OwnerClientId, Camera.main, data, launchVel);
                }
            }
            else
            {
                // Hitscan: ray from muzzle toward the aim point (no Camera.main) for local visual feedback
                Vector3 rayDir = dir.normalized;
                if (Physics.Raycast(origin, rayDir, out RaycastHit hit, hitRange, hitMask, QueryTriggerInteraction.Ignore))
                {
                    GameObject hitVFX = data.HitVFXPrefab != null ? data.HitVFXPrefab : _fallbackHitscanImpactVFX;
                    if (hitVFX != null)
                    {
                        Instantiate(hitVFX, hit.point, Quaternion.LookRotation(hit.normal));
                    }
                    if (_hitscanImpactSound != null)
                    {
                        AudioSource.PlayClipAtPoint(_hitscanImpactSound, hit.point, 0.6f);
                    }
                }
            }

            if (_muzzleFlash != null) _muzzleFlash.Play();

            DecrementDurability(data);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void FireProjectileRpc(int weaponIndex, Vector3 origin, Quaternion rotation, Vector3 velocity)
        {
            // Owner already fired locally to ensure instant feedback
            if (IsOwner) return;

            WeaponAttachSO attach = null;
            if (loadout != null && weaponIndex >= 0 && weaponIndex < loadout.Length)
            {
                attach = loadout[weaponIndex];
            }
            if (attach == null)
            {
                attach = CurrentAttach;
            }
            if (attach == null) return;
            var data = attach.gameplayData;
            if (data == null) return;

            // Remote clients fire audio
            if (data.audio != null && data.audio.fireClip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFXAtPosition(data.audio.fireClip, origin);
            }

            if (data.projectilePrefab != null)
            {
                var proj = Instantiate(data.projectilePrefab, origin, rotation);
                var bp = proj.GetComponent<BaseProjectile>();
                if (bp != null) bp.Init(data, gameObject);
                var tb = proj.GetComponent<TrapBase>();
                if (tb != null) tb.Initialise((int)OwnerClientId, null, data, velocity);
            }
            else
            {
                // Hitscan
                Vector3 rayDir = rotation * Vector3.forward;
                if (Physics.Raycast(origin, rayDir, out RaycastHit hit, hitRange, hitMask, QueryTriggerInteraction.Ignore))
                {
                    GameObject hitVFX = data.HitVFXPrefab != null ? data.HitVFXPrefab : _fallbackHitscanImpactVFX;
                    if (hitVFX != null)
                    {
                        Instantiate(hitVFX, hit.point, Quaternion.LookRotation(hit.normal));
                    }
                    if (_hitscanImpactSound != null)
                    {
                        AudioSource.PlayClipAtPoint(_hitscanImpactSound, hit.point, 0.6f);
                    }
                }
            }

            if (_muzzleFlash != null) _muzzleFlash.Play();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void FireWeaponServerRpc(Vector3 firePoint, Vector3 aimDir, string weaponName)
        {
            if (!IsServer) return;

            var attach = CurrentAttach;
            if (attach == null && !string.IsNullOrEmpty(weaponName)) attach = GetAttachByName(weaponName);
            if (attach == null && loadout != null && _currentIndex >= 0 && _currentIndex < loadout.Length) attach = loadout[_currentIndex];
            if (attach == null && loadout != null && loadout.Length > 0) attach = loadout[0];
            if (attach == null) return;
            var data = attach.gameplayData;
            if (data == null) return;

            // Projectiles handle their own collision-based damage on the server
            if (data.projectilePrefab != null) return;

            Debug.Log($"[WeaponController] Server validating hitscan shot from Client {OwnerClientId} (Weapon: {weaponName})");

            if (Physics.Raycast(firePoint, aimDir, out RaycastHit hit, hitRange, hitMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
                {
                    var targetHealth = hit.collider.GetComponentInParent<PlayerHealth>();
                    if (targetHealth == null) targetHealth = hit.collider.GetComponent<PlayerHealth>();

                    bool wasAlive = targetHealth != null && targetHealth.GetCurrentHp() > 0f;

                    // Apply damage
                    if (targetHealth != null)
                    {
                        targetHealth.LastAttackerClientId = OwnerClientId;
                    }
                    damageable.TakeDamage(data.Damage, DamageType.Bullet, data);

                    // Report kill if target was killed
                    if (wasAlive && targetHealth != null && targetHealth.GetCurrentHp() <= 0f)
                    {
                        ulong killerId = OwnerClientId;
                        ulong victimId = targetHealth.OwnerClientId;

                        Debug.Log($"[WeaponController] Server detected hitscan kill: Player {killerId} killed Player {victimId} with {weaponName}");

                        if (NetworkedGameManager.Instance != null)
                        {
                            NetworkedGameManager.ReportKill(killerId, victimId, weaponName);
                        }

                    }
                }
            }
        }

        private void DecrementDurability(WeaponSO data)
        {
            switch (data.durabilityType)
            {
                case DurabilityType.Ammo:
                    _currentAmmo = Mathf.Max(0, _currentAmmo - 1);
                    onDurabilityChanged?.Raise(_currentAmmo);
                    if (_currentAmmo <= 0) ExpireWeapon();
                    break;
                case DurabilityType.Uses:
                    _currentUses = Mathf.Max(0, _currentUses - 1);
                    onDurabilityChanged?.Raise(_currentUses);
                    if (_currentUses <= 0) ExpireWeapon();
                    break;
                case DurabilityType.TimeBased:
                    break; // handled by coroutine
            }
        }

        private void InitDurability(WeaponSO data)
        {
            if (_timeRoutine != null) { StopCoroutine(_timeRoutine); _timeRoutine = null; }
            float max = 1f;
            switch (data.durabilityType)
            {
                case DurabilityType.Ammo:
                    _currentAmmo = data.MagazineSize; max = data.MagazineSize;
                    onDurabilityChanged?.Raise(_currentAmmo);
                    break;
                case DurabilityType.Uses:
                    _currentUses = data.maxUses; max = data.maxUses;
                    onDurabilityChanged?.Raise(_currentUses);
                    break;
                case DurabilityType.TimeBased:
                    _timeRemaining = data.weaponDuration; max = data.weaponDuration;
                    _timeRoutine = StartCoroutine(TimeBasedRoutine());
                    break;
            }
            onMaxDurabilityChanged?.Raise(max);
        }

        private IEnumerator TimeBasedRoutine()
        {
            while (_timeRemaining > 0f)
            {
                _timeRemaining -= Time.deltaTime;
                onDurabilityChanged?.Raise(_timeRemaining);
                yield return null;
            }
            ExpireWeapon();
        }

        private void ExpireWeapon()
        {
            if (IsServer)
            {
                _equippedAttachName.Value = "";
                ForceEquipWeaponRpc("");
            }
            else
            {
                EquipAttachByNameLocal("", forceFresh: true);
            }
            onWeaponExpired?.Raise();
            onDurabilityChanged?.Raise(0f);
        }

        public void CycleWeapon()
        {
            ExitCombatStance();
            if (loadout == null || loadout.Length == 0) return;
            int next = (_currentIndex + 1) % loadout.Length;
            if (loadout[next] != null)
            {
                _currentIndex = next;
                EquipNetworkedWeapon(loadout[next].name);
            }
        }

        public void EquipNetworkedWeapon(string attachName)
        {
            if (string.IsNullOrEmpty(attachName)) return;

            if (IsServer)
            {
                _equippedAttachName.Value = attachName;
                ForceEquipWeaponRpc(attachName);
            }
            else
            {
                RequestEquipServerRpc(attachName);
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestEquipServerRpc(string attachName)
        {
            if (!IsServer) return;
            _equippedAttachName.Value = attachName;
            ForceEquipWeaponRpc(attachName);
        }

        [Rpc(SendTo.Everyone)]
        private void ForceEquipWeaponRpc(string attachName)
        {
            EquipAttachByNameLocal(attachName, forceFresh: true);
        }

        public void EquipByAttachSO(WeaponAttachSO attach)
        {
            if (attach == null) return;
            EquipNetworkedWeapon(attach.name);
        }

        /// <summary>
        /// Replaces whatever weapon is currently in hand with a fresh copy of the
        /// given WeaponAttachSO. Always results in full ammo / durability.
        /// Replicates authoritatively to all clients.
        /// Used by loot box Chest 2 and Chest 3.
        /// </summary>
        public void ReplaceCurrentWeapon(WeaponAttachSO attach)
        {
            if (attach == null) return;
            EquipNetworkedWeapon(attach.name);
        }

        public void Equip(int index)
        {
            if (loadout != null && index >= 0 && index < loadout.Length && loadout[index] != null)
            {
                _currentIndex = index;
                EquipNetworkedWeapon(loadout[index].name);
            }
            else
            {
                ExpireWeapon();
            }
        }

        public void EquipAttachByNameLocal(string attachName, bool forceFresh = false)
        {
            if (string.IsNullOrEmpty(attachName))
            {
                CancelTrapAim();
                if (_cooldownRoutine != null) { StopCoroutine(_cooldownRoutine); _cooldownRoutine = null; }
                _inTrapCooldown = false;

                if (_timeRoutine != null) { StopCoroutine(_timeRoutine); _timeRoutine = null; }

                CancelCharge();
                ExitCombatStance();
                if (animator != null)
                {
                    animator.SetFloat(HashStance, 2f);
                }
                if (_isADSActive && IsOwner) ExitADS();

                if (_currentVisual != null) Destroy(_currentVisual);
                Muzzle = GripR = GripL = null;
                CurrentAttach = null;
                _muzzleFlash = null;
                _currentIndex = -1;
                return;
            }

            var attach = GetAttachByName(attachName);
            if (attach == null) return;

            if (!forceFresh && CurrentAttach == attach && _currentVisual != null) return;

            CancelTrapAim();
            if (_cooldownRoutine != null) { StopCoroutine(_cooldownRoutine); _cooldownRoutine = null; }
            _inTrapCooldown = false;

            if (attach.gameplayData != null && (attach.gameplayData.name.Contains("Health") || attach.name.Contains("Health")))
            {
                var hp = GetComponent<PlayerHealth>();
                if (hp == null) hp = GetComponentInParent<PlayerHealth>();
                if (hp != null)
                {
                    hp.Heal(50f);
                    Debug.Log($"[WeaponController] Health Box equipped by Client {OwnerClientId}! Restored 50 HP.");
                }
                ExpireWeapon();
                return;
            }

            CancelCharge();
            ExitCombatStance();

            // Tear down previous visual
            if (_currentVisual != null) Destroy(_currentVisual);
            Muzzle = GripR = GripL = null;

            CurrentAttach = attach;
            if (loadout != null)
            {
                for (int i = 0; i < loadout.Length; i++)
                {
                    if (loadout[i] != null && loadout[i].name == attachName)
                    {
                        _currentIndex = i;
                        break;
                    }
                }
            }

            // Reset durability & stats for new weapon
            var data = attach.gameplayData;
            if (data != null)
            {
                if (_timeRoutine != null) { StopCoroutine(_timeRoutine); _timeRoutine = null; }

                float max = 0f;
                switch (data.durabilityType)
                {
                    case DurabilityType.Ammo:
                        _currentAmmo = data.MagazineSize; max = data.MagazineSize;
                        onDurabilityChanged?.Raise(_currentAmmo);
                        break;
                    case DurabilityType.Uses:
                        _currentUses = data.maxUses; max = data.maxUses;
                        onDurabilityChanged?.Raise(_currentUses);
                        break;
                    case DurabilityType.TimeBased:
                        _timeRemaining = data.weaponDuration; max = data.weaponDuration;
                        _timeRoutine = StartCoroutine(TimeBasedRoutine());
                        break;
                }
                onMaxDurabilityChanged?.Raise(max);

                if (data.audio != null && data.audio.drawClip != null && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFXAtPosition(data.audio.drawClip, transform.position);
                }
            }

            if (attach.visualPrefab != null && weaponSocket != null)
            {
                _currentVisual = Instantiate(attach.visualPrefab, weaponSocket);
                _currentVisual.transform.localPosition = attach.equipLocalPos;
                _currentVisual.transform.localRotation = Quaternion.Euler(attach.equipLocalEuler);
                _currentVisual.transform.localScale = Vector3.one;

                var rbs = _currentVisual.GetComponentsInChildren<Rigidbody>(true);
                foreach (var rb in rbs) rb.isKinematic = true;

                var cols = _currentVisual.GetComponentsInChildren<Collider>(true);
                foreach (var c in cols) c.enabled = false;

                var traps = _currentVisual.GetComponentsInChildren<TrapBase>(true);
                foreach (var t in traps) t.enabled = false;

                Muzzle = FindChild(_currentVisual.transform, attach.muzzleChildName);
                GripR = FindChild(_currentVisual.transform, attach.gripRChildName);
                GripL = FindChild(_currentVisual.transform, attach.gripLChildName);
                _muzzleFlash = _currentVisual.GetComponentInChildren<ParticleSystem>(true);

                if (attach.snapRightGripToHand && GripR != null)
                {
                    Vector3 gripInSocket = weaponSocket.InverseTransformPoint(GripR.position);
                    _currentVisual.transform.localPosition -= gripInSocket;
                }
            }

            if (animator != null)
            {
                float stance = attach.stanceId;
                if (attach.stanceId == 3 || (data != null && (data.category == WeaponCategory.Melee || data.name.Contains("Knife"))))
                {
                    stance = 3f;
                }
                else if (data != null)
                {
                    stance = data.handedness == WeaponHandedness.OneHanded ? 1f : 0f;
                }
                animator.SetFloat(HashStance, stance);
            }

            Debug.Log($"[WeaponController] Player {OwnerClientId} equipped '{attach.name}' (Local)");
        }

        private void StartCharge(WeaponSO data)
        {
            if (IsSpawned)
            {
                StartChargeRpc();
            }
            else
            {
                StartChargeLocal();
            }
        }

        [Rpc(SendTo.Everyone)]
        private void StartChargeRpc()
        {
            StartChargeLocal();
        }

        private void StartChargeLocal()
        {
            _isCharging = true;
            _chargeTimer = 0f;
            var data = CurrentAttach != null ? CurrentAttach.gameplayData : null;
            if (data != null)
            {
                StartChargeVisual(data);

                // Play charge SFX hum (uses chargeClip or fallback drawClip as placeholder)
                if (data.chargeTime > 0f && data.audio != null && AudioManager.Instance != null && Muzzle != null)
                {
                    AudioClip clip = data.audio.chargeClip != null ? data.audio.chargeClip : data.audio.drawClip;
                    if (clip != null)
                    {
                        _activeChargeAudioSource = AudioManager.Instance.PlayLoopingSFX(clip, Muzzle, 0.75f, 0.8f);
                    }
                }
            }
            if (animator != null)
            {
                animator.SetBool(HashIsFiring, true);
                animator.SetFloat(HashShootSpeed, 0f); // Freeze at the first frame (aim/prep stance)
            }
        }

        private void CancelCharge()
        {
            if (IsSpawned)
            {
                CancelChargeRpc();
            }
            else
            {
                CancelChargeLocal();
            }
        }

        [Rpc(SendTo.Everyone)]
        private void CancelChargeRpc()
        {
            CancelChargeLocal();
        }

        private void CancelChargeLocal()
        {
            _isCharging = false;
            CancelTrapAim();
            _chargeTimer = 0f;
            if (_activeChargeVFX != null)
            {
                Destroy(_activeChargeVFX);
                _activeChargeVFX = null;
            }
            if (_activeChargeAudioSource != null)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.StopLoopingSFX(_activeChargeAudioSource);
                }
                _activeChargeAudioSource = null;
            }
            ExitCombatStance();
            if (animator != null)
            {
                SafeSetBool(HashIsThrowAiming, false);
            }
        }

        private LineRenderer _bounceLineRenderer;

        private void SetupBounceLineRenderer()
        {
            if (_bounceLineRenderer != null) return;
            GameObject lineObj = new GameObject("BounceLineRendererContainer");
            lineObj.transform.SetParent(transform, false);
            _bounceLineRenderer = lineObj.AddComponent<LineRenderer>();
            _bounceLineRenderer.useWorldSpace = true;
            _bounceLineRenderer.startWidth = 0.07f;
            _bounceLineRenderer.endWidth = 0.03f;
            _bounceLineRenderer.positionCount = 0;
            _bounceLineRenderer.enabled = false;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            Material lineMat = new Material(shader);
            Color neonGreen = new Color(0.15f, 1.0f, 0.35f, 0.85f);
            lineMat.renderQueue = 3000;
            if (lineMat.HasProperty("_BaseColor")) lineMat.SetColor("_BaseColor", neonGreen);
            if (lineMat.HasProperty("_Color")) lineMat.SetColor("_Color", neonGreen);
            _bounceLineRenderer.material = lineMat;
            _bounceLineRenderer.startColor = neonGreen;
            _bounceLineRenderer.endColor = neonGreen;
        }

        private void UpdateBounceLinePreview(WeaponSO data)
        {
            SetupBounceLineRenderer();
            if (_bounceLineRenderer == null) return;

            Vector3 origin;
            Vector3 aimDir;

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Ray camRay = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                origin = camRay.origin;
                aimDir = camRay.direction;
            }
            else
            {
                origin = Muzzle != null ? Muzzle.position : transform.position + Vector3.up * 1.4f;
                aimDir = transform.forward;
                if (Aim != null && Aim.AimPoint != Vector3.zero)
                {
                    aimDir = (Aim.AimPoint - origin).normalized;
                }
            }

            if (aimDir.sqrMagnitude < 0.001f) aimDir = transform.forward;
            aimDir.Normalize();

            float firstDist = data != null && data.previewDistance > 0f ? data.previewDistance : 25f;
            float bounceDist = 15f;
            LayerMask mask = ~0;

            var points = new System.Collections.Generic.List<Vector3>();
            points.Add(origin);

            if (Physics.Raycast(origin, aimDir, out RaycastHit hit1, firstDist, mask, QueryTriggerInteraction.Ignore))
            {
                points.Add(hit1.point);

                Vector3 bounceDir = Vector3.Reflect(aimDir, hit1.normal).normalized;
                if (Physics.Raycast(hit1.point + hit1.normal * 0.01f, bounceDir, out RaycastHit hit2, bounceDist, mask, QueryTriggerInteraction.Ignore))
                {
                    points.Add(hit2.point);
                }
                else
                {
                    points.Add(hit1.point + bounceDir * bounceDist);
                }
            }
            else
            {
                points.Add(origin + aimDir * firstDist);
            }

            _bounceLineRenderer.positionCount = points.Count;
            _bounceLineRenderer.SetPositions(points.ToArray());
            _bounceLineRenderer.enabled = true;
        }

        private void HideBounceLinePreview()
        {
            if (_bounceLineRenderer != null)
            {
                _bounceLineRenderer.enabled = false;
            }
        }

        private void StartChargeVisual(WeaponSO data)
        {
            if (Muzzle == null || data == null || data.chargeTime <= 0f) return;

            // Clean up old charge visual if existing
            if (_activeChargeVFX != null)
            {
                Destroy(_activeChargeVFX);
                _activeChargeVFX = null;
            }

            Color chargeColor = GetProjectileColor(data);

            // Container object attached to Muzzle
            _activeChargeVFX = new GameObject("ChargeVFXContainer");
            _activeChargeVFX.transform.SetParent(Muzzle, false);
            _activeChargeVFX.transform.localPosition = Vector3.zero;
            _activeChargeVFX.transform.localRotation = Quaternion.identity;
            _activeChargeVFX.transform.localScale = Vector3.one;

            // 1. Refined Translucent Glowing Charge Sphere
            GameObject sphereObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphereObj.name = "ChargeVisualSphere";
            sphereObj.transform.SetParent(_activeChargeVFX.transform, false);
            sphereObj.transform.localPosition = Vector3.zero;
            sphereObj.transform.localRotation = Quaternion.identity;
            sphereObj.transform.localScale = Vector3.one * 0.02f;

            Collider col = sphereObj.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);

            MeshRenderer mr = sphereObj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");

                Material mat = new Material(shader);
                mat.name = "ChargeSphere_RefinedMat";
                mat.renderQueue = 3000;

                Color baseCol = new Color(chargeColor.r, chargeColor.g, chargeColor.b, 0.25f);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseCol);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseCol);

                mat.EnableKeyword("_EMISSION");
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.EnableKeyword("_ALPHABLEND_ON");

                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", chargeColor * 3.5f);
                }

                // Enable alpha blending / transparency
                if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1); // 1 = Transparent in URP
                if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0); // 0 = Alpha blend
                if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0);

                mr.material = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }

            // 2. Instantiate Implosion VFX (vfx_Implosion_01.prefab)
            GameObject prefabToUse = vfxImplosionPrefab;
            if (prefabToUse == null)
            {
#if UNITY_EDITOR
                prefabToUse = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GabrielAguiarProductions/FreeQuickEffectsVol1/Prefabs/vfx_Implosion_01.prefab");
#endif
            }

            if (prefabToUse != null)
            {
                GameObject implosionInst = Instantiate(prefabToUse, _activeChargeVFX.transform, false);
                implosionInst.name = "ImplosionVFX";
                implosionInst.transform.localPosition = Vector3.zero;
                implosionInst.transform.localRotation = Quaternion.identity;
                implosionInst.transform.localScale = Vector3.one * 0.175f;

                // Tint particle systems in the implosion prefab to match projectile color
                ParticleSystem[] particles = implosionInst.GetComponentsInChildren<ParticleSystem>(true);
                foreach (ParticleSystem ps in particles)
                {
                    var main = ps.main;
                    Color pCol = chargeColor;
                    pCol.a = main.startColor.color.a > 0.01f ? main.startColor.color.a : 0.8f;
                    main.startColor = pCol;
                }
            }
        }

        private void UpdateChargeVisual(WeaponSO data)
        {
            if (data == null || data.chargeTime <= 0f) return;

            float t = Mathf.Clamp01(_chargeTimer / data.chargeTime);

            // Pitch-bend active charge audio from 0.8 pitch up to 1.6 pitch as charge builds
            if (_activeChargeAudioSource != null)
            {
                _activeChargeAudioSource.pitch = Mathf.Lerp(0.8f, 1.6f, t);
            }

            if (_activeChargeVFX == null) return;

            Transform sphereTr = _activeChargeVFX.transform.Find("ChargeVisualSphere");
            if (sphereTr != null)
            {
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                float maxSphereScale = 0.28f;
                float currentScale = Mathf.Lerp(0.02f, maxSphereScale, smoothT);

                // Dynamically scale and pulse
                if (t >= 1.0f)
                {
                    float pulse = Mathf.Sin(Time.time * 18f) * 0.015f;
                    sphereTr.localScale = Vector3.one * (maxSphereScale + pulse);
                }
                else
                {
                    sphereTr.localScale = Vector3.one * currentScale;
                }

                // Update sphere material alpha and emission intensity as charge builds up
                MeshRenderer mr = sphereTr.GetComponent<MeshRenderer>();
                if (mr != null && mr.material != null)
                {
                    Color chargeColor = GetProjectileColor(data);
                    float alpha = Mathf.Lerp(0.2f, 0.65f, smoothT);
                    Color baseCol = new Color(chargeColor.r, chargeColor.g, chargeColor.b, alpha);

                    if (mr.material.HasProperty("_BaseColor")) mr.material.SetColor("_BaseColor", baseCol);
                    if (mr.material.HasProperty("_Color")) mr.material.SetColor("_Color", baseCol);

                    if (mr.material.HasProperty("_EmissionColor"))
                    {
                        float emMult = Mathf.Lerp(1.5f, 4.0f, smoothT);
                        mr.material.SetColor("_EmissionColor", chargeColor * emMult);
                    }
                }
            }

            // Adjust implosion particle simulation scale (halved to 0.1f -> 0.25f)
            Transform implosionTr = _activeChargeVFX.transform.Find("ImplosionVFX");
            if (implosionTr != null)
            {
                float implosionScale = Mathf.Lerp(0.1f, 0.25f, t);
                implosionTr.localScale = Vector3.one * implosionScale;
            }
        }

        private Color GetProjectileColor(WeaponSO data)
        {
            if (data == null) return new Color(0.3f, 0.7f, 1.0f, 0.9f);

            string nameLower = (data.weaponName + " " + (data.name ?? "")).ToLowerInvariant();

            // Special refined theme color matching
            if (nameLower.Contains("void") || nameLower.Contains("gravity"))
            {
                return new Color(0.55f, 0.08f, 0.95f, 0.7f); // Deep Cosmic Purple/Black Void
            }
            if (nameLower.Contains("area") || nameLower.Contains("blaster") || nameLower.Contains("explosive"))
            {
                return new Color(1.0f, 0.25f, 0.02f, 0.7f); // Fiery Red-Orange Blast
            }
            if (nameLower.Contains("freeze") || nameLower.Contains("cryo") || nameLower.Contains("ice"))
            {
                return new Color(0.15f, 0.85f, 1.0f, 0.65f); // Ice Cyan Frost
            }
            if (nameLower.Contains("trick") || nameLower.Contains("ricochet") || nameLower.Contains("bounce"))
            {
                return new Color(0.15f, 1.0f, 0.35f, 0.65f); // Neon Radioactive Green
            }
            if (nameLower.Contains("sniper"))
            {
                return new Color(1.0f, 0.85f, 0.15f, 0.7f); // Golden Amber / Electric Yellow
            }

            // 1. Try to extract color directly from the projectilePrefab materials or renderers
            if (data.projectilePrefab != null)
            {
                Renderer[] renderers = data.projectilePrefab.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer r in renderers)
                {
                    if (r == null || r.sharedMaterials == null) continue;
                    foreach (Material mat in r.sharedMaterials)
                    {
                        if (mat == null) continue;

                        if (mat.HasProperty("_EmissionColor"))
                        {
                            Color em = mat.GetColor("_EmissionColor");
                            if (em.r > 0.05f || em.g > 0.05f || em.b > 0.05f)
                            {
                                float maxCh = Mathf.Max(em.r, Mathf.Max(em.g, em.b));
                                if (maxCh > 1f) em /= maxCh;
                                em.a = 0.8f;
                                return em;
                            }
                        }

                        if (mat.HasProperty("_BaseColor"))
                        {
                            Color baseCol = mat.GetColor("_BaseColor");
                            if (baseCol.a > 0.05f && (baseCol.r > 0.05f || baseCol.g > 0.05f || baseCol.b > 0.05f))
                            {
                                baseCol.a = 0.8f;
                                return baseCol;
                            }
                        }

                        if (mat.HasProperty("_Color"))
                        {
                            Color col = mat.GetColor("_Color");
                            if (col.a > 0.05f && (col.r > 0.05f || col.g > 0.05f || col.b > 0.05f))
                            {
                                col.a = 0.8f;
                                return col;
                            }
                        }
                    }
                }
            }

            return new Color(0.3f, 0.7f, 1.0f, 0.8f); // Electric Blue fallback
        }

        /// <summary>
        /// Executed when performing a melee hit (punch or knife slash). Uses a close-range spherecast around aim direction.
        /// </summary>
        public void PerformMeleeHit(float damage = 25f, WeaponSO weaponUsed = null)
        {
            if (!IsOwner) return;

            Vector3 origin = transform.position + Vector3.up * 1.2f;
            Vector3 aimDir = transform.forward;

            if (Aim != null && Aim.AimPoint != Vector3.zero)
            {
                Vector3 dirToAim = (Aim.AimPoint - origin).normalized;
                if (dirToAim.sqrMagnitude > 0.001f) aimDir = dirToAim;
            }

            float meleeRange = 2.5f;   // Close to body melee range
            float meleeRadius = 0.8f;  // Forgiving spherecast radius (close to body)

            RaycastHit[] hits = Physics.SphereCastAll(origin, meleeRadius, aimDir, meleeRange, hitMask, QueryTriggerInteraction.Ignore);

            foreach (var hit in hits)
            {
                if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
                    continue;

                var targetHealth = hit.collider.GetComponentInParent<PlayerHealth>();
                if (targetHealth == null) targetHealth = hit.collider.GetComponent<PlayerHealth>();

                var damageable = hit.collider.GetComponent<IDamageable>();
                if (damageable == null && targetHealth != null) damageable = targetHealth;
                if (damageable == null) damageable = hit.collider.GetComponentInParent<IDamageable>();

                if (damageable != null)
                {
                    if (targetHealth != null)
                    {
                        targetHealth.LastAttackerClientId = OwnerClientId;
                    }
                    damageable.TakeDamage(damage, DamageType.Melee, weaponUsed);
                    Debug.Log($"[WeaponController] Melee Hit registered on {hit.collider.name} for {damage} damage! (Weapon: {(weaponUsed != null ? weaponUsed.weaponName : "Punch")})");
                    break;
                }
            }
        }

        public void OnMeleeHit()
        {
            float damage = 25f;
            WeaponSO data = null;
            if (CurrentAttach != null && CurrentAttach.gameplayData != null)
            {
                data = CurrentAttach.gameplayData;
                if (data.Damage > 0) damage = data.Damage;
            }
            PerformMeleeHit(damage, data);
        }

        private IEnumerator DelayedMeleeHitRoutine(float delaySeconds, float damage, WeaponSO weaponUsed)
        {
            yield return new WaitForSeconds(delaySeconds);
            PerformMeleeHit(damage, weaponUsed);
        }

        private IEnumerator TriggerMeleeAnimation(float duration)
        {
            if (animator == null) yield break;
            animator.SetBool(HashIsFiring, true);
            animator.SetFloat(HashShootSpeed, 1f);
            yield return new WaitForSeconds(duration);
            animator.SetBool(HashIsFiring, false);
        }

        private static Transform FindChild(Transform root, string childName)
        {
            if (string.IsNullOrEmpty(childName)) return null;
            if (root.name == childName) return root;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == childName) return t;
            return null;
        }
    }
}
