using UnityEngine;

namespace Shootopia.FSM
{
    public class JumpEndState : BaseState
    {
        private bool _isFinished;

        public JumpEndState(CharacterMotor context, StateFactory factory) : base(context, factory) { }

        public override void EnterState()
        {
            _isFinished = false;
        }

        public override void UpdateState(float dt)
        {
            CheckSwitchStates();
            
            // Allow slight movement or clamp movement on landing?
            Ctx.ApplyMovementAndRotation(dt * 0.5f); // 50% movement speed while landing
        }

        public override void ExitState() { }

        public override void CheckSwitchStates()
        {
            if (_isFinished)
            {
                SwitchState(Factory.Locomotion());
            }
        }

        public override void InitializeSubState() { }

        public override void OnAnimationFinished(string animName)
        {
            base.OnAnimationFinished(animName);
            if (animName == "JumpEnd")
            {
                _isFinished = true;
            }
        }
    }
}
