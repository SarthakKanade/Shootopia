namespace Shootopia.FSM
{
    public class StateFactory
    {
        private CharacterMotor _context;
        
        public BaseState Grounded() => new GroundedState(_context, this);
        public BaseState Airborne() => new AirborneState(_context, this);
        public BaseState Locomotion() => new LocomotionState(_context, this);
        public BaseState Dash() => new DashState(_context, this);
        public BaseState Roll() => new RollState(_context, this);
        public BaseState JumpStart() => new JumpStartState(_context, this);
        public BaseState JumpAir() => new JumpAirState(_context, this);
        public BaseState JumpEnd() => new JumpEndState(_context, this);

        public StateFactory(CharacterMotor currentContext)
        {
            _context = currentContext;
        }
    }
}
