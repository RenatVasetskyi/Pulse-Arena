using Architecture.Services;
using Architecture.Services.Interfaces;
using Data;
using Game.Enemy;
using Game.Enemy.Interfaces;
using Game.Pickups;
using Game.Pickups.Interfaces;
using Game.Player;
using Game.Player.Interfaces;
using UI.Loading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using Zenject;

namespace Architecture.Installers
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
        }
    }
}
