using UnityEngine;

namespace Shootopia.FSM
{
    public class DashState : BaseState
    {
        private Vector3 _dashDirection;
        private bool _isFinished;

        public DashState(CharacterMotor context, StateFactory factory) : base(context, factory) { }

        public override void EnterState()
        {
            _isFinished = false;
            
            // Capture movement direction
            if (Ctx.InputIntent.Move.sqrMagnitude > 0.01f)
            {
                // Similar to the old character motor dodge direction logic
                Vector3 inputDir = new Vector3(Ctx.InputIntent.Move.x, 0, Ctx.InputIntent.Move.y).normalized;
                _dashDirection = (Ctx.ModelRoot.right * inputDir.x + Ctx.ModelRoot.forward * inputDir.z).normalized;
            }
            else
            {
                _dashDirection = Ctx.ModelRoot.forward; // Default to forward
            }

            Ctx.Animator.SetTrigger(CharacterMotor.HashDash);
            Ctx.PlayDashAudio();
        }

        public override void UpdateState(float dt)
        {
            CheckSwitchStates();
            
            // Move character linearly via direct character controller move
            Ctx.Controller.Move(_dashDirection * Ctx.DashSpeed * dt);
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
            if (animName == "Dash")
            {
                _isFinished = true;
            }
        }
    }
}
