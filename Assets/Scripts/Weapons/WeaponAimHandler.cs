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
            UpdateDynamicStance(currentAttach);
        }

        private void FeedAimPitch()
        {
            if (_animator == null) return;
            float pitch01 = _aimRig != null ? Mathf.Clamp(_aimRig.Pitch / 45f, -1f, 1f) : 0f;
            _animator.SetFloat(HashAimPitch, pitch01, 0.1f, Time.deltaTime);
        }

        public void UpdateDynamicStance(WeaponAttachSO currentAttach)
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
                bool isFiring = _animator.GetCurrentAnimatorStateInfo(1).IsName("Shoot");
                float targetStance = isFiring ? 1f : 0f;
                float currentStance = _animator.GetFloat(HashStance);
                _animator.SetFloat(HashStance, Mathf.MoveTowards(currentStance, targetStance, Time.deltaTime * 6f));
            }
        }

        public void UpdateBouncePreview(bool isOwner, WeaponAttachSO currentAttach, Transform muzzle, IAimProvider aim)
        {
            if (!isOwner || currentAttach == null || currentAttach.gameplayData == null)
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
            Vector3 aimDir = mainCam != null ? mainCam.transform.forward : (aim != null ? aim.CameraPlanarForward : _ownerTransform.forward);

            int layerMask = ~LayerMask.GetMask("Ignore Raycast", "Trigger", "UI");
            if (_ownerTransform != null) layerMask &= ~(1 << _ownerTransform.gameObject.layer);

            float previewDist = currentAttach.gameplayData.previewDistance > 1f ? currentAttach.gameplayData.previewDistance : 25f;

            if (mainCam != null)
            {
                Ray aimRay = new Ray(mainCam.transform.position, mainCam.transform.forward);
                if (Physics.Raycast(aimRay, out RaycastHit camHit, 100f, layerMask, QueryTriggerInteraction.Ignore))
                {
                    Vector3 toCamHit = (camHit.point - startPos).normalized;
                    if (Vector3.Dot(toCamHit, aimDir) > 0.2f)
                    {
                        aimDir = toCamHit;
                    }
                }
            }

            if (Physics.Raycast(startPos, aimDir, out RaycastHit hit, previewDist, layerMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 reflectedDir = Vector3.Reflect(aimDir, hit.normal);
                float remainingDist = previewDist - hit.distance;

                _bounceLineInstance.positionCount = 3;
                _bounceLineInstance.SetPosition(0, startPos);
                _bounceLineInstance.SetPosition(1, hit.point);
                _bounceLineInstance.SetPosition(2, hit.point + reflectedDir * Mathf.Max(remainingDist, 6f));
            }
            else
            {
                _bounceLineInstance.positionCount = 2;
                _bounceLineInstance.SetPosition(0, startPos);
                _bounceLineInstance.SetPosition(1, startPos + aimDir * previewDist);
            }
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
            _bounceLineInstance.startWidth = 0.045f;
            _bounceLineInstance.endWidth = 0.015f;

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") 
                         ?? Shader.Find("Universal Render Pipeline/Unlit") 
                         ?? Shader.Find("Sprites/Default");

            if (shader != null)
            {
                Material mat = new Material(shader) { name = "BounceLaser_Mat" };
                Color laserColor = new Color(0.15f, 1.0f, 0.45f, 0.9f);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", laserColor);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", laserColor);
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", laserColor * 3.0f);
                }
                _bounceLineInstance.material = mat;
            }

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(new Color(0.8f, 1f, 0.8f), 0.0f), 
                    new GradientColorKey(new Color(0.15f, 1f, 0.4f), 0.4f),
                    new GradientColorKey(new Color(0.0f, 0.9f, 0.85f), 1.0f)
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0.95f, 0.0f), 
                    new GradientAlphaKey(0.85f, 0.6f), 
                    new GradientAlphaKey(0.05f, 1.0f) 
                }
            );
            _bounceLineInstance.colorGradient = gradient;
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
