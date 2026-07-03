using Architecture.Services.Interfaces;
using Architecture.States.Interfaces;
using Data;

namespace Architecture.States
{
    public class LoadGameState : IState
    {
        private readonly ISceneLoader _sceneLoader;
        private readonly GameSettings _gameSettings;

        public LoadGameState(ISceneLoader sceneLoader, GameSettings gameSettings)
        {
            _sceneLoader = sceneLoader;
            _gameSettings = gameSettings;
        }

        public void Enter()
        {
            _sceneLoader.Load(_gameSettings.GameSceneName);
        }

        public void Exit()
        {
        }
    }
}
