using UnityEngine;

namespace Shootopia.FSM
{
    public class LocomotionState : BaseState
    {
        public LocomotionState(CharacterMotor context, StateFactory factory) : base(context, factory) { }

        public override void EnterState() { }

        public override void UpdateState(float dt)
        {
            CheckSwitchStates();
            
            // Allow moving
            Ctx.ApplyMovementAndRotation(dt);
        }

        public override void ExitState() { }

        public override void CheckSwitchStates()
        {
            if (Ctx.InputIntent.Jump)
            {
                SwitchState(Factory.JumpStart());
            }
            else if (Ctx.InputIntent.Dash && Ctx.IsMoving)
            {
                SwitchState(Factory.Dash());
            }
            else if (Ctx.InputIntent.Roll && Ctx.IsMoving)
            {
                SwitchState(Factory.Roll());
            }
        }

        public override void InitializeSubState() { }
    }
}
