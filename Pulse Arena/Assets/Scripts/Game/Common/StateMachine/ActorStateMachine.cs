namespace Game.Common.StateMachine
{
    /// <summary>
    ///     A one-state-at-a-time actor FSM. Beyond the plain <see cref="ChangeState" /> (Exit old → Enter new), it
    ///     supports a mechanical <see cref="Pause" />/<see cref="Resume" />: the pause state is OVERLAID on the
    ///     running state without Exiting it, and Resume hands control back without re-Entering it — so the
    ///     interrupted state keeps its exact fields + delta-timers and continues from the same frame, which is the
    ///     whole point of the game's mechanical pause. The pause state's own <c>Enter</c>/<c>Exit</c> carry the
    ///     freeze / restore. While paused, <see cref="Tick" />/<see cref="FixedTick" /> drive the pause state (which
    ///     does nothing), so the suspended state is never ticked — its timers freeze for free.
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

        // Drop the pause state (its Exit restores) and hand control back to the suspended state — NOT re-entered,
        // so it continues from the exact frame + countdowns it was frozen at.
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
