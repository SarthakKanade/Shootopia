using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using VelocityWars;
using VelocityWars.Core;
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

        // Charging state
        private bool _isCharging;
        private float _chargeTimer;
        private GameObject _activeChargeVFX;

        private readonly NetworkVariable<int> _equippedWeaponIndex = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private int _currentIndex = -1;
        private GameObject _currentVisual;
        private PlayerAimRig _aimRig;
        private ParticleSystem _muzzleFlash;

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

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            if (motor == null) motor = GetComponent<CharacterMotor>();
            if (input == null) input = GetComponent<PlayerInputRouter>();
            _aimRig = GetComponent<PlayerAimRig>();

            // Scope overlay and camera FOV are HUD/local-owner concerns.
            // Instantiated in OnNetworkSpawn once IsOwner is known.

            // playerFollowCamera and defaultFOV are only needed by the local owner.
            // Do NOT cache them here; wait until OnNetworkSpawn so IsOwner is valid.
            if (motor != null)
            {
                _originalMoveSpeed = motor.moveSpeed;
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

        private void HandleFire()
        {
            _timeSinceLastShot += Time.deltaTime;

            bool fireHeld = input.Intent.Fire;
            bool firePressed = fireHeld && !_lastFire;
            bool fireReleased = !fireHeld && _lastFire;
            _lastFire = fireHeld;

            if (!IsArmed)
            {
                if (animator != null)
                {
                    animator.SetBool(HashIsFiring, firePressed);
                }
                return;
            }

            var data = CurrentAttach.gameplayData;
            if (data == null) return;

            if (data.isCharged)
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
                            if (isAuto)
                            {
                                animator.SetBool(HashIsFiring, true);
                                animator.Play("Shoot", 1, 0f); // Play shoot animation on every shot in hold
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

        private void FireProjectile(WeaponSO data)
        {
            _timeSinceLastShot = 0f;

            Vector3 origin = Muzzle != null ? Muzzle.position : transform.position;
            Vector3 aimPoint = Aim != null ? Aim.AimPoint : origin + transform.forward * 100f;
            Vector3 dir = (aimPoint - origin);
            if (dir.sqrMagnitude < 0.0001f) dir = Muzzle != null ? Muzzle.forward : transform.forward;
            Quaternion rot = Quaternion.LookRotation(dir.normalized, Vector3.up);

            // Replicate firing, projectile spawning, and VFX to all clients
            FireProjectileRpc(_currentIndex, origin, rot);

            if (data.projectilePrefab != null)
            {
                var proj = Instantiate(data.projectilePrefab, origin, rot);
                var bp = proj.GetComponent<BaseProjectile>();
                if (bp != null) bp.Init(data, gameObject);
            }
            else
            {
                // Hitscan: ray from muzzle toward the aim point (no Camera.main).
                Vector3 rayDir = dir.normalized;
                if (Physics.Raycast(origin, rayDir, out RaycastHit hit, hitRange, hitMask, QueryTriggerInteraction.Ignore))
                {
                    if (data.HitVFXPrefab != null)
                        Instantiate(data.HitVFXPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                    if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
                        damageable.TakeDamage(data.Damage, DamageType.Player, data);
                }
            }

            if (_muzzleFlash != null) _muzzleFlash.Play();

            DecrementDurability(data);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void FireProjectileRpc(int weaponIndex, Vector3 origin, Quaternion rotation)
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
            }
            else
            {
                // Hitscan
                Vector3 rayDir = rotation * Vector3.forward;
                if (Physics.Raycast(origin, rayDir, out RaycastHit hit, hitRange, hitMask, QueryTriggerInteraction.Ignore))
                {
                    if (data.HitVFXPrefab != null)
                        Instantiate(data.HitVFXPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                }
            }

            if (_muzzleFlash != null) _muzzleFlash.Play();
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
            if (_timeRoutine != null) { StopCoroutine(_timeRoutine); _timeRoutine = null; }

            CancelCharge();
            if (animator != null)
            {
                animator.SetBool(HashIsFiring, false);
                animator.SetFloat(HashShootSpeed, 1f);
            }
            if (_isADSActive) ExitADS();

            if (_currentVisual != null) Destroy(_currentVisual);
            Muzzle = GripR = GripL = null;
            CurrentAttach = null;
            _muzzleFlash = null;
            onWeaponExpired?.Raise();
            onDurabilityChanged?.Raise(0f);
        }

        public void CycleWeapon()
        {
            if (loadout == null || loadout.Length == 0) return;
            int next = (_currentIndex + 1) % loadout.Length;
            Equip(next);
        }

        public void Equip(int index)
        {
            if (loadout == null || index < 0 || index >= loadout.Length) return;
            var attach = loadout[index];
            if (attach == null) return;

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

                Muzzle = FindChild(_currentVisual.transform, attach.muzzleChildName);
                GripR = FindChild(_currentVisual.transform, attach.gripRChildName);
                GripL = FindChild(_currentVisual.transform, attach.gripLChildName);
                _muzzleFlash = _currentVisual.GetComponentInChildren<ParticleSystem>(true);

                // Slide the gun so the right grip sits exactly on the hand socket.
                // Keeps the orientation from equipLocalEuler; only corrects position.
                // The right hand then holds the gun by construction (no right-hand IK needed).
                if (attach.snapRightGripToHand && GripR != null)
                {
                    Vector3 gripInSocket = weaponSocket.InverseTransformPoint(GripR.position);
                    _currentVisual.transform.localPosition -= gripInSocket;
                }
                }

                if (animator != null)
                {
                    // Stance is driven by the weapon's handedness (OneHanded -> HandGun, TwoHanded -> AR).
                    // attach.stanceId is only a fallback when there is no gameplay data.
                    float stance = attach.stanceId;
                    if (attach.gameplayData != null)
                        stance = attach.gameplayData.handedness == VelocityWars.WeaponHandedness.OneHanded ? 1f : 0f;
                    animator.SetFloat(HashStance, stance);
                }

                _timeSinceLastShot = float.MaxValue; // allow immediate first shot
                if (attach.gameplayData != null)
                InitDurability(attach.gameplayData);
                }

        /// <summary>
        /// Executed when the unarmed strike event ('Hit') fires, dealing damage to targets in front.
        /// </summary>
        public void OnMeleeHit()
        {
            Vector3 origin = transform.position + Vector3.up * 1.0f;
            Vector3 direction = transform.forward;
            float meleeRange = 2.0f;
            float meleeRadius = 0.5f;

            if (Physics.SphereCast(origin, meleeRadius, direction, out RaycastHit hit, meleeRange, hitMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(25f, DamageType.Melee, null);
                }
            }
        }

        private void CancelCharge()
        {
            _isCharging = false;
            _chargeTimer = 0f;
            if (_activeChargeVFX != null)
            {
                Destroy(_activeChargeVFX);
                _activeChargeVFX = null;
            }
            if (animator != null)
            {
                animator.SetBool(HashIsFiring, false);
                animator.SetFloat(HashShootSpeed, 1f);
            }
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
