using UnityEngine;
using Shootopia.FSM;

namespace Shootopia
{
    public partial class CharacterMotor
    {
        public void ApplyMovementAndRotation(float dt, float speedMultiplier = 1f)
        {
            Vector3 fwd = Aim != null ? Aim.CameraPlanarForward : Vector3.forward;
            Vector3 right = Aim != null ? Aim.CameraPlanarRight : Vector3.right;
            
            Vector3 worldMove = fwd * InputIntent.Move.y + right * InputIntent.Move.x;
            if (worldMove.sqrMagnitude > 1f) worldMove.Normalize();

            if (Aim != null && fwd.sqrMagnitude >= 0.0004f)
            {
                Quaternion target = Quaternion.LookRotation(fwd, Vector3.up);
                float activeRotSpeed = (IsInCombat || (animator != null && animator.GetBool(HashIsFiring))) ? 1400f : rotationSpeed;
                ModelRoot.rotation = Quaternion.RotateTowards(ModelRoot.rotation, target, activeRotSpeed * dt);
                _modelRotation.Value = ModelRoot.rotation;
            }

            Vector3 localMove = ModelRoot.InverseTransformDirection(worldMove);
            if (animator != null)
            {
                animator.SetFloat(HashMoveX, localMove.x, animDamp, dt);
                animator.SetFloat(HashMoveY, localMove.z, animDamp, dt);
                animator.SetFloat(HashSpeed, worldMove.magnitude, animDamp, dt);
            }

            Vector3 velocity = worldMove * (moveSpeed * speedMultiplier * MoveSpeedMultiplier) + Vector3.up * VerticalVelocity;
            Controller.Move(velocity * dt);

            // Immediate zero-latency ground contact detection
            if (Controller.isGrounded && VerticalVelocity <= 0f && !_isInLava.Value)
            {
                if (animator != null)
                {
                    animator.SetBool(HashGrounded, true);
                }
                if (CurrentState is AirborneState)
                {
                    CurrentState.ExitState();
                    CurrentState = _factory.Grounded();
                    CurrentState.EnterState();
                    VerticalVelocity = -2f;
                }
            }
        }
    }
}
