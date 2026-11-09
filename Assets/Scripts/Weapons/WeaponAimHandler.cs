using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// Manages aiming pitch feed to the Animator, combat stance persistence timers,
    /// dynamic weapon stance blending, and bounce laser line preview.
    /// </summary>
    public partial class WeaponAimHandler
    {
        private readonly Transform _ownerTransform;
        private readonly Animator _animator;
        private readonly CharacterMotor _motor;
        private readonly PlayerAimRig _aimRig;

        private LineRenderer _bounceLineInstance;
        private float _bounceLineOffset = 0f;
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

        public void UpdateAimState(float dt, bool fireHeld, WeaponAttachSO currentAttach, bool isCharging, bool isAimingTrap, bool isAimingBounce = false)
        {
            if (isCharging || isAimingBounce)
            {
                // Maintain combat stance continuously while charging or aiming bounce shot
                _combatStanceTimer = COMBAT_STANCE_DURATION;
                if (_motor != null) _motor.IsInCombat = true;
                if (_animator != null)
                {
                    _animator.SetBool(HashIsFiring, true);
                    _animator.SetFloat(HashShootSpeed, 0f); // Steady aim/charge pose
                }
            }
            else if (_combatStanceTimer > 0f)
            {
                // Combat stance timer decay after firing or exiting charge
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

            if (currentAttach.stanceId == 3 || (currentAttach.gameplayData != null && (currentAttach.gameplayData.category == WeaponCategory.Melee || currentAttach.gameplayData.name.Contains("Knife"))))
            {
                _animator.SetFloat(HashStance, 3f);
                return;
            }

            if (currentAttach.name.Contains("Sniper") || (currentAttach.gameplayData != null && (currentAttach.gameplayData.CanZoom || currentAttach.gameplayData.category == WeaponCategory.Precision)))
            {
                bool isFiring = isCharging || (_animator != null && (_animator.GetBool(HashIsFiring) || _animator.GetCurrentAnimatorStateInfo(1).IsName("Shoot")));
                float targetStance = isFiring ? 1f : 0f;
                float currentStance = _animator.GetFloat(HashStance);
                _animator.SetFloat(HashStance, Mathf.MoveTowards(currentStance, targetStance, Time.deltaTime * 6f));
            }
        }
    }
}
