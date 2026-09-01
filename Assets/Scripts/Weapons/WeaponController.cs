using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using Unity.Netcode;

namespace Shootopia
{
    /// <summary>
    /// Master third-person weapon controller.
    /// Manages weapon loadout equipping, firing dispatch, durability tracking,
    /// melee combat, and multiplayer synchronization over Unity Netcode for GameObjects (NGO).
    /// </summary>
    public class WeaponController : NetworkBehaviour
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

        // Modular Sub-Handlers
        private WeaponADSHandler _adsHandler;
        private WeaponChargeHandler _chargeHandler;
        private WeaponAimHandler _aimHandler;
        private ThrowableTrajectoryPredictor _trajectoryPredictor;

        // Animator Hashes
        private static readonly int HashStance = Animator.StringToHash("Stance");
        private static readonly int HashShootSpeed = Animator.StringToHash("ShootSpeed");

        // Network State
        private readonly NetworkVariable<Unity.Collections.FixedString64Bytes> _equippedAttachName = 
            new NetworkVariable<Unity.Collections.FixedString64Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private static Dictionary<string, WeaponAttachSO> _attachCache;

        // Runtime State
        private int _currentIndex = -1;
        private GameObject _currentVisual;
        private ParticleSystem _muzzleFlash;
        private float _timeSinceLastShot;
        private bool _lastFire;
        private int _currentAmmo;
        private int _currentUses;
        private float _timeRemaining;
        private Coroutine _timeRoutine;
        private bool _isAimingTrap;
        private bool _inTrapCooldown;
        private Coroutine _trapCooldownRoutine;

        // Public Properties
        public WeaponAttachSO CurrentAttach { get; private set; }
        public Transform Muzzle { get; private set; }
        public Transform GripR { get; private set; }
        public Transform GripL { get; private set; }
        public bool IsArmed => CurrentAttach != null && _currentVisual != null;
        public int CurrentAmmo => _currentAmmo;
        public int CurrentUses => _currentUses;
        public float TimeRemaining => _timeRemaining;
        public float TimeSinceLastShot => _timeSinceLastShot;

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

        private IAimProvider Aim => motor != null ? motor.Aim : null;

        public static WeaponAttachSO GetAttachByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            if (_attachCache == null)
            {
                _attachCache = new Dictionary<string, WeaponAttachSO>();
                var allAttaches = Resources.LoadAll<WeaponAttachSO>("");
                foreach (var a in allAttaches)
                {
                    if (a != null && !string.IsNullOrEmpty(a.name) && !_attachCache.ContainsKey(a.name))
                        _attachCache[a.name] = a;
                }
#if UNITY_EDITOR
                var guids = UnityEditor.AssetDatabase.FindAssets("t:Shootopia.WeaponAttachSO");
                foreach (var g in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                    var a = UnityEditor.AssetDatabase.LoadAssetAtPath<Shootopia.WeaponAttachSO>(path);
                    if (a != null && !string.IsNullOrEmpty(a.name) && !_attachCache.ContainsKey(a.name))
                        _attachCache[a.name] = a;
                }
#endif
            }

