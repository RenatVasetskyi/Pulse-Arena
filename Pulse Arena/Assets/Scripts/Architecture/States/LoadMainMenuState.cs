using Architecture.Services.Interfaces;
using Architecture.States.Interfaces;
using Data;
using UI.MainMenu;

namespace Architecture.States
{
    public class LoadMainMenuState : IState
    {
        private readonly ISceneLoader _sceneLoader;
        private readonly IStateMachine _stateMachine;
        private readonly GameSettings _gameSettings;

        private MainMenuPresenter _presenter;

        public LoadMainMenuState(
            ISceneLoader sceneLoader,
            IStateMachine stateMachine,
            GameSettings gameSettings)
        {
            _sceneLoader = sceneLoader;
            _stateMachine = stateMachine;
            _gameSettings = gameSettings;
        }

        public void Enter()
        {
            _sceneLoader.Load(_gameSettings.MainMenuSceneName, CreateMenu);
        }

        public void Exit()
        {
            _presenter?.Dispose();
            _presenter = null;
        }

        private void CreateMenu()
        {
            MainMenuView view = MainMenuView.Create();

            _presenter = new MainMenuPresenter(view, _stateMachine);
            _presenter.Initialize();
        }
    }
}
