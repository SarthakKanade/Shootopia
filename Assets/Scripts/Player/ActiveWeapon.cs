using UnityEngine;
using StarterAssets;
using Unity.Cinemachine;
using TMPro;
using VelocityWars.Core;
using System.Collections;

namespace VelocityWars
{
    public class ActiveWeapon : MonoBehaviour 
{
        [SerializeField] WeaponSO startingWeapon;
        [SerializeField] CinemachineCamera playerFollowCamera;
        [SerializeField] Camera weaponCamera;
        [SerializeField] GameObject zoomVignette;
        [SerializeField] TMP_Text ammoText;
        [SerializeField] Transform muzzlePoint;
        public GameObject scopeOverlay;

        [Header("Durability Events")]
        [SerializeField] private GameEvent _onWeaponExpired;
        [SerializeField] private FloatGameEvent _onDurabilityChanged;
        [SerializeField] private FloatGameEvent _onMaxDurabilityChanged;

        WeaponSO currentWeaponSO;
        Animator animator;
        StarterAssetsInputs starterAssetsInputs;
        FirstPersonController firstPersonController;
        Weapon currentWeapon;
        public Weapon CurrentWeapon => currentWeapon;

        const string SHOOT_STRING = "Shoot";

        float timeSinceLastShot = 0f;
        float defaultFOV;
        float defaultRotationSpeed;
        int currentAmmo;

        private int _currentUses;
        private float _timeRemaining;
        private Coroutine _timeBasedRoutine;

        // New state fields
        private bool _isCharging;
        private float _chargeTimer;
        [SerializeField] private GameObject chargeVFXPrefab;
        private GameObject _activeChargeVFX;
        private bool _isADSActive;
        private float _fuelRemaining;
        private Coroutine _flameRoutine;
        private bool _lastShoot;
        private bool _lastZoom;
        private float _originalMoveSpeed;
        private float _originalSprintSpeed;

        [Header("Trap Trajectory")]
        [SerializeField] private LineRenderer _trajectoryLine;
        [SerializeField] private int _trajectoryPoints = 30;
        private bool _isPreparingTrap;

        public float FireRateMultiplier { get; set; } = 1f;

        public int CurrentUses
        {
            get => _currentUses;
            set
            {
                _currentUses = value;
                _onDurabilityChanged?.Raise((float)_currentUses);
                ammoText.text = _currentUses.ToString("D2");
            }
        }

        public int CurrentAmmo
        {
            get => currentAmmo;
            set
            {
                currentAmmo = value;
                _onDurabilityChanged?.Raise((float)currentAmmo);
                ammoText.text = currentAmmo.ToString("D2");
            }
        }

        private bool _isOverclocked;
        public bool IsOverclocked
        {
            get => _isOverclocked;
            set
            {
                _isOverclocked = value;
                UpdateAmmoDisplay();
            }
        }

        public void SetCurrentUses(int uses)
        {
            _currentUses = uses;
            _onDurabilityChanged?.Raise((float)_currentUses);
            UpdateAmmoDisplay();
        }

        void Awake()
        {
            starterAssetsInputs = GetComponentInParent<StarterAssetsInputs>();
            firstPersonController = GetComponentInParent<FirstPersonController>();
            animator = GetComponent<Animator>();

            if (playerFollowCamera != null)
            {
                defaultFOV = playerFollowCamera.Lens.FieldOfView;
            }

            if (firstPersonController != null)
            {
                defaultRotationSpeed = firstPersonController.RotationSpeed;
                _originalMoveSpeed = firstPersonController.MoveSpeed;
                _originalSprintSpeed = firstPersonController.SprintSpeed;
            }
        }

        void Start() 
        {
            SwitchWeapon(startingWeapon);
        }

        void Update()
        {
            HandleShoot();
            HandleADS();
            HandleFOVLerp();
            
            _lastShoot = starterAssetsInputs.shoot;
            _lastZoom = starterAssetsInputs.zoom;
        }

        public void AdjustAmmo(int amount) 
        {
            if (currentWeaponSO == null) return;

            currentAmmo += amount;

            if (currentAmmo > currentWeaponSO.MagazineSize) 
            {
                currentAmmo = currentWeaponSO.MagazineSize;
            }

            UpdateAmmoDisplay();
            _onDurabilityChanged?.Raise((float)currentAmmo);

            if (currentAmmo <= 0 && currentWeaponSO.durabilityType == DurabilityType.Ammo)
            {
                ExpireWeapon();
            }
        }