            _attachCache.TryGetValue(name, out var result);
            return result;
        }

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            if (motor == null) motor = GetComponent<CharacterMotor>();
            if (input == null) input = GetComponent<PlayerInputRouter>();

            var aimRig = GetComponent<PlayerAimRig>();
            _trajectoryPredictor = GetComponent<ThrowableTrajectoryPredictor>();
            if (_trajectoryPredictor == null) _trajectoryPredictor = gameObject.AddComponent<ThrowableTrajectoryPredictor>();

            _adsHandler = new WeaponADSHandler(transform, motor, playerFollowCamera, scopeOverlay);
            _chargeHandler = new WeaponChargeHandler(vfxImplosionPrefab);
            _aimHandler = new WeaponAimHandler(transform, animator, motor, aimRig);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _equippedAttachName.OnValueChanged += OnEquippedAttachNameChanged;

            if (IsOwner)
            {
                if (playerFollowCamera == null)
                    playerFollowCamera = FindAnyObjectByType<CinemachineCamera>();

                _adsHandler.SetCamera(playerFollowCamera);

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
            _adsHandler.ExitADS();
            _chargeHandler.CancelCharge();
            _aimHandler.DisableBouncePreview();

            base.OnNetworkDespawn();
            _equippedAttachName.OnValueChanged -= OnEquippedAttachNameChanged;
        }

        private void OnDisable()
        {
            _adsHandler?.ExitADS();
            _chargeHandler?.CancelCharge();
            _aimHandler?.DisableBouncePreview();
        }

        private void OnEquippedAttachNameChanged(Unity.Collections.FixedString64Bytes previousValue, Unity.Collections.FixedString64Bytes newValue)
        {
            string newName = newValue.ToString();
            if (!string.IsNullOrEmpty(newName))
            {
                EquipAttachByNameLocal(newName, forceFresh: false);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _timeSinceLastShot += dt;

            // Update charge visuals for remote clients
            if (!IsOwner)
            {
                if (_chargeHandler.IsCharging && CurrentAttach != null && CurrentAttach.gameplayData != null)
                {
                    _chargeHandler.UpdateCharge(dt, CurrentAttach.gameplayData);
                }
                return;
            }

            if (input == null) return;

            // Gating input when menu is open or pre-match countdown is running
            bool isFrozen = WaitingRoomController.IsLobbyMenuOpen || PauseMenuController.IsMenuPaused;
            if (GameModeBase.Instance != null && GameModeBase.Instance.PreMatchCountdown.Value > 0)
            {
                isFrozen = true;
            }

            if (isFrozen)
            {
                _adsHandler.ExitADS();
                _aimHandler.ExitCombatStance();
                _lastFire = false;
                return;
            }

            if (input.Intent.SwapWeapon)
            {
                CycleWeapon();
            }

            HandleFire(dt);
            _adsHandler.UpdateADS(_chargeHandler.IsCharging || _isAimingTrap, input.Intent.AltFire, CurrentAttach);
            _aimHandler.UpdateAimState(dt, input.Intent.Fire, CurrentAttach, _chargeHandler.IsCharging, _isAimingTrap);
            _aimHandler.UpdateBouncePreview(IsOwner, CurrentAttach, Muzzle, Aim);
        }

        #region Firing Logic

        private void HandleFire(float dt)
        {
            bool fireHeld = input.Intent.Fire;
            bool firePressed = fireHeld && !_lastFire;
            bool fireReleased = !fireHeld && _lastFire;
            _lastFire = fireHeld;

            // 1. Unarmed Punch
            if (!IsArmed)
            {
                if (firePressed && _timeSinceLastShot >= 0.4f)
                {
                    _timeSinceLastShot = 0f;
                    if (animator != null)
                    {
                        animator.SetFloat(HashStance, 2.0f);
                        animator.Play("Shoot", 1, 0f);
                        StartCoroutine(TriggerMeleeAnimation(0.60f));
                    }
                    StartCoroutine(DelayedMeleeHitRoutine(0.45f, 25f, null));
                }
                return;
            }

            var data = CurrentAttach.gameplayData;
            if (data == null) return;

            // 2. Knife / Melee
            bool isKnifeOrMelee = CurrentAttach.stanceId == 3 || data.category == WeaponCategory.Melee || data.name.Contains("Knife");
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

            // 3. Traps / Throwables
            if (data.isTrap || data.category == WeaponCategory.Trap)
            {
                HandleTrapFire(data, fireHeld, fireReleased);
                return;
            }

            // 4. Charged Weapons (e.g. Plasma / Void / Sniper)
            if (data.isCharged)
            {
                HandleChargedFire(data, dt, fireHeld, fireReleased);
                return;
            }

            // 5. Continuous Stream (Flame)
            if (CurrentAttach.name.Contains("Flame"))
            {
                HandleFlameFire(data, fireHeld, firePressed);
                return;
            }

            // 6. Standard Ranged (Automatic or Semi-Automatic)
            bool isAuto = data.isAutomatic || data.isAutoFire;
            bool wantFire = isAuto ? fireHeld : firePressed;

            if (wantFire && CanFire(data) && _timeSinceLastShot >= data.FireRate)
            {
                _aimHandler.TriggerCombatStance();
                FireProjectile(data);
                if (animator != null) animator.Play("Shoot", 1, 0f);
            }

            if (isAuto && !fireHeld && !_aimHandler.IsInCombatStance && animator != null)
            {
                animator.SetBool(Animator.StringToHash("IsFiring"), false);
            }
        }

        private void HandleTrapFire(WeaponSO data, bool fireHeld, bool fireReleased)
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
                        animator.SetFloat(HashShootSpeed, 1f);
                        animator.CrossFade("ThrowBall_Prep", 0.05f, -1, 0f);
                    }
                    if (motor != null) motor.MoveSpeedMultiplier = 0.25f;
                }

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
                    animator.SetFloat(HashShootSpeed, 1f);
                    animator.CrossFade("ThrowBall_Release", 0.02f, -1, 0f);
                }
                if (motor != null) motor.MoveSpeedMultiplier = 1.0f;
                if (_trajectoryPredictor != null) _trajectoryPredictor.HideTrajectory();

                if (_currentVisual != null) _currentVisual.SetActive(false);

                GetTrapLaunchParameters(data, out Vector3 trapOrigin, out Vector3 trapVel);
                FireProjectile(data, trapOrigin, trapVel);

                StartCoroutine(ResetAnimationToIdleRoutine());

                if (_trapCooldownRoutine != null) StopCoroutine(_trapCooldownRoutine);
                _trapCooldownRoutine = StartCoroutine(TrapCooldownRoutine());
            }
            else if (!fireHeld && _isAimingTrap)
            {
                CancelTrapAim();
            }
        }

        private void HandleChargedFire(WeaponSO data, float dt, bool fireHeld, bool fireReleased)
        {
            if (fireHeld && CanFire(data) && _timeSinceLastShot >= data.FireRate)
            {
                if (!_chargeHandler.IsCharging)
                {
                    StartCharge(data);
                }
                else
                {
                    _chargeHandler.UpdateCharge(dt, data);
                }
            }
            else if (fireReleased && _chargeHandler.IsCharging)
            {
                if (_chargeHandler.IsFullyCharged(data))
                {
                    _aimHandler.TriggerCombatStance();
                    FireProjectile(data);
                    if (animator != null)
                    {
                        animator.Play("Shoot", 1, 0f);
                        animator.SetFloat(HashShootSpeed, 1f);
                    }
                }
                CancelCharge();
            }
            else if (!fireHeld && _chargeHandler.IsCharging)
            {
                CancelCharge();
            }
        }

        private void HandleFlameFire(WeaponSO data, bool fireHeld, bool firePressed)
        {
            if (fireHeld && CanFire(data))
            {
                bool isStartingFire = firePressed || (animator != null && !animator.GetCurrentAnimatorStateInfo(1).IsName("Shoot"));
                _aimHandler.TriggerCombatStance();

                if (animator != null)
                {
                    if (isStartingFire) animator.Play("Shoot", 1, 0f);
                    animator.SetFloat(HashShootSpeed, 0f);
                }

                if (_timeSinceLastShot >= data.FireRate)
                {
                    FireProjectile(data);
                }
            }
            else if (!_aimHandler.IsInCombatStance && animator != null)
            {
                animator.SetBool(Animator.StringToHash("IsFiring"), false);
                animator.SetFloat(HashShootSpeed, 1f);
            }
        }

        private bool CanFire(WeaponSO data)
        {
            if (data == null) return false;
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

            Vector3 origin = customOrigin ?? (Muzzle != null ? Muzzle.position : transform.position + Vector3.up * 1.2f);
            if (!customOrigin.HasValue)
            {
                float minFireHeight = transform.position.y + 0.90f;
                if (origin.y < minFireHeight) origin.y = minFireHeight;
            }

            Camera mainCam = Camera.main;
            Vector3 camForward = mainCam != null ? mainCam.transform.forward : (Aim != null ? Aim.CameraPlanarForward : transform.forward);
            Vector3 aimPoint = Aim != null ? Aim.AimPoint : origin + camForward * 100f;
            Vector3 dir = customVel.HasValue ? customVel.Value.normalized : (aimPoint - origin).normalized;

            if (!customVel.HasValue)
            {
                if (Vector3.Dot(dir, camForward) <= 0.15f || Vector3.Dot(aimPoint - origin, camForward) <= 0.5f)
                    dir = camForward;
            }

            if (dir.sqrMagnitude < 0.0001f) dir = Muzzle != null ? Muzzle.forward : transform.forward;
            Vector3 launchVel = customVel ?? (dir * (data.throwForce > 0f ? data.throwForce : 14f));
            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

            FireProjectileRpc(_currentIndex, origin, rot, launchVel);

            if (data.audio != null && data.audio.fireClip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFXAtPosition(data.audio.fireClip, origin);
            }

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
                if (tb != null) tb.Initialise((int)OwnerClientId, Camera.main, data, launchVel);
            }
            else
            {
                // Hitscan fallback
                if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, hitRange, hitMask, QueryTriggerInteraction.Ignore))
                {
                    GameObject hitVFX = data.HitVFXPrefab != null ? data.HitVFXPrefab : fallbackHitscanImpactVFX;
                    if (hitVFX != null) Instantiate(hitVFX, hit.point, Quaternion.LookRotation(hit.normal));
                    if (hitscanImpactSound != null) AudioSource.PlayClipAtPoint(hitscanImpactSound, hit.point, 0.6f);
                }
            }

            if (_muzzleFlash != null) _muzzleFlash.Play();
            DecrementDurability(data);
        }

        #endregion

        #region RPCs

        [Rpc(SendTo.ClientsAndHost)]
        private void FireProjectileRpc(int weaponIndex, Vector3 origin, Quaternion rotation, Vector3 velocity)
        {
            if (IsOwner) return;

            WeaponAttachSO attach = (loadout != null && weaponIndex >= 0 && weaponIndex < loadout.Length) ? loadout[weaponIndex] : CurrentAttach;
            if (attach == null || attach.gameplayData == null) return;
            var data = attach.gameplayData;

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
                if (Physics.Raycast(origin, rotation * Vector3.forward, out RaycastHit hit, hitRange, hitMask, QueryTriggerInteraction.Ignore))
                {
                    GameObject hitVFX = data.HitVFXPrefab != null ? data.HitVFXPrefab : fallbackHitscanImpactVFX;
                    if (hitVFX != null) Instantiate(hitVFX, hit.point, Quaternion.LookRotation(hit.normal));
                    if (hitscanImpactSound != null) AudioSource.PlayClipAtPoint(hitscanImpactSound, hit.point, 0.6f);
                }
            }

            if (_muzzleFlash != null) _muzzleFlash.Play();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void FireWeaponServerRpc(Vector3 firePoint, Vector3 aimDir, string weaponName)
        {
            if (!IsServer) return;

            var attach = CurrentAttach ?? GetAttachByName(weaponName);
            if (attach == null || attach.gameplayData == null || attach.gameplayData.projectilePrefab != null) return;
            var data = attach.gameplayData;

            if (Physics.Raycast(firePoint, aimDir, out RaycastHit hit, hitRange, hitMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
                {
                    var targetHealth = hit.collider.GetComponentInParent<PlayerHealth>() ?? hit.collider.GetComponent<PlayerHealth>();
                    bool wasAlive = targetHealth != null && targetHealth.GetCurrentHp() > 0f;

                    if (targetHealth != null) targetHealth.LastAttackerClientId = OwnerClientId;
                    damageable.TakeDamage(data.Damage, DamageType.Bullet, data);

                    if (wasAlive && targetHealth != null && targetHealth.GetCurrentHp() <= 0f)
                    {
                        NetworkedGameManager.ReportKill(OwnerClientId, targetHealth.OwnerClientId, weaponName);
                    }
                }
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

        [Rpc(SendTo.Everyone)]
        private void StartChargeRpc() => _chargeHandler.StartCharge(Muzzle, CurrentAttach != null ? CurrentAttach.gameplayData : null);

        [Rpc(SendTo.Everyone)]
        private void CancelChargeRpc() => _chargeHandler.CancelCharge();

        #endregion

        #region Durability & Weapon Swapping

        private void DecrementDurability(WeaponSO data)
        {
            if (data == null) return;
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
            }
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
            _aimHandler.ExitCombatStance();
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

        public void EquipByAttachSO(WeaponAttachSO attach)
        {
            if (attach != null) EquipNetworkedWeapon(attach.name);
        }

        public void ReplaceCurrentWeapon(WeaponAttachSO attach)
        {
            if (attach != null) EquipNetworkedWeapon(attach.name);
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
                ResetWeaponState();
                return;
            }

            var attach = GetAttachByName(attachName);
            if (attach == null) return;
            if (!forceFresh && CurrentAttach == attach && _currentVisual != null) return;

            ResetWeaponState();

            // Health Box consumption
            if (attach.gameplayData != null && (attach.gameplayData.name.Contains("Health") || attach.name.Contains("Health")))
            {
                var hp = GetComponent<PlayerHealth>() ?? GetComponentInParent<PlayerHealth>();
                if (hp != null) hp.Heal(50f);
                ExpireWeapon();
                return;
            }

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

            var data = attach.gameplayData;
            if (data != null)
            {
                InitDurability(data);
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

                foreach (var rb in _currentVisual.GetComponentsInChildren<Rigidbody>(true)) rb.isKinematic = true;
                foreach (var col in _currentVisual.GetComponentsInChildren<Collider>(true)) col.enabled = false;
                foreach (var tr in _currentVisual.GetComponentsInChildren<TrapBase>(true)) tr.enabled = false;

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
                    stance = 3f;
                else if (data != null)
                    stance = data.handedness == WeaponHandedness.OneHanded ? 1f : 0f;

                animator.SetFloat(HashStance, stance);
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

        private void ResetWeaponState()
        {
            CancelTrapAim();
            if (_trapCooldownRoutine != null) { StopCoroutine(_trapCooldownRoutine); _trapCooldownRoutine = null; }
            _inTrapCooldown = false;

            if (_timeRoutine != null) { StopCoroutine(_timeRoutine); _timeRoutine = null; }

            CancelCharge();
            _aimHandler.ExitCombatStance();
            if (_adsHandler.IsADSActive && IsOwner) _adsHandler.ExitADS();

            if (_currentVisual != null) Destroy(_currentVisual);
            Muzzle = GripR = GripL = null;
            CurrentAttach = null;
            _muzzleFlash = null;
            _currentIndex = -1;

            if (animator != null) animator.SetFloat(HashStance, 2f);
        }

        #endregion

        #region Charging & Trap Helpers

        private void StartCharge(WeaponSO data)
        {
            if (IsSpawned) StartChargeRpc();
            else _chargeHandler.StartCharge(Muzzle, data);
        }

        private void CancelCharge()
        {
            if (IsSpawned) CancelChargeRpc();
            else _chargeHandler.CancelCharge();
            _aimHandler.ExitCombatStance();
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
            yield return new WaitForSeconds(1.0f);
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

        #endregion

        #region Melee Combat

        public void PerformMeleeHit(float damage = 25f, WeaponSO weaponUsed = null)
        {
            if (!IsOwner) return;

            Vector3 origin = transform.position + Vector3.up * 1.2f;
            Vector3 aimDir = (Aim != null && Aim.AimPoint != Vector3.zero) ? (Aim.AimPoint - origin).normalized : transform.forward;
            if (aimDir.sqrMagnitude < 0.001f) aimDir = transform.forward;

            RaycastHit[] hits = Physics.SphereCastAll(origin, 0.8f, aimDir, 2.5f, hitMask, QueryTriggerInteraction.Ignore);

            foreach (var hit in hits)
            {
                if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform)) continue;

                var targetHealth = hit.collider.GetComponentInParent<PlayerHealth>() ?? hit.collider.GetComponent<PlayerHealth>();
                var damageable = hit.collider.GetComponent<IDamageable>() ?? (IDamageable)targetHealth ?? hit.collider.GetComponentInParent<IDamageable>();

                if (damageable != null)
                {
                    if (targetHealth != null) targetHealth.LastAttackerClientId = OwnerClientId;
                    damageable.TakeDamage(damage, DamageType.Melee, weaponUsed);
                    break;
                }
            }
        }

        public void OnMeleeHit()
        {
            float damage = (CurrentAttach != null && CurrentAttach.gameplayData != null && CurrentAttach.gameplayData.Damage > 0)
                ? CurrentAttach.gameplayData.Damage
                : 25f;
            PerformMeleeHit(damage, CurrentAttach != null ? CurrentAttach.gameplayData : null);
        }

        private IEnumerator DelayedMeleeHitRoutine(float delaySeconds, float damage, WeaponSO weaponUsed)
        {
            yield return new WaitForSeconds(delaySeconds);
            PerformMeleeHit(damage, weaponUsed);
        }

        private IEnumerator TriggerMeleeAnimation(float duration)
        {
            if (animator == null) yield break;
            animator.SetBool(Animator.StringToHash("IsFiring"), true);
            animator.SetFloat(HashShootSpeed, 1f);
            yield return new WaitForSeconds(duration);
            animator.SetBool(Animator.StringToHash("IsFiring"), false);
        }

        #endregion

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
