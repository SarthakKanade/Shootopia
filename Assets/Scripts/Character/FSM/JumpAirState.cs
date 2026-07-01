using UnityEngine;

namespace Shootopia.FSM
{
    public class JumpAirState : BaseState
    {
        public JumpAirState(CharacterMotor context, StateFactory factory) : base(context, factory) { }

        public override void EnterState() { }

        public override void UpdateState(float dt)
        {
            CheckSwitchStates();
            Ctx.ApplyMovementAndRotation(dt);
        }

        public override void ExitState() { }

        public override void CheckSwitchStates()
        {
            // JumpAir transitions to Fall automatically based on vertical velocity?
            // For now, JumpAir can just act as the single Airborne sub-state.
            // If the user wants a separate FallState, we switch here.
        }

        public override void InitializeSubState() { }
    }
}
