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
            
            // Apply gravity
            Ctx.VerticalVelocity += Ctx.Gravity * dt;

            // Optional terminal velocity
            if (Ctx.VerticalVelocity < -50f) {
                Ctx.VerticalVelocity = -50f;
            }
        }

        public override void ExitState() { }

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
