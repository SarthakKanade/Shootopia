using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

namespace Shootopia
{
    /// <summary>
    /// Manages Aim-Down-Sights (ADS) camera FOV lerp, third-person shoulder offsets,
    /// scope HUD overlay, and local player mesh visibility during zoom.
    /// </summary>
    public class WeaponADSHandler
    {
        private readonly Transform _ownerTransform;
        private readonly CharacterMotor _motor;
        private CinemachineCamera _camera;
        private CinemachineThirdPersonFollow _thirdPersonFollow;
        private GameObject _scopeOverlayInstance;

        private float _defaultFOV = 68f;
        private float _originalMoveSpeed = 5f;
        private float _defaultCameraDistance = 3.2f;
        private Vector3 _defaultShoulderOffset = new Vector3(0.48f, 1.7f, 0f);
        private float _defaultVerticalArmLength = 0f;
        private readonly List<Renderer> _hiddenADSRenderers = new List<Renderer>();

        public bool IsADSActive { get; private set; }

        public WeaponADSHandler(Transform ownerTransform, CharacterMotor motor, CinemachineCamera camera, GameObject scopeOverlayPrefab)
        {
            _ownerTransform = ownerTransform;
            _motor = motor;
            if (_motor != null) _originalMoveSpeed = _motor.moveSpeed;

            SetCamera(camera);

            if (scopeOverlayPrefab != null)
            {
                _scopeOverlayInstance = Object.Instantiate(scopeOverlayPrefab);
                _scopeOverlayInstance.SetActive(false);
            }
        }

        public void SetCamera(CinemachineCamera camera)
        {
            _camera = camera;
            if (_camera == null) return;

            _defaultFOV = _camera.Lens.FieldOfView;
            _thirdPersonFollow = _camera.GetComponent<CinemachineThirdPersonFollow>();
            if (_thirdPersonFollow != null)
            {
                _defaultCameraDistance = _thirdPersonFollow.CameraDistance;
                _defaultShoulderOffset = _thirdPersonFollow.ShoulderOffset;
                _defaultVerticalArmLength = _thirdPersonFollow.VerticalArmLength;
            }
        }

        /// <summary>
        /// Pulls the current active camera values from <see cref="ThirdPersonCameraController"/>
        /// so that ADS exit returns to the correct walking/combat view rather than
        /// a stale snapshot from initial camera setup.
        /// </summary>
        private void RefreshDefaults()
        {
            var tpc = ThirdPersonCameraController.Instance;
            if (tpc != null)
            {
                _defaultFOV = tpc.ActiveFOV;
                _defaultCameraDistance = tpc.ActiveDistance;
                _defaultShoulderOffset = tpc.ActiveShoulderOffset;
                _defaultVerticalArmLength = tpc.ActiveVerticalArm;
            }
        }

        public void UpdateADS(bool isChargingOrAiming, bool altFireHeld, WeaponAttachSO currentAttach)
        {
            if (currentAttach == null)
            {
                if (IsADSActive) ExitADS(instant: true);
                // Non-ADS camera is managed by ThirdPersonCameraController
                return;
            }

            bool isSniper = currentAttach != null && (currentAttach.name.Contains("Sniper") || (currentAttach.gameplayData != null && currentAttach.gameplayData.hasADS));
            if (!isSniper)
            {
                if (IsADSActive) ExitADS();
                // Non-ADS camera is managed by ThirdPersonCameraController
                return;
            }

            bool shouldScope = isChargingOrAiming || altFireHeld;
            if (shouldScope && !IsADSActive)
            {
                RefreshDefaults();
                EnterADS(currentAttach);
            }
            else if (!shouldScope && IsADSActive)
            {
                ExitADS();
            }

            // Only lerp camera when ADS is active; otherwise ThirdPersonCameraController owns it
            if (IsADSActive)
            {
                HandleFOVLerp(currentAttach);
            }
        }

