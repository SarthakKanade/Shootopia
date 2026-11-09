using UnityEngine;

namespace Shootopia
{
    public partial class ThirdPersonCameraController
    {
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

