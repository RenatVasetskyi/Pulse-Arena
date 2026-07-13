using Architecture.Services;
using Architecture.Services.Interfaces;
using Data;
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
            BindInputService();
            BindScoreService();
            BindLevelServices();
            BindSettingsService();
            BindPauseService();
            BindAudioService();
        }

        // The linear-campaign pair: the roster + current selection (ILevelService, reads GameSettings.Levels) and the
        // persistent unlock/stars (ILevelProgressService, PlayerPrefs). ProjectContext-scoped so the chosen level +
        // progress survive the menu → match scene load.
        private void BindLevelServices()
        {
            Container.Bind<ILevelService>().To<LevelService>().AsSingle();
            Container.Bind<ILevelProgressService>().To<LevelProgressService>().AsSingle();
        }

        private void BindPauseService()
        {
            // ProjectContext-scoped so the global AudioService AND per-scene gameplay objects (player,
            // enemies, spawners) can all register with the same mechanical pause. Scene pausables unregister
            // on teardown/pool-return; GameWorldBuilder.Teardown calls Clear() as a safety net.
            Container
                .Bind<IPauseService>()
                .To<PauseService>()
                .AsSingle();
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

        // Plain-C# service (NOT a MonoBehaviour): it instantiates its AudioHost prefab (from config) for the actual
        // AudioSources. BindInterfacesAndSelfTo so the container also owns its IDisposable (destroys the host at
        // shutdown). Constructor injection is safe — a NonLazy singleton is built after all installers register.
        private void BindAudioService()
        {
            Container
                .BindInterfacesAndSelfTo<AudioService>()
                .AsSingle()
                .NonLazy();
        }
    }
}