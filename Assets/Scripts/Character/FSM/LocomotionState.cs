using UnityEngine;

namespace Shootopia.FSM
{
    public class LocomotionState : BaseState
    {
        public LocomotionState(CharacterMotor context, StateFactory factory) : base(context, factory) { }

        public override void EnterState()
        {
            if (Ctx.Animator != null && Ctx.Animator.layerCount > 1)
            {
                Ctx.Animator.SetLayerWeight(1, 1f);
            }
        }

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
            else if (Ctx.InputIntent.Roll)
            {
                SwitchState(Factory.Roll());
            }
        }

        public override void InitializeSubState() { }
    }
}