        private void UpdateAmmoDisplay()
        {
            if (ammoText == null) return;

            if (_isOverclocked)
            {
                ammoText.text = "∞";
                return;
            }

            if (currentWeaponSO == null)
            {
                ammoText.text = "--";
                return;
            }

            switch (currentWeaponSO.durabilityType)
            {
                case DurabilityType.Ammo:
                    ammoText.text = currentAmmo.ToString("D2");
                    break;
                case DurabilityType.Uses:
                    ammoText.text = _currentUses.ToString("D2");
                    break;
                case DurabilityType.TimeBased:
                    ammoText.text = Mathf.CeilToInt(_timeRemaining).ToString() + "s";
                    break;
            }

            if (currentWeaponSO.isContinuous)
            {
                ammoText.text = Mathf.CeilToInt(_fuelRemaining).ToString() + "s";
            }
        }

        public void SwitchWeapon(WeaponSO weaponSO) 
        {
            if (currentWeapon) 
            {
                Destroy(currentWeapon.gameObject);
            }

            if (_timeBasedRoutine != null)
            {
                StopCoroutine(_timeBasedRoutine);
                _timeBasedRoutine = null;
            }
            
            if (_flameRoutine != null)
            {
                StopCoroutine(_flameRoutine);
                _flameRoutine = null;
            }

            if (weaponSO == null)
            {
                currentWeaponSO = null;
                currentWeapon = null;
                UpdateAmmoDisplay();
                return;
            }

            if (weaponSO.weaponPrefab == null)
            {
                Debug.LogWarning($"WeaponSO '{weaponSO.weaponName}' has no weaponPrefab assigned!");
                currentWeapon = null;
                this.currentWeaponSO = weaponSO;
            }
            else
            {
                Weapon newWeapon = Instantiate(weaponSO.weaponPrefab, transform).GetComponent<Weapon>();
                currentWeapon = newWeapon;
                this.currentWeaponSO = weaponSO;
            }

            float maxDurability = 1f;
            switch (weaponSO.durabilityType)
            {
                case DurabilityType.Ammo:
                    maxDurability = weaponSO.MagazineSize;
                    currentAmmo = 0;
                    AdjustAmmo(weaponSO.MagazineSize);
                    break;
                case DurabilityType.Uses:
                    maxDurability = weaponSO.maxUses;
                    _currentUses = weaponSO.maxUses;
                    _onDurabilityChanged?.Raise((float)_currentUses);
                    break;
                case DurabilityType.TimeBased:
                    maxDurability = weaponSO.weaponDuration;
                    _timeRemaining = weaponSO.weaponDuration;
                    _timeBasedRoutine = StartCoroutine(TimeBasedRoutine());
                    break;
            }

            if (weaponSO.isContinuous)
            {
                _fuelRemaining = weaponSO.fuelDuration;
                maxDurability = weaponSO.fuelDuration;
                if (currentWeapon is FlameGunWeapon flameGun)
                {
                    flameGun.Initialize(weaponSO, muzzlePoint, currentWeapon.interactionLayers);
                }
            }
            
            UpdateAmmoDisplay();
            _onMaxDurabilityChanged?.Raise(maxDurability);
        }

