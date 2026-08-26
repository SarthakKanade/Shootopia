namespace Shootopia.FSM
{
    public class GroundedState : BaseState
    {
        public GroundedState(CharacterMotor context, StateFactory factory) : base(context, factory) { }

        public override void EnterState()
        {
            InitializeSubState();
            if (!Ctx.IsInLava)
            {
                Ctx.Animator.SetBool(CharacterMotor.HashGrounded, true);
            }
        }

        public override void UpdateState(float dt)
        {
            CheckSwitchStates();
            
            // Keep player snapped to ground
            if (Ctx.VerticalVelocity < 0) {
                Ctx.VerticalVelocity = -2f;
            }
        }

        public override void ExitState()
        {
            if (CurrentSubState != null)
            {
                CurrentSubState.ExitState();
                CurrentSubState = null;
            }
            Ctx.Animator.SetBool(CharacterMotor.HashGrounded, false);
        }

        public override void CheckSwitchStates()
        {
            // If we fall off a ledge (or the jump impulse pushes us up and un-grounds us),
            // promote to Airborne where gravity is integrated. The Jump animation trigger
            // fired in JumpStartState is not cleared by Airborne, so the jump anim still plays.
            if (!Ctx.IsGrounded || Ctx.IsInLava)
            {
                SwitchState(Factory.Airborne());
            }
        }

        public override void InitializeSubState()
        {
            SetSubState(Factory.Locomotion());
        }
    }
}
