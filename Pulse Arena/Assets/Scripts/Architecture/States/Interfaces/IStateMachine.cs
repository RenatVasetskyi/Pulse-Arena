namespace Architecture.States.Interfaces
{
    public interface IStateMachine
    {
        void AddState<TState>(TState state) where TState : class, IExitableState;
        void Enter<TState>() where TState : class, IState;
    }
}