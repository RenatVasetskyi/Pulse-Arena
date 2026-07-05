namespace Game.Common.StateMachine
{
    public class ActorStateMachine
    {
        private IActorState _activeState;

        public IActorState ActiveState => _activeState;

        public void ChangeState(IActorState state)
        {
            _activeState?.Exit();
            _activeState = state;
            _activeState?.Enter();
        }

        public void Tick()
        {
            _activeState?.Tick();
        }

        public void FixedTick()
        {
            _activeState?.FixedTick();
        }

        public void Clear()
        {
            _activeState?.Exit();
            _activeState = null;
        }
    }
}
