using Architecture.Services.Interfaces;
using Architecture.States.Interfaces;
using Data;
using UI.MainMenu;
using UnityEngine;

namespace Architecture.States
{
    public class LoadMainMenuState : IState
    {
        private readonly ISceneLoader _sceneLoader;
        private readonly IStateMachine _stateMachine;
        private readonly GameSettings _gameSettings;
        private readonly IAudioService _audioService;

        private MainMenuPresenter _presenter;

        public LoadMainMenuState(
            ISceneLoader sceneLoader,
            IStateMachine stateMachine,
            GameSettings gameSettings,
            IAudioService audioService)
        {
            _sceneLoader = sceneLoader;
            _stateMachine = stateMachine;
            _gameSettings = gameSettings;
            _audioService = audioService;
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
            GameObject prefab = _gameSettings.Prefabs.MainMenuPrefab;

            if (prefab == null)
            {
                Debug.LogError("MainMenuPrefab is not assigned in Game Settings → Prefabs.");
                return;
            }

            MainMenuView view = Object.Instantiate(prefab).GetComponent<MainMenuView>();

            _presenter = new MainMenuPresenter(view, _stateMachine, _audioService);
            _presenter.Initialize();

            _audioService.PlayMusic(_gameSettings.AudioData.MenuMusic);
        }
    }
}
