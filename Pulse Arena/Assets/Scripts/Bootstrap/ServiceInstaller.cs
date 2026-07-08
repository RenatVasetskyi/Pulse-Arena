using Architecture.Services;
using Architecture.Services.Interfaces;
using Data;
using Game.Arena;
using Game.Arena.Interfaces;
using Game.Enemy;
using Game.Enemy.Interfaces;
using Game.Pickups;
using Game.Pickups.Interfaces;
using Game.Player;
using Game.Player.Interfaces;
using Game.Scene;
using Game.Spawning;
using UI;
using UI.Loading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using Zenject;

namespace Bootstrap
{
    public class ServiceInstaller : MonoInstaller
    {
        [SerializeField] private GameSettings _gameSettings;

        private EventSystem _eventSystem;

        public override void InstallBindings()
        {
            BindGameSettings();
            BindCoroutineRunner();
            CreateUiEventSystem();
            BindLoadingScreen();
            BindSceneLoader();
            BindFactories();
            BindInputService();
            BindScoreService();
            BindSettingsService();
            BindAudioService();
            BindGameWorld();
        }

        private void BindGameSettings()
        {
            Container
                .Bind<GameSettings>()
                .FromScriptableObject(_gameSettings)
                .AsSingle();
        }

        private void BindCoroutineRunner()
        {
            CoroutineRunner coroutineRunner = new GameObject("CoroutineRunner")
                .AddComponent<CoroutineRunner>();
            coroutineRunner.transform.SetParent(transform);

            Container
                .Bind<ICoroutineRunner>()
                .FromInstance(coroutineRunner)
                .AsSingle()
                .NonLazy();
        }

        private void CreateUiEventSystem()
        {
            GameObject eventSystem = new("EventSystem");
            eventSystem.transform.SetParent(transform);
            _eventSystem = eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();

            Container
                .Bind<EventSystem>()
                .FromInstance(_eventSystem)
                .AsSingle();
        }

        private void BindLoadingScreen()
        {
            LoadingScreenView view = Instantiate(_gameSettings.Prefabs.LoadingScreenPrefab, transform)
                .GetComponent<LoadingScreenView>();

            Container
                .Bind<ILoadingScreen>()
                .To<LoadingScreen>()
                .AsSingle()
                .WithArguments(view)
                .NonLazy();
        }

        private void BindSceneLoader()
        {
            Container
                .Bind<ISceneLoader>()
                .To<SceneLoader>()
                .AsSingle();
        }

        private void BindFactories()
        {
            Container
                .Bind<IArenaFactory>()
                .To<ArenaFactory>()
                .AsSingle();

            Container
                .Bind<IPlayerFactory>()
                .To<PlayerFactory>()
                .AsSingle();

            Container
                .Bind<IEnemyFactory>()
                .To<EnemyFactory>()
                .AsSingle();

            Container
                .Bind<IPickupFactory>()
                .To<PickupFactory>()
                .AsSingle();
        }

        private void BindInputService()
        {
            Container
                .Bind<IInputService>()
                .To<InputService>()
                .AsSingle()
                .NonLazy();
        }

        private void BindScoreService()
        {
            Container
                .Bind<IScoreService>()
                .To<ScoreService>()
                .AsSingle();

            Container
                .Bind<IComboService>()
                .To<ComboService>()
                .AsSingle();

            Container
                .Bind<ISlowMoService>()
                .To<SlowMoService>()
                .AsSingle();

            Container
                .BindInterfacesTo<SuperMeterService>()
                .AsSingle()
                .NonLazy();

            Container
                .Bind<IScorePopupService>()
                .To<ScorePopupService>()
                .AsSingle();
        }

        private void BindSettingsService()
        {
            Container
                .Bind<ISettingsService>()
                .To<SettingsService>()
                .AsSingle()
                .NonLazy();

            Container
                .Bind<ISettingsController>()
                .To<SettingsController>()
                .AsSingle();
        }

        private void BindAudioService()
        {
            AudioService audioService = new GameObject("AudioService")
                .AddComponent<AudioService>();
            audioService.transform.SetParent(transform);
            audioService.Initialize(_gameSettings.AudioData, Container.Resolve<ISettingsService>());

            Container
                .Bind<IAudioService>()
                .FromInstance(audioService)
                .AsSingle()
                .NonLazy();
        }

        // Game-world composition lives here (ProjectContext) rather than in the game scene, so the state
        // machine can drive it: LoadGameState resolves IGameWorldBuilder and calls Build(). These are
        // app-lifetime singletons — Build() re-initializes them each run, GameLoopState.Teardown() resets them.
        private void BindGameWorld()
        {
            Container
                .Bind<IEnemySpawner>()
                .To<EnemySpawner>()
                .AsSingle();

            Container
                .Bind<IPickupSpawner>()
                .To<PickupSpawner>()
                .AsSingle();

            Container
                .Bind<IPitFactory>()
                .To<PitFactory>()
                .AsSingle();

            Container
                .Bind<IPitSpawner>()
                .To<PitSpawner>()
                .AsSingle();

            Container
                .Bind<GameplayFeedbackDirector>()
                .AsSingle();

            Container
                .Bind<HudPresenter>()
                .AsSingle();

            Container
                .Bind<GameFlowController>()
                .AsSingle();

            Container
                .Bind<IGameWorldBuilder>()
                .To<GameWorldBuilder>()
                .AsSingle();
        }
    }
}
