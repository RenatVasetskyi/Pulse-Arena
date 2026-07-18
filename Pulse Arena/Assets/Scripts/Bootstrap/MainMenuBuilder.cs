using Architecture.Services.Interfaces;
using Architecture.States.Interfaces;
using Data;
using UI.MainMenu;
using UnityEngine;
using Zenject;

namespace Bootstrap
{
    /// <summary>
    ///     THE composition root of the menu scene — read <see cref="Build" /> top-to-bottom for the entire menu
    ///     setup. The mirror of <see cref="Game.Scene.GameWorldBuilder" />: runs off the SceneContext kernel, so
    ///     <see cref="Initialize" /> (→ Build) fires on scene load and <see cref="Dispose" /> (→ Teardown) on unload
    ///     automatically — no FSM state has to dispose the presenter.
    /// </summary>
    public class MainMenuBuilder : IInitializable, System.IDisposable
    {
        private readonly IAudioService _audioService;
        private readonly GameSettings _gameSettings;
        private readonly ILevelProgressService _levelProgress;
        private readonly ILevelService _levelService;
        private readonly ISettingsController _settingsController;
        private readonly IStateMachine _stateMachine;
        private readonly IWindowFactory _windowFactory;

        private MainMenuPresenter _presenter;

        public MainMenuBuilder(IStateMachine stateMachine, GameSettings gameSettings, IAudioService audioService,
            ISettingsController settingsController, ILevelService levelService, ILevelProgressService levelProgress,
            IWindowFactory windowFactory)
        {
            _stateMachine = stateMachine;
            _gameSettings = gameSettings;
            _audioService = audioService;
            _settingsController = settingsController;
            _levelService = levelService;
            _levelProgress = levelProgress;
            _windowFactory = windowFactory;
        }

        public void Initialize()
        {
            Build();
        }

        public void Dispose()
        {
            Teardown();
        }

        private void Build()
        {
            MainMenuView view = SpawnView();

            if (view == null)
                return;

            BindPresenter(view);
            PlayMenuMusic();
        }

        private MainMenuView SpawnView()
        {
            return _windowFactory.Create<MainMenuView>(_gameSettings.Prefabs.MainMenuPrefab, "MainMenuPrefab");
        }

        private void BindPresenter(MainMenuView view)
        {
            _presenter = new MainMenuPresenter(view, _stateMachine, _audioService, _settingsController,
                _levelService, _levelProgress, _gameSettings, _windowFactory);
            _presenter.Initialize();
        }

        private void PlayMenuMusic()
        {
            _audioService.PlayMusic(_gameSettings.AudioData.MenuMusic);
        }

        // Only unsubscribes: the view was instantiated into the menu scene, so Unity destroys it with the scene.
        // Destroying it here would mean touching GameObjects mid scene-unload for no gain.
        private void Teardown()
        {
            _presenter?.Dispose();
            _presenter = null;
        }
    }
}