        /// <summary>
        /// Lerps camera FOV / distance / offset toward ADS targets.
        /// Only called when <see cref="IsADSActive"/> is true.
        /// Non-ADS camera management is handled by <see cref="ThirdPersonCameraController"/>.
        /// </summary>
        public void HandleFOVLerp(WeaponAttachSO currentAttach)
        {
            if (_camera == null) return;

            bool isSniper = currentAttach != null && (currentAttach.name.Contains("Sniper") || (currentAttach.gameplayData != null && currentAttach.gameplayData.hasADS));

            float targetFOV = _defaultFOV;
            if (IsADSActive && isSniper)
            {
                if (currentAttach.gameplayData != null && currentAttach.gameplayData.adsFOV > 0)
                {
                    targetFOV = currentAttach.gameplayData.adsFOV;
                }
                else
                {
                    targetFOV = 20f;
                }
            }

            if (Mathf.Abs(_camera.Lens.FieldOfView - targetFOV) > 0.05f)
            {
                _camera.Lens.FieldOfView = Mathf.Lerp(_camera.Lens.FieldOfView, targetFOV, Time.deltaTime * 12f);
            }
            else
            {
                _camera.Lens.FieldOfView = targetFOV;
            }

            if (_thirdPersonFollow != null)
            {
                float targetDist = IsADSActive && isSniper ? 0.02f : _defaultCameraDistance;
                Vector3 targetOffset = IsADSActive && isSniper ? Vector3.zero : _defaultShoulderOffset;
                float targetArm = IsADSActive && isSniper ? 0f : _defaultVerticalArmLength;

                _thirdPersonFollow.CameraDistance = Mathf.Lerp(_thirdPersonFollow.CameraDistance, targetDist, Time.deltaTime * 12f);
                _thirdPersonFollow.ShoulderOffset = Vector3.Lerp(_thirdPersonFollow.ShoulderOffset, targetOffset, Time.deltaTime * 12f);
                _thirdPersonFollow.VerticalArmLength = Mathf.Lerp(_thirdPersonFollow.VerticalArmLength, targetArm, Time.deltaTime * 12f);
            }
        }

        public void EnterADS(WeaponAttachSO currentAttach)
        {
            bool isSniper = currentAttach != null && (currentAttach.name.Contains("Sniper") || (currentAttach.gameplayData != null && currentAttach.gameplayData.hasADS));
            if (!isSniper) return;

            IsADSActive = true;

            if (_scopeOverlayInstance != null)
            {
                _scopeOverlayInstance.SetActive(true);
            }

            if (_motor != null)
            {
                float speedMult = (currentAttach != null && currentAttach.gameplayData != null && currentAttach.gameplayData.adsMoveSpeedMult > 0)
                    ? currentAttach.gameplayData.adsMoveSpeedMult
                    : 0.8f;
                _motor.moveSpeed = _originalMoveSpeed * speedMult;
            }

            // Hide character meshes only for sniper so they never obstruct scope reticle
            if (isSniper && _ownerTransform != null)
            {
                _hiddenADSRenderers.Clear();
                var renderers = _ownerTransform.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    if (r != null && r.enabled && !(r is ParticleSystemRenderer))
                    {
                        r.enabled = false;
                        _hiddenADSRenderers.Add(r);
                    }
                }
            }
        }

        public void ExitADS(bool instant = false)
        {
            IsADSActive = false;
            if (_scopeOverlayInstance != null) _scopeOverlayInstance.SetActive(false);

            if (_motor != null)
            {
                _motor.moveSpeed = _originalMoveSpeed;
            }

            foreach (var r in _hiddenADSRenderers)
            {
                if (r != null) r.enabled = true;
            }
            _hiddenADSRenderers.Clear();

            // Refresh defaults so we return to the correct walking/combat view
            RefreshDefaults();

            if (instant)
            {
                if (_camera != null) _camera.Lens.FieldOfView = _defaultFOV;
                if (_thirdPersonFollow != null)
                {
                    _thirdPersonFollow.CameraDistance = _defaultCameraDistance;
                    _thirdPersonFollow.ShoulderOffset = _defaultShoulderOffset;
                    _thirdPersonFollow.VerticalArmLength = _defaultVerticalArmLength;
                }
            }
        }

        public void Cleanup()
        {
            if (IsADSActive) ExitADS();
            if (_scopeOverlayInstance != null)
            {
                Object.Destroy(_scopeOverlayInstance);
                _scopeOverlayInstance = null;
            }
        }
    }
}
