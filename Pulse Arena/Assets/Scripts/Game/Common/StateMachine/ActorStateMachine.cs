namespace Game.Common.StateMachine
{
    /// <summary>
    ///     A one-state-at-a-time actor FSM. Beyond plain <see cref="ChangeState" /> (Exit old → Enter new), its
    ///     mechanical <see cref="Pause" />/<see cref="Resume" /> overlays a pause state without Exiting the running
    ///     one and hands control back without re-Entering it, so the interrupted state keeps its fields + delta-timers
    ///     and resumes from the same frame. While paused, ticks drive the (inert) pause state, so the suspended state
    ///     freezes for free.
    /// </summary>
    public class ActorStateMachine
    {
        private IActorState _activeState;
        private IActorState _suspendedState;

        public IActorState ActiveState => _activeState;
        public bool IsPaused => _suspendedState != null;

        public void ChangeState(IActorState state)
        {
            _activeState?.Exit();
            _activeState = state;
            _activeState?.Enter();
        }

        public void Clear()
        {
            _activeState?.Exit();
            _activeState = null;
            _suspendedState = null;
        }

        // Overlay the pause state on the current one WITHOUT exiting it, so its fields + timers survive the freeze.
        public void Pause(IActorState pausedState)
        {
            if (_suspendedState != null)
                return;

            _suspendedState = _activeState;
            _activeState = pausedState;
            _activeState?.Enter();
        }

        // Suspended state is handed back, NOT re-entered, so it continues from the frame + countdowns it froze at.
        public void Resume()
        {
            if (_suspendedState == null)
                return;

            _activeState?.Exit();
            _activeState = _suspendedState;
            _suspendedState = null;
        }

        public void FixedTick()
        {
            _activeState?.FixedTick();
        }

        public void Tick()
        {
            _activeState?.Tick();
        }
    }
}
