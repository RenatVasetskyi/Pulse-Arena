using Architecture.Services.Interfaces;
using Architecture.States.Interfaces;
using Data;
using UI.MainMenu;
using UnityEngine;
using Zenject;

namespace Bootstrap
{
    /// <summary>
    ///     THE composition root of the menu scene — read <see cref="Build" /> top-to-bottom to see the ENTIRE menu
    ///     setup in one place. The exact mirror of <see cref="Game.Scene.GameWorldBuilder" />, so both scenes answer
    ///     "where is this built?" the same way. It lives in the menu scene's SceneContext and runs off that context's
    ///     kernel: <see cref="Initialize" /> (→ Build) fires when the scene loads, <see cref="Dispose" /> (→ Teardown)
    ///     fires automatically when the scene unloads — so no FSM state has to remember to dispose the presenter.
    /// </summary>
    public class MainMenuBuilder : IInitializable, System.IDisposable
    {
        private readonly IAudioService _audioService;
        private readonly GameSettings _gameSettings;
        private readonly ILevelProgressService _levelProgress;
        private readonly ILevelService _levelService;
        private readonly ISettingsController _settingsController;
        private readonly IStateMachine _stateMachine;

        private MainMenuPresenter _presenter;

        public MainMenuBuilder(IStateMachine stateMachine, GameSettings gameSettings, IAudioService audioService,
            ISettingsController settingsController, ILevelService levelService, ILevelProgressService levelProgress)
        {
            _stateMachine = stateMachine;
            _gameSettings = gameSettings;
            _audioService = audioService;
            _settingsController = settingsController;
            _levelService = levelService;
            _levelProgress = levelProgress;
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
            GameObject prefab = _gameSettings.Prefabs.MainMenuPrefab;

            if (prefab == null)
            {
                Debug.LogError("MainMenuPrefab is not assigned in Game Settings → Prefabs.");
                return null;
            }

            return Object.Instantiate(prefab).GetComponent<MainMenuView>();
        }

        private void BindPresenter(MainMenuView view)
        {
            _presenter = new MainMenuPresenter(view, _stateMachine, _audioService, _settingsController,
                _levelService, _levelProgress, _gameSettings);
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