        void HandleShoot()
        {
            if (currentWeaponSO == null || currentWeapon == null) return;

            timeSinceLastShot += Time.deltaTime;

            bool shootHeld = starterAssetsInputs.shoot;
            bool shootPressed = shootHeld && !_lastShoot;
            bool shootReleased = !shootHeld && _lastShoot;

            // Charged weapons (Hold style)
            if (currentWeaponSO.isCharged && currentWeaponSO.isAutoFire)
            {
                if (shootHeld)
                {
                    if (!_isCharging)
                    {
                        _isCharging = true;
                        _chargeTimer = 0;
                        if (chargeVFXPrefab != null && muzzlePoint != null)
                        {
                            _activeChargeVFX = Instantiate(chargeVFXPrefab, muzzlePoint);
                            _activeChargeVFX.transform.localPosition = Vector3.zero;
                            _activeChargeVFX.transform.localRotation = Quaternion.identity;
                            _activeChargeVFX.transform.localScale = Vector3.zero;
                        }
                    }

                    float previousTimer = _chargeTimer;
                    _chargeTimer += Time.deltaTime;
                    float chargeRatio = Mathf.Clamp01(_chargeTimer / currentWeaponSO.chargeTime);
                    
                    if (_activeChargeVFX != null)
                    {
                        _activeChargeVFX.transform.localScale = Vector3.one * chargeRatio;
                    }

                    // Pulse at 100%
                    if (_chargeTimer >= currentWeaponSO.chargeTime && previousTimer < currentWeaponSO.chargeTime)
                    {
                        // Scale pulse or other feedback could go here
                        if (_activeChargeVFX != null)
                        {
                            _activeChargeVFX.transform.localScale = Vector3.one * 1.2f;
                        }
                    }
                }
                
                if (shootReleased)
                {
                    if (_isCharging)
                    {
                        if (_chargeTimer >= currentWeaponSO.chargeTime)
                        {
                            FireProjectile();
                        }
                        
                        if (_activeChargeVFX != null) Destroy(_activeChargeVFX);
                        _chargeTimer = 0;
                        _isCharging = false;
                    }
                }
            }
            // Charged weapons (Press style - used for Area Blaster)
            else if (currentWeaponSO.isCharged && !currentWeaponSO.isAutoFire)
            {
                if (shootPressed && CanFire() && !_isCharging)
                {
                    if (timeSinceLastShot >= currentWeaponSO.FireRate / FireRateMultiplier)
                    {
                        StartCoroutine(ChargeAndFireRoutine());
                    }
                }
            }
            // Auto fire
            else if (currentWeaponSO.isAutoFire)
            {
                if (shootHeld && CanFire())
                {
                    if (timeSinceLastShot >= currentWeaponSO.FireRate / FireRateMultiplier)
                    {
                        FireProjectile();
                    }
                }
            }
            // Trap throw logic: Hold to preview, Release to throw, 'T' to cancel
            else if (currentWeaponSO.isTrap)
            {
                if (shootPressed && CanFire())
                {
                    _isPreparingTrap = true;
                    if (_trajectoryLine != null) _trajectoryLine.enabled = true;
                }

                if (_isPreparingTrap)
                {
                    UpdateTrajectory(currentWeaponSO);

                    if (Input.GetKeyDown(KeyCode.T))
                    {
                        _isPreparingTrap = false;
                        if (_trajectoryLine != null) _trajectoryLine.enabled = false;
                    }
                    else if (shootReleased)
                    {
                        ThrowTrap();
                        _isPreparingTrap = false;
                        if (_trajectoryLine != null) _trajectoryLine.enabled = false;
                    }
                }
            }
            // Sniper/Precision: Hold to Scope, Release to Fire
            else if (currentWeaponSO.requiresADSToFire)
            {
                if (shootReleased && CanFire())
                {
                    if (timeSinceLastShot >= currentWeaponSO.FireRate / FireRateMultiplier)
                    {
                        FireProjectile();
                    }
                }
            }
            // Continuous stream
            else if (currentWeaponSO.isContinuous)
            {
                if (shootHeld && CanFire())
                {
                    if (_flameRoutine == null)
                    {
                        _flameRoutine = StartCoroutine(FlameRoutine());
                        if (currentWeapon is FlameGunWeapon flameGun) flameGun.SetFiring(true);
                    }
                }
                else if (_flameRoutine != null)
                {
                    StopCoroutine(_flameRoutine);
                    _flameRoutine = null;
                    if (currentWeapon is FlameGunWeapon flameGun) flameGun.SetFiring(false);
                }
            }
            // Standard semi-auto
            else
            {
                if (shootPressed && CanFire())
                {
                    if (timeSinceLastShot >= currentWeaponSO.FireRate / FireRateMultiplier)
                    {
                        FireProjectile();
                    }
                }
            }
        }

        private bool CanFire()
        {
            if (_isOverclocked) return true;

            switch (currentWeaponSO.durabilityType)
            {
                case DurabilityType.Ammo:
                    return currentAmmo > 0;
                case DurabilityType.Uses:
                    return _currentUses > 0;
                case DurabilityType.TimeBased:
                    return _timeRemaining > 0;
                default:
                    return true;
            }
        }

