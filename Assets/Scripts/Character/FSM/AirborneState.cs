using UnityEngine;

namespace Shootopia.FSM
{
    public class AirborneState : BaseState
    {
        public AirborneState(CharacterMotor context, StateFactory factory) : base(context, factory) { }

        public override void EnterState()
        {
            InitializeSubState();
        }

        public override void UpdateState(float dt)
        {
            CheckSwitchStates();
            
            // Apply tuned gravity with apex hang and crisp fall multiplier
            float effectiveGravity = Ctx.Gravity;
            if (Mathf.Abs(Ctx.VerticalVelocity) < 2.5f)
            {
                // Near apex: reduced gravity for responsive mid-air feel and control
                effectiveGravity *= 0.65f;
            }
            else if (Ctx.VerticalVelocity < 0f)
            {
                // Falling: slightly increased gravity for crisp landing
                effectiveGravity *= 1.15f;
            }

            Ctx.VerticalVelocity += effectiveGravity * dt;

            // Terminal velocity
            if (Ctx.VerticalVelocity < -50f) {
                Ctx.VerticalVelocity = -50f;
            }
        }

        public override void ExitState()
        {
            if (CurrentSubState != null)
            {
                CurrentSubState.ExitState();
                CurrentSubState = null;
            }
        }

        public override void CheckSwitchStates()
        {
            if (Ctx.IsGrounded && !Ctx.IsInLava && Ctx.VerticalVelocity <= 0f)
            {
                SwitchState(Factory.Grounded());
            }
        }

        public override void InitializeSubState()
        {
            if (Ctx.VerticalVelocity > 0)
            {
                SetSubState(Factory.JumpAir());
            }
            else
            {
                // Just fall
                SetSubState(Factory.JumpAir()); // We can use the same state or create a FallState later
            }
        }
    }
}
