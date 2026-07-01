namespace Shootopia.FSM
{
    public abstract class BaseState
    {
        protected CharacterMotor Ctx;
        protected StateFactory Factory;
        
        protected BaseState CurrentSuperState;
        protected BaseState CurrentSubState;
        
        public BaseState(CharacterMotor context, StateFactory factory)
        {
            Ctx = context;
            Factory = factory;
        }

        public abstract void EnterState();
        public abstract void UpdateState(float dt);
        public abstract void ExitState();
        public abstract void CheckSwitchStates();
        public abstract void InitializeSubState();

        public virtual void OnAnimationFinished(string animName)
        {
            if (CurrentSubState != null)
            {
                CurrentSubState.OnAnimationFinished(animName);
            }
        }

        public void UpdateStates(float dt)
        {
            UpdateState(dt);
            if (CurrentSubState != null)
            {
                CurrentSubState.UpdateStates(dt);
            }
        }

        protected void SwitchState(BaseState newState)
        {
            ExitState();
            newState.EnterState();
            
            if (CurrentSuperState != null)
            {
                CurrentSuperState.SetSubState(newState);
            }
            else
            {
                // If it's a super state, update the context
                Ctx.CurrentState = newState;
            }
        }

        protected void SetSuperState(BaseState newSuperState)
        {
            CurrentSuperState = newSuperState;
        }

        protected void SetSubState(BaseState newSubState)
        {
            CurrentSubState = newSubState;
            newSubState.SetSuperState(this);
        }
    }
}
