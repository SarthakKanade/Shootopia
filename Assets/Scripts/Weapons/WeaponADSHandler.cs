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

        private float _defaultFOV = 60f;
        private float _originalMoveSpeed = 5f;
        private float _defaultCameraDistance = 4.5f;
        private Vector3 _defaultShoulderOffset = new Vector3(0f, 1.5f, -1.5f);
        private float _defaultVerticalArmLength = 0.3f;
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

        public void UpdateADS(bool isChargingOrAiming, bool altFireHeld, WeaponAttachSO currentAttach)
        {
            if (currentAttach == null)
            {
                if (IsADSActive) ExitADS();
                return;
            }

            bool isSniper = currentAttach.name.Contains("Sniper") || (currentAttach.gameplayData != null && currentAttach.gameplayData.hasADS);
            if (!isSniper)
            {
                if (IsADSActive) ExitADS();
                return;
            }

            bool shouldScope = isChargingOrAiming || altFireHeld;
            if (shouldScope && !IsADSActive)
            {
                EnterADS(currentAttach);
            }
            else if (!shouldScope && IsADSActive)
            {
                ExitADS();
            }

            HandleFOVLerp(currentAttach);
        }

        public void HandleFOVLerp(WeaponAttachSO currentAttach)
        {
            if (_camera == null) return;

            float targetFOV = (IsADSActive && currentAttach != null && currentAttach.gameplayData != null)
                ? currentAttach.gameplayData.adsFOV
                : _defaultFOV;

            if (Mathf.Abs(_camera.Lens.FieldOfView - targetFOV) > 0.05f)
            {
                _camera.Lens.FieldOfView = Mathf.Lerp(_camera.Lens.FieldOfView, targetFOV, Time.deltaTime * 12f);
            }

            if (_thirdPersonFollow != null)
            {
                float targetDist = IsADSActive ? 0.02f : _defaultCameraDistance;
                Vector3 targetOffset = IsADSActive ? Vector3.zero : _defaultShoulderOffset;
                float targetArm = IsADSActive ? 0f : _defaultVerticalArmLength;

                _thirdPersonFollow.CameraDistance = Mathf.Lerp(_thirdPersonFollow.CameraDistance, targetDist, Time.deltaTime * 12f);
                _thirdPersonFollow.ShoulderOffset = Vector3.Lerp(_thirdPersonFollow.ShoulderOffset, targetOffset, Time.deltaTime * 12f);
                _thirdPersonFollow.VerticalArmLength = Mathf.Lerp(_thirdPersonFollow.VerticalArmLength, targetArm, Time.deltaTime * 12f);
            }
        }

        public void EnterADS(WeaponAttachSO currentAttach)
        {
            IsADSActive = true;
            if (_scopeOverlayInstance != null) _scopeOverlayInstance.SetActive(true);

            if (_motor != null && currentAttach != null && currentAttach.gameplayData != null)
            {
                _motor.moveSpeed = _originalMoveSpeed * currentAttach.gameplayData.adsMoveSpeedMult;
            }

            // Hide character meshes so they never obstruct scope reticle
            if (_ownerTransform != null)
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

        public void ExitADS()
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
