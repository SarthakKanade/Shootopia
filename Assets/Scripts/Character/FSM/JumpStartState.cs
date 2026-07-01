using UnityEngine;

namespace Shootopia.FSM
{
    public class JumpStartState : BaseState
    {
        public JumpStartState(CharacterMotor context, StateFactory factory) : base(context, factory) { }

        public override void EnterState()
        {
            Ctx.Animator.SetTrigger(CharacterMotor.HashJump);

            // Apply upward launch velocity immediately.
            Ctx.VerticalVelocity = Mathf.Sqrt(Ctx.JumpForce * -2f * Ctx.Gravity);
        }

        public override void UpdateState(float dt)
        {
            CheckSwitchStates();

            // Integrate gravity so vertical velocity always decays. The parent GroundedState
            // promotes us to Airborne on the first frame the launch un-grounds the player;
            // this is a safety net that guarantees flight can never persist even for a stray frame.
            Ctx.VerticalVelocity += Ctx.Gravity * dt;

            Ctx.ApplyMovementAndRotation(dt);
        }

        public override void ExitState() { }

        public override void CheckSwitchStates()
        {
            // Transition is handled by the parent GroundedState: once the launch lifts the
            // player off the ground, GroundedState swaps the root state to Airborne (which
            // sets JumpAir as its sub-state and continues integrating gravity).
        }

        public override void InitializeSubState() { }
    }
}
