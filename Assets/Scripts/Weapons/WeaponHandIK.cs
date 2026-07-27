using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// Minimal hand attachment via <see cref="OnAnimatorIK"/>. The gun is parented
    /// to the right-hand socket and positioned so its right grip sits in the hand
    /// (see WeaponController snapRightGripToHand), so the RIGHT hand needs no IK by
    /// default. This component pulls the LEFT hand onto the weapon's GripL target.
    /// No head-look, no elbow hints, no weight ramping — intentionally lightweight.
    /// Robust to model move/scale: it targets the live grip transforms each frame.
    /// Requires "IK Pass" enabled on an Animator layer. Lives on the Animator GameObject.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class WeaponHandIK : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private WeaponController weapon;

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (weapon == null) weapon = GetComponentInParent<WeaponController>();
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (animator == null || weapon == null) return;

            bool armed = weapon.IsArmed;
            var attach = weapon.CurrentAttach;

            // --- Left hand -> GripL (support hand) ---
            bool isTrap = attach != null && attach.gameplayData != null && (attach.gameplayData.isTrap || attach.gameplayData.category == WeaponCategory.Trap);
            float lw = (armed && weapon.GripL != null && attach != null && !isTrap) ? attach.leftHandWeight : 0f;
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, lw);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, lw);
            if (lw > 0.001f)
            {
                animator.SetIKPosition(AvatarIKGoal.LeftHand, weapon.GripL.position);
                animator.SetIKRotation(AvatarIKGoal.LeftHand, weapon.GripL.rotation);
            }

            // --- Right hand -> GripR ---
            float rw = (armed && weapon.GripR != null && attach != null && !isTrap) ? attach.rightHandWeight : 0f;
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, rw);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, rw);
            if (rw > 0.001f)
            {
                animator.SetIKPosition(AvatarIKGoal.RightHand, weapon.GripR.position);
                animator.SetIKRotation(AvatarIKGoal.RightHand, weapon.GripR.rotation);
            }
        }

        /// <summary>
        /// Animation Event receiver for the 'Hit' event triggered by melee animation clips.
        /// </summary>
        public void Hit()
        {
            if (weapon != null)
            {
                weapon.OnMeleeHit();
            }
        }
    }
}