        private void FireProjectile()
        {
            if (currentWeaponSO.projectilePrefab != null)
            {
                // Centered Aiming: Raycast from camera center to find target, then aim from muzzle at that target
                Ray cameraRay = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                Vector3 targetPoint;
                LayerMask mask = currentWeapon != null ? currentWeapon.interactionLayers : ~0;

                if (Physics.Raycast(cameraRay, out RaycastHit hit, 1000f, mask, QueryTriggerInteraction.Ignore))
                {
                    targetPoint = hit.point;
                }
                else
                {
                    targetPoint = cameraRay.GetPoint(100f);
                }

                Vector3 fireDir = (targetPoint - muzzlePoint.position).normalized;
                Quaternion rotation = Quaternion.LookRotation(fireDir);

                var proj = Instantiate(currentWeaponSO.projectilePrefab, muzzlePoint.position, rotation);
                proj.GetComponent<BaseProjectile>()?.Init(currentWeaponSO, gameObject);
            }
            else
            {
                currentWeapon.Shoot(currentWeaponSO);
            }

            animator.Play(SHOOT_STRING, 0, 0f);
            timeSinceLastShot = 0f;
            DecrementDurability();
        }

        private IEnumerator ChargeAndFireRoutine()
        {
            _isCharging = true;
            float localChargeTimer = 0;

            GameObject visualProj = null;
            if (currentWeaponSO.projectilePrefab != null)
            {
                visualProj = Instantiate(currentWeaponSO.projectilePrefab, muzzlePoint);
                visualProj.transform.localPosition = Vector3.zero;
                visualProj.transform.localRotation = Quaternion.identity;

                // Disable logic components to make it a visual-only "charge" orb
                if (visualProj.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;
                if (visualProj.TryGetComponent<Collider>(out var col)) col.enabled = false;
                if (visualProj.TryGetComponent<BaseProjectile>(out var bp)) bp.enabled = false;

                Vector3 targetScale = visualProj.transform.localScale;
                visualProj.transform.localScale = Vector3.zero;

                while (localChargeTimer < currentWeaponSO.chargeTime)
                {
                    localChargeTimer += Time.deltaTime;
                    float t = localChargeTimer / currentWeaponSO.chargeTime;
                    visualProj.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
                    yield return null;
                }
            }

            if (visualProj != null) Destroy(visualProj);

            FireProjectile();
            _isCharging = false;
        }

        private void ThrowTrap()
        {
            if (currentWeaponSO.projectilePrefab == null) return;

            var trap = Instantiate(currentWeaponSO.projectilePrefab, muzzlePoint.position, Quaternion.identity);
            trap.GetComponent<TrapBase>()?.Initialise(GetOwnerID(), Camera.main, currentWeaponSO);
            
            animator.Play(SHOOT_STRING, 0, 0f);
            timeSinceLastShot = 0f;
            DecrementDurability();
        }

        private void UpdateTrajectory(WeaponSO trap)
        {
            if (_trajectoryLine == null) return;

            Vector3 startPos = muzzlePoint.position;
            Vector3 throwDir = Quaternion.AngleAxis(-trap.throwAngle, Camera.main.transform.right) * Camera.main.transform.forward;
            Vector3 initialVelocity = throwDir * trap.throwForce;

            _trajectoryLine.positionCount = _trajectoryPoints;
            float timeStep = 0.1f;
            for (int i = 0; i < _trajectoryPoints; i++)
            {
                float t = i * timeStep;
                Vector3 pos = startPos + initialVelocity * t + 0.5f * Physics.gravity * t * t;
                _trajectoryLine.SetPosition(i, pos);
            }
        }

        private int GetOwnerID()
        {
            return gameObject.GetInstanceID();
        }

        private IEnumerator FlameRoutine()
        {
            while (_fuelRemaining > 0 && starterAssetsInputs.shoot)
            {
                _fuelRemaining -= Time.deltaTime;
                float progress = (_fuelRemaining / currentWeaponSO.fuelDuration) * 100f;
                _onDurabilityChanged?.Raise(progress);
                UpdateAmmoDisplay();
                yield return null;
            }

            if (_fuelRemaining <= 0)
            {
                if (currentWeapon is FlameGunWeapon flameGun) flameGun.SetFiring(false);
                ExpireWeapon();
            }
        }

        private void DecrementDurability()
        {
            if (currentWeaponSO == null || _isOverclocked) return;

            switch (currentWeaponSO.durabilityType)
            {
                case DurabilityType.Ammo:
                    AdjustAmmo(-1);
                    break;
                case DurabilityType.Uses:
                    _currentUses--;
                    _onDurabilityChanged?.Raise((float)_currentUses);
                    UpdateAmmoDisplay();
                    if (_currentUses <= 0)
                    {
                        ExpireWeapon();
                    }
                    break;
                case DurabilityType.TimeBased:
                    break;
            }
        }

        private IEnumerator TimeBasedRoutine()
        {
            while (_timeRemaining > 0)
            {
                if (!_isOverclocked)
                {
                    _timeRemaining -= Time.deltaTime;
                    _onDurabilityChanged?.Raise(_timeRemaining);
                    UpdateAmmoDisplay();
                }
                yield return null;
            }
            ExpireWeapon();
        }

        private void ExpireWeapon()
        {
            if (_isADSActive) ExitADS();

            if (currentWeapon != null)
            {
                Destroy(currentWeapon.gameObject);
                currentWeapon = null;
            }
            
            currentWeaponSO = null;
            _onWeaponExpired?.Raise();
            _onDurabilityChanged?.Raise(0);
            ammoText.text = "--";

            if (_timeBasedRoutine != null)
            {
                StopCoroutine(_timeBasedRoutine);
                _timeBasedRoutine = null;
            }

            if (_flameRoutine != null)
            {
                StopCoroutine(_flameRoutine);
                _flameRoutine = null;
            }
        }

        void HandleADS()
        {
            if (currentWeaponSO == null) return;

            if (currentWeaponSO.hasADS)
            {
                // Auto-scope on left click if required to fire
                bool autoScopeInput = currentWeaponSO.requiresADSToFire && starterAssetsInputs.shoot;
                bool zoomInput = starterAssetsInputs.zoom || autoScopeInput;

                if (zoomInput && !_isADSActive)
                {
                    EnterADS();
                }
                else if (!zoomInput && _isADSActive)
                {
                    ExitADS();
                }
            }
            else if (currentWeaponSO.CanZoom)
            {
                // Legacy zoom behavior
                if (starterAssetsInputs.zoom) 
                {
                    playerFollowCamera.Lens.FieldOfView = currentWeaponSO.ZoomAmount;
                    weaponCamera.fieldOfView = currentWeaponSO.ZoomAmount;
                    zoomVignette.SetActive(true);
                    firstPersonController.ChangeRotationSpeed(currentWeaponSO.ZoomRotationSpeed);
                }
                else 
                {
                    playerFollowCamera.Lens.FieldOfView = defaultFOV;
                    weaponCamera.fieldOfView = defaultFOV;
                    zoomVignette.SetActive(false);
                    firstPersonController.ChangeRotationSpeed(defaultRotationSpeed);
                }
            }
        }


        void HandleFOVLerp()
        {
            if (currentWeaponSO == null || playerFollowCamera == null) return;

            float targetFOV = _isADSActive ? currentWeaponSO.adsFOV : defaultFOV;
            if (Mathf.Abs(playerFollowCamera.Lens.FieldOfView - targetFOV) > 0.1f)
            {
                float newFOV = Mathf.Lerp(playerFollowCamera.Lens.FieldOfView, targetFOV, Time.deltaTime * 10f);
                playerFollowCamera.Lens.FieldOfView = newFOV;
                if (weaponCamera != null) weaponCamera.fieldOfView = newFOV;
            }
        }

        void EnterADS()
        {
            _isADSActive = true;
            
            if (firstPersonController != null)
            {
                firstPersonController.MoveSpeed = _originalMoveSpeed * currentWeaponSO.adsMoveSpeedMult;
                firstPersonController.SprintSpeed = _originalSprintSpeed * currentWeaponSO.adsMoveSpeedMult;
            }
            
            if (zoomVignette != null) zoomVignette.SetActive(true);
            if (scopeOverlay != null) scopeOverlay.SetActive(true);
        }

        void ExitADS()
        {
            _isADSActive = false;
            
            if (firstPersonController != null)
            {
                firstPersonController.MoveSpeed = _originalMoveSpeed;
                firstPersonController.SprintSpeed = _originalSprintSpeed;
            }
            
            if (zoomVignette != null) zoomVignette.SetActive(false);
            if (scopeOverlay != null) scopeOverlay.SetActive(false);
        }
    }
}
