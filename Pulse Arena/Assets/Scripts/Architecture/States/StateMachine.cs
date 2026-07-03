using System;
using System.Collections.Generic;
using Architecture.States.Interfaces;

namespace Architecture.States
{
    public class StateMachine : IStateMachine
    {
        private readonly Dictionary<Type, IExitableState> _states = new();
        private IExitableState _activeState;

        public void AddState<TState>(TState state) where TState : class, IExitableState
        {
            if (!_states.TryAdd(typeof(TState), state))
                throw new InvalidOperationException($"{typeof(TState).Name} is already registered.");
        }

        public void Enter<TState>() where TState : class, IState
        {
            TState state = ChangeState<TState>();
            state.Enter();
        }

        private TState ChangeState<TState>() where TState : class, IExitableState
        {
            _activeState?.Exit();

            TState state = GetState<TState>();
            _activeState = state;

            return state;
        }

        private TState GetState<TState>() where TState : class, IExitableState
        {
            if (_states.TryGetValue(typeof(TState), out IExitableState state))
                return state as TState;

            throw new InvalidOperationException($"{typeof(TState).Name} is not registered.");
        }
    }
}
