using UnityEngine;

namespace Shootopia.FSM
{
    public class AnimationExitSMB : StateMachineBehaviour
    {
        [Tooltip("The name to pass back to the FSM when this animation exits.")]
        public string stateName;

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            var motor = animator.GetComponentInParent<CharacterMotor>();
            if (motor != null)
            {
                motor.OnAnimationFinished(stateName);
            }
        }
    }
}
