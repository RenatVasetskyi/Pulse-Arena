using Architecture.States.Interfaces;
using Data;
using Game.Common;
using UnityEngine;

namespace Architecture.States
{
    public class BootstrapState : IState
    {
        private readonly GameSettings _gameSettings;
        private readonly IStateMachine _stateMachine;

        public BootstrapState(IStateMachine stateMachine, GameSettings gameSettings)
        {
            _stateMachine = stateMachine;
            _gameSettings = gameSettings;
        }

        public void Enter()
        {
            Application.targetFrameRate = _gameSettings.TargetFrameRate;
            ActorGroundingUtility.Configure(_gameSettings.Grounding);
            _stateMachine.Enter<LoadMainMenuState>();
        }

        public void Exit()
        {
        }
    }
}