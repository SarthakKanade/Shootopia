using System.Collections;
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
        private GameObject _activeChargeVFX;
        private bool _isAimingTrap;
        private bool _inTrapCooldown;
        private Coroutine _cooldownRoutine;
        private ThrowableTrajectoryPredictor _trajectoryPredictor;

        private readonly NetworkVariable<int> _equippedWeaponIndex = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private int _currentIndex = -1;
        private GameObject _currentVisual;
        private PlayerAimRig _aimRig;
        private ParticleSystem _muzzleFlash;
        private Transform _neckTransform;
        private Quaternion _initialNeckRotation;

        // Firing / durability runtime
        private float _timeSinceLastShot;
        private bool _lastFire;
        private int _currentAmmo;
        private int _currentUses;
        private float _timeRemaining;
        private Coroutine _timeRoutine;

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
        public bool IsCharging => _isCharging;
        public float ChargeTimer => _chargeTimer;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            if (motor == null) motor = GetComponent<CharacterMotor>();
            if (input == null) input = GetComponent<PlayerInputRouter>();
            _aimRig = GetComponent<PlayerAimRig>();
            _trajectoryPredictor = GetComponent<ThrowableTrajectoryPredictor>();
            if (_trajectoryPredictor == null) _trajectoryPredictor = gameObject.AddComponent<ThrowableTrajectoryPredictor>();

            // Scope overlay and camera FOV are HUD/local-owner concerns.
            // Instantiated in OnNetworkSpawn once IsOwner is known.

            // playerFollowCamera and defaultFOV are only needed by the local owner.
            // Do NOT cache them here; wait until OnNetworkSpawn so IsOwner is valid.
            if (motor != null)
            {
                _originalMoveSpeed = motor.moveSpeed;
            }

            _neckTransform = FindChild(transform, "neck_01");
            if (_neckTransform == null) _neckTransform = FindChild(transform, "head");
            if (_neckTransform != null) _initialNeckRotation = _neckTransform.localRotation;
        }

        private void LateUpdate()
        {
            // Freeze neck rotation to static during knife animation / knife stance
            if (CurrentAttach != null && (CurrentAttach.stanceId == 3 || (CurrentAttach.gameplayData != null && (CurrentAttach.gameplayData.category == WeaponCategory.Melee || CurrentAttach.gameplayData.name.Contains("Knife")))))
            {
                if (_neckTransform != null)
                {
                    _neckTransform.localRotation = _initialNeckRotation;
                }
            }
        }

            private IAimProvider Aim => motor != null ? motor.Aim : null;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _equippedWeaponIndex.OnValueChanged += OnEquippedWeaponChanged;

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

                if (loadout != null && loadout.Length > 0)
                    Equip(0);
            }
            else
            {
                if (_equippedWeaponIndex.Value >= 0)
                {
                    Equip(_equippedWeaponIndex.Value);
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            _equippedWeaponIndex.OnValueChanged -= OnEquippedWeaponChanged;
        }

        private void OnEquippedWeaponChanged(int previousValue, int newValue)
        {
            if (!IsOwner)
            {
                Equip(newValue);
            }
        }

        private void Update()
        {
            // Only the owning client drives input, firing, ADS, and camera FOV.
            if (!IsOwner) return;
            if (input == null) return;

            // Weapon control gating
            bool isFrozen = false;
            string activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            if (activeSceneName == "WaitingRoom")
            {
                isFrozen = WaitingRoomController.IsLobbyMenuOpen;
            }
            else if (activeSceneName == "GameScene")
            {
                if (GameModeBase.Instance != null && !GameModeBase.Instance.MatchActive.Value)
                {
                    isFrozen = true;
                }
            }

            if (isFrozen)
            {
                if (_isADSActive) ExitADS();
                if (animator != null)
                {
                    animator.SetBool(HashIsFiring, false);
                }
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
                animator.SetFloat(HashStance, 2f);
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
                animator.SetBool(HashIsThrowAiming, false);
                animator.SetFloat(HashShootSpeed, 1f);
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
            _timeSinceLastShot += Time.deltaTime;

            bool fireHeld = input.Intent.Fire;
            bool firePressed = fireHeld && !_lastFire;
            bool fireReleased = !fireHeld && _lastFire;
            _lastFire = fireHeld;

            if (!IsArmed)
            {
                if (firePressed && _timeSinceLastShot >= 0.4f)
                {
                    _timeSinceLastShot = 0f;
                    if (animator != null)
                    {
                        animator.Play("Shoot", 1, 0f);
                        StartCoroutine(TriggerShootAnimationOnce());
                    }
                    PerformMeleeHit(25f, null);
                }
                return;
            }

            var data = CurrentAttach.gameplayData;
            if (data == null) return;

            bool isKnifeOrMelee = CurrentAttach.stanceId == 3 ||
                                  (data != null && (data.category == WeaponCategory.Melee || data.name.Contains("Knife") || CurrentAttach.name.Contains("Knife")));

            if (isKnifeOrMelee)
            {
                if (firePressed && CanFire(data) && _timeSinceLastShot >= data.FireRate)
                {
                    _timeSinceLastShot = 0f;
                    if (animator != null)
                    {
                        animator.Play("Shoot", 1, 0f);
                        StartCoroutine(TriggerShootAnimationOnce());
                    }
                    PerformMeleeHit(data.Damage > 0 ? data.Damage : 50f, data);
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
                            animator.SetBool(HashIsThrowAiming, true);
                            animator.SetFloat(HashShootSpeed, 1f);
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
                        animator.SetBool(HashIsThrowAiming, false);
                        animator.SetTrigger(HashThrowRelease);
                        animator.SetFloat(HashShootSpeed, 1f);
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
                        // Start charging
                        _isCharging = true;
                        _chargeTimer = 0f;
                        if (defaultChargeVFXPrefab != null && Muzzle != null && !CurrentAttach.name.Contains("Sniper"))
                        {
                            _activeChargeVFX = Instantiate(defaultChargeVFXPrefab, Muzzle);
                            _activeChargeVFX.transform.localPosition = Vector3.zero;
                            _activeChargeVFX.transform.localRotation = Quaternion.identity;
                        }
                        if (animator != null)
                        {
                            animator.SetBool(HashIsFiring, true);
                            animator.SetFloat(HashShootSpeed, 0f); // Freeze at the first frame (aim/prep stance)
                        }
                    }
                    else
                    {
                        // Progress charge
                        _chargeTimer += Time.deltaTime;
                    }
                }
                else if (fireReleased && _isCharging)
                {
                    // Fire on release (mouse up) if charged
                    if (_chargeTimer >= data.chargeTime)
                    {
                        FireProjectile(data);
                        if (animator != null)
                        {
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
                        if (animator != null)
                        {
                            animator.SetBool(HashIsFiring, true);
                            animator.SetFloat(HashShootSpeed, 0f); // Go to shoot stance, but never play recoil
                        }
                        if (_timeSinceLastShot >= data.FireRate)
                        {
                            FireProjectile(data);
                        }
                    }
                    else
                    {
                        if (animator != null && animator.GetBool(HashIsFiring))
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
                        FireProjectile(data);
                        if (animator != null)
                        {
                            animator.Play("Shoot", 1, 0f);
                            if (isAuto)
                            {
                                animator.SetBool(HashIsFiring, true);
                            }
                            else
                            {
                                StartCoroutine(TriggerShootAnimationOnce());
                            }
                        }
                    }

                    if (isAuto && !fireHeld && animator != null)
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

            Vector3 origin = customOrigin.HasValue ? customOrigin.Value : (Muzzle != null ? Muzzle.position : transform.position);
            Vector3 aimPoint = Aim != null ? Aim.AimPoint : origin + transform.forward * 100f;
            Vector3 dir = customVel.HasValue ? customVel.Value.normalized : (aimPoint - origin).normalized;
            if (dir.sqrMagnitude < 0.0001f) dir = Muzzle != null ? Muzzle.forward : transform.forward;
            Vector3 launchVel = customVel.HasValue ? customVel.Value : (dir * (data.throwForce > 0f ? data.throwForce : 14f));
            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

            // Replicate firing, projectile spawning, and VFX to all clients
            FireProjectileRpc(_currentIndex, origin, rot, launchVel);

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

            if (loadout == null || weaponIndex < 0 || weaponIndex >= loadout.Length) return;
            var attach = loadout[weaponIndex];
            if (attach == null) return;
            var data = attach.gameplayData;
            if (data == null) return;

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

            if (loadout == null || _currentIndex < 0 || _currentIndex >= loadout.Length) return;
            var attach = loadout[_currentIndex];
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
            Equip(-1);
            onWeaponExpired?.Raise();
            onDurabilityChanged?.Raise(0f);
        }

        public void CycleWeapon()
        {
            if (loadout == null || loadout.Length == 0) return;
            int next = (_currentIndex + 1) % loadout.Length;
            Equip(next);
        }

        public void EquipByAttachSO(WeaponAttachSO attach)
        {
            if (attach == null) return;

            int index = -1;
            if (loadout != null)
            {
                for (int i = 0; i < loadout.Length; i++)
                {
                    if (loadout[i] == attach || (loadout[i] != null && loadout[i].name == attach.name))
                    {
                        index = i;
                        break;
                    }
                }
            }

            if (index < 0)
            {
                int currentLen = loadout != null ? loadout.Length : 0;
                System.Array.Resize(ref loadout, currentLen + 1);
                index = currentLen;
                loadout[index] = attach;
            }

            Equip(index);
        }

        /// <summary>
        /// Replaces whatever weapon is currently in hand with a fresh copy of the
        /// given WeaponAttachSO. Always results in full ammo / durability because
        /// we force-overwrite the loadout slot and re-equip from scratch.
        /// Used by loot box Chest 2 and Chest 3.
        /// </summary>
        public void ReplaceCurrentWeapon(WeaponAttachSO attach)
        {
            if (attach == null) return;

            // Use the current slot, or slot 0 if nothing is equipped
            int slot = _currentIndex >= 0 ? _currentIndex : 0;

            // Ensure loadout array is large enough
            if (loadout == null || loadout.Length == 0)
            {
                loadout = new WeaponAttachSO[1];
                slot = 0;
            }
            else if (slot >= loadout.Length)
            {
                slot = 0;
            }

            // Tear down current weapon first so Equip treats it as fresh
            Equip(-1);

            // Overwrite the slot with the new weapon
            loadout[slot] = attach;

            // Equip the fresh weapon — Equip() initialises ammo/durability from scratch
            Equip(slot);

            Debug.Log($"[WeaponController] ReplaceCurrentWeapon: slot {slot} → {attach.name}");
        }

        public void Equip(int index)
        {
            CancelTrapAim();
            if (_cooldownRoutine != null) { StopCoroutine(_cooldownRoutine); _cooldownRoutine = null; }
            _inTrapCooldown = false;

            if (loadout == null || index < 0 || index >= loadout.Length)
            {
                if (IsOwner)
                {
                    _equippedWeaponIndex.Value = -1;
                }
                if (_timeRoutine != null) { StopCoroutine(_timeRoutine); _timeRoutine = null; }

                CancelCharge();
                if (animator != null)
                {
                    animator.SetBool(HashIsFiring, false);
                    animator.SetFloat(HashShootSpeed, 1f);
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
            var attach = loadout[index];
            if (attach == null) return;

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

            if (IsOwner)
            {
                _equippedWeaponIndex.Value = index;
            }

            CancelCharge();
            if (animator != null)
            {
                animator.SetBool(HashIsFiring, false);
                animator.SetFloat(HashShootSpeed, 1f);
            }

            // Tear down previous visual
            if (_currentVisual != null) Destroy(_currentVisual);
            Muzzle = GripR = GripL = null;

            _currentIndex = index;
            CurrentAttach = attach;

            if (attach.visualPrefab != null && weaponSocket != null)
            {
                _currentVisual = Instantiate(attach.visualPrefab, weaponSocket);
                _currentVisual.transform.localPosition = attach.equipLocalPos;
                _currentVisual.transform.localRotation = Quaternion.Euler(attach.equipLocalEuler);
                _currentVisual.transform.localScale = Vector3.one;

                // Disable physics, colliders, and trap execution scripts on the in-hand visual copy
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
                if (attach.stanceId == 3 || (attach.gameplayData != null && attach.gameplayData.name.Contains("Knife")))
                {
                    stance = 3f;
                }
                else if (attach.gameplayData != null)
                {
                    if (attach.gameplayData.isTrap || attach.gameplayData.category == WeaponCategory.Trap)
                        stance = 2f;
                    else
                        stance = attach.gameplayData.handedness == Shootopia.WeaponHandedness.OneHanded ? 1f : 0f;
                }
                animator.SetFloat(HashStance, stance);
            }

            _timeSinceLastShot = float.MaxValue; // allow immediate first shot
            if (attach.gameplayData != null)
                InitDurability(attach.gameplayData);
        }

        private void CancelCharge()
        {
            _isCharging = false;
            CancelTrapAim();
            _chargeTimer = 0f;
            if (_activeChargeVFX != null)
            {
                Destroy(_activeChargeVFX);
                _activeChargeVFX = null;
            }
            if (animator != null)
            {
                animator.SetBool(HashIsFiring, false);
                animator.SetBool(HashIsThrowAiming, false);
                animator.SetFloat(HashShootSpeed, 1f);
            }
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

        private IEnumerator TriggerShootAnimationOnce()
        {
            if (animator == null) yield break;
            animator.SetBool(HashIsFiring, true);
            yield return null;
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
