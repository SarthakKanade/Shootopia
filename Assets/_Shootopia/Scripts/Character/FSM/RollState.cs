using UnityEngine;

namespace Shootopia.FSM
{
    public class RollState : BaseState
    {
        private Vector3 _rollDirection;
        private bool _isFinished;

        public RollState(CharacterMotor context, StateFactory factory) : base(context, factory) { }

        public override void EnterState()
        {
            _isFinished = false;
            
            if (Ctx.InputIntent.Move.sqrMagnitude > 0.01f)
            {
                Vector3 inputDir = new Vector3(Ctx.InputIntent.Move.x, 0, Ctx.InputIntent.Move.y).normalized;
                _rollDirection = (Ctx.ModelRoot.right * inputDir.x + Ctx.ModelRoot.forward * inputDir.z).normalized;
            }
            else
            {
                _rollDirection = Ctx.ModelRoot.forward;
            }

            Ctx.Animator.SetTrigger(CharacterMotor.HashRoll);
        }

        public override void UpdateState(float dt)
        {
            CheckSwitchStates();
            
            Ctx.Controller.Move(_rollDirection * Ctx.RollSpeed * dt);
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
            if (animName == "Roll")
            {
                _isFinished = true;
            }
        }
    }
}
