using UnityEngine;

namespace Shootopia.FSM
{
    public class RollState : BaseState
    {
        private Vector3 _rollDirection;
        private bool _isFinished;
        private float _elapsedTime;

        public RollState(CharacterMotor context, StateFactory factory) : base(context, factory) { }

        public override void EnterState()
        {
            _isFinished = false;
            _elapsedTime = 0f;
            Ctx.TriggerRollCooldown();
            
            Vector3 fwd = Ctx.Aim != null ? Ctx.Aim.CameraPlanarForward : Ctx.ModelRoot.forward;
            Vector3 right = Ctx.Aim != null ? Ctx.Aim.CameraPlanarRight : Ctx.ModelRoot.right;
            Vector3 desiredDir = fwd * Ctx.InputIntent.Move.y + right * Ctx.InputIntent.Move.x;

            if (desiredDir.sqrMagnitude > 0.01f)
            {
                _rollDirection = desiredDir.normalized;
            }
            else
            {
                _rollDirection = Ctx.ModelRoot.forward;
            }

            if (_rollDirection.sqrMagnitude > 0.001f)
            {
                Ctx.ModelRoot.rotation = Quaternion.LookRotation(_rollDirection, Vector3.up);
                Ctx.SyncModelRotation();
            }

            if (Ctx.Animator != null)
            {
                // Force blend tree directly to forward roll (aligned with character model facing)
                Ctx.Animator.SetFloat(CharacterMotor.HashMoveX, 0f);
                Ctx.Animator.SetFloat(CharacterMotor.HashMoveY, 1f);
                Ctx.Animator.SetFloat(CharacterMotor.HashSpeed, 1f);
                Ctx.Animator.SetBool(CharacterMotor.HashIsFiring, false);

                // Immediate crossfade starting right at the active dive frame (skip standing wind-up)
                Ctx.Animator.CrossFadeInFixedTime("Roll", 0.02f, 0, 0.07f);
                Ctx.Animator.SetTrigger(CharacterMotor.HashRoll);
            }

            Ctx.PlayRollAudio();
        }

        public override void UpdateState(float dt)
        {
            _elapsedTime += dt;
            if (_elapsedTime >= Ctx.RollDuration)
            {
                _isFinished = true;
                CheckSwitchStates();
                return;
            }

            // Snappy dive velocity curve: launches immediately into dive, peaks through somersault,
            // and eases into run speed as feet touch down.
            // Average speed multiplier = 1.0 (exact distance = RollSpeed * RollDuration = 11.96m).
            float t = Mathf.Clamp01(_elapsedTime / Ctx.RollDuration);
            float speedMultiplier = (t < 0.70f) ? 1.04f : Mathf.Lerp(1.04f, 0.77f, (t - 0.70f) / 0.30f);

            // Integrate slight downward gravity force so roll sticks to sloped ground
            float verticalSpeed = Ctx.IsGrounded ? -2f : Ctx.VerticalVelocity;
            Ctx.Controller.Move((_rollDirection * (Ctx.RollSpeed * speedMultiplier) + Vector3.up * verticalSpeed) * dt);
        }

        public override void ExitState()
        {
            if (Ctx.Animator != null && Ctx.Animator.layerCount > 1)
            {
                Ctx.Animator.SetLayerWeight(1, 1f);
            }
        }

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
