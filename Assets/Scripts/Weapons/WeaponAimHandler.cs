using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// Manages aiming pitch feed to the Animator, combat stance persistence timers,
    /// dynamic weapon stance blending, and bounce laser line preview.
    /// </summary>
    public class WeaponAimHandler
    {
        private readonly Transform _ownerTransform;
        private readonly Animator _animator;
        private readonly CharacterMotor _motor;
        private readonly PlayerAimRig _aimRig;

        private LineRenderer _bounceLineInstance;
        private float _combatStanceTimer = 0f;
        private const float COMBAT_STANCE_DURATION = 1.5f;

        private static readonly int HashStance = Animator.StringToHash("Stance");
        private static readonly int HashAimPitch = Animator.StringToHash("AimPitch");
        private static readonly int HashIsFiring = Animator.StringToHash("IsFiring");
        private static readonly int HashShootSpeed = Animator.StringToHash("ShootSpeed");

        public bool IsInCombatStance => _combatStanceTimer > 0f;

        public WeaponAimHandler(Transform ownerTransform, Animator animator, CharacterMotor motor, PlayerAimRig aimRig)
        {
            _ownerTransform = ownerTransform;
            _animator = animator;
            _motor = motor;
            _aimRig = aimRig;
        }

        public void TriggerCombatStance()
        {
            _combatStanceTimer = COMBAT_STANCE_DURATION;
            if (_motor != null) _motor.IsInCombat = true;
            if (_animator != null)
            {
                _animator.SetBool(HashIsFiring, true);
                _animator.SetFloat(HashShootSpeed, 1f);
            }
        }

        public void ExitCombatStance()
        {
            _combatStanceTimer = 0f;
            if (_motor != null) _motor.IsInCombat = false;
            if (_animator != null)
            {
                _animator.SetBool(HashIsFiring, false);
                _animator.SetFloat(HashShootSpeed, 1f);
            }
        }

        public void UpdateAimState(float dt, bool fireHeld, WeaponAttachSO currentAttach, bool isCharging, bool isAimingTrap)
        {
            // Combat stance timer decay
            if (_combatStanceTimer > 0f)
            {
                _combatStanceTimer -= dt;
                if (_combatStanceTimer <= 0f)
                {
                    ExitCombatStance();
                }
                else if (_combatStanceTimer <= COMBAT_STANCE_DURATION - 0.25f && _animator != null && !fireHeld)
                {
                    _animator.SetFloat(HashShootSpeed, 0f); // Steady aim pose after recoil
                }
            }

            FeedAimPitch();
            UpdateDynamicStance(currentAttach, isCharging);
        }

        private void FeedAimPitch()
        {
            if (_animator == null) return;
            float pitch01 = _aimRig != null ? Mathf.Clamp(_aimRig.Pitch / 45f, -1f, 1f) : 0f;
            _animator.SetFloat(HashAimPitch, pitch01, 0.1f, Time.deltaTime);
        }

        public void UpdateDynamicStance(WeaponAttachSO currentAttach, bool isCharging = false)
        {
            if (_animator == null) return;

            if (currentAttach == null)
            {
                if (_animator.GetFloat(HashStance) != 2.0f)
                {
                    _animator.SetFloat(HashStance, 2.0f);
                }
                return;
            }

            if (currentAttach.gameplayData != null && (currentAttach.gameplayData.isTrap || currentAttach.gameplayData.category == WeaponCategory.Trap))
            {
                _animator.SetFloat(HashStance, 2f);
                return;
            }

            if (currentAttach.stanceId == 3 || (currentAttach.gameplayData != null && currentAttach.gameplayData.name.Contains("Knife")))
            {
                _animator.SetFloat(HashStance, 3f);
                return;
            }

            if (currentAttach.name.Contains("Sniper"))
            {
                bool isFiring = isCharging || (_animator != null && (_animator.GetBool(HashIsFiring) || _animator.GetCurrentAnimatorStateInfo(1).IsName("Shoot")));
                float targetStance = isFiring ? 1f : 0f;
                float currentStance = _animator.GetFloat(HashStance);
                _animator.SetFloat(HashStance, Mathf.MoveTowards(currentStance, targetStance, Time.deltaTime * 6f));
            }
        }

        public void UpdateBouncePreview(bool isOwner, WeaponAttachSO currentAttach, Transform muzzle, IAimProvider aim, bool isAiming = true)
        {
            if (!isOwner || !isAiming || currentAttach == null || currentAttach.gameplayData == null)
            {
                DisableBouncePreview();
                return;
            }

            bool isBounceGun = currentAttach.gameplayData.maxBounces > 0 || currentAttach.name.Contains("TrickShot");
            if (!isBounceGun)
            {
                DisableBouncePreview();
                return;
            }

            EnsureBounceLineRenderer();
            if (_bounceLineInstance == null) return;

            _bounceLineInstance.enabled = true;
            Transform muzzleTr = muzzle != null ? muzzle : _ownerTransform;
            Vector3 startPos = muzzleTr.position;

            Camera mainCam = Camera.main;
            Vector3 camForward = mainCam != null ? mainCam.transform.forward : (aim != null ? aim.CameraPlanarForward : _ownerTransform.forward);
            Vector3 aimPoint = (aim != null && aim.AimPoint != Vector3.zero) ? aim.AimPoint : (startPos + camForward * 100f);
            Vector3 aimDir = (aimPoint - startPos).normalized;
            if (aimDir.sqrMagnitude < 0.001f) aimDir = camForward;

            // Retain Layer 0 (Default) so all level geometry/colliders are hit; only ignore UI and raycast-ignore layers
            int layerMask = ~LayerMask.GetMask("Ignore Raycast", "UI");

            float previewDist = currentAttach.gameplayData.previewDistance > 1f ? currentAttach.gameplayData.previewDistance : 35f;
            int maxBounces = currentAttach.gameplayData.maxBounces > 0 ? currentAttach.gameplayData.maxBounces : 3;

            var points = new System.Collections.Generic.List<Vector3>();
            points.Add(startPos);

            Vector3 curOrigin = startPos;
            Vector3 curDir = aimDir;
            float remainingDist = previewDist;

            for (int b = 0; b <= maxBounces; b++)
            {
                Ray ray = new Ray(curOrigin, curDir);
                RaycastHit[] hits = Physics.RaycastAll(ray, remainingDist, layerMask, QueryTriggerInteraction.Ignore);
                System.Array.Sort(hits, (h1, h2) => h1.distance.CompareTo(h2.distance));

                bool hitFound = false;
                for (int i = 0; i < hits.Length; i++)
                {
                    var h = hits[i];
                    if (_ownerTransform != null && (h.collider.transform == _ownerTransform || h.collider.transform.IsChildOf(_ownerTransform)))
                    {
                        continue; // Do not hit own body
                    }

                    points.Add(h.point);
                    remainingDist -= h.distance;
                    curDir = Vector3.Reflect(curDir, h.normal).normalized;
                    curOrigin = h.point + h.normal * 0.02f;
                    hitFound = true;
                    break;
                }

                if (!hitFound || remainingDist <= 0.5f)
                {
                    if (!hitFound)
                    {
                        points.Add(curOrigin + curDir * remainingDist);
                    }
                    break;
                }
            }

            _bounceLineInstance.positionCount = points.Count;
            _bounceLineInstance.SetPositions(points.ToArray());
        }

        public void DisableBouncePreview()
        {
            if (_bounceLineInstance != null && _bounceLineInstance.enabled)
            {
                _bounceLineInstance.enabled = false;
            }
        }

        private void EnsureBounceLineRenderer()
        {
            if (_bounceLineInstance != null || _ownerTransform == null) return;

            GameObject lineObj = new GameObject("BouncePreviewLine");
            lineObj.transform.SetParent(_ownerTransform, false);
            _bounceLineInstance = lineObj.AddComponent<LineRenderer>();
            _bounceLineInstance.useWorldSpace = true;
            _bounceLineInstance.startWidth = 0.08f;
            _bounceLineInstance.endWidth = 0.04f;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") 
                         ?? Shader.Find("Sprites/Default");

            if (shader != null)
            {
                Material mat = new Material(shader) { name = "BounceLaser_Mat" };
                Color laserColor = new Color(0.15f, 1.0f, 0.45f, 1.0f);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", laserColor);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", laserColor);
                mat.renderQueue = 3000;
                _bounceLineInstance.material = mat;
                _bounceLineInstance.startColor = laserColor;
                _bounceLineInstance.endColor = laserColor;
            }
        }

        public void Cleanup()
        {
            DisableBouncePreview();
            if (_bounceLineInstance != null)
            {
                Object.Destroy(_bounceLineInstance.gameObject);
                _bounceLineInstance = null;
            }
        }
    }
}
