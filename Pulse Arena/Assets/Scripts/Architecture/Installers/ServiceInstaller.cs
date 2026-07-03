using Architecture.Services;
using Architecture.Services.Interfaces;
using Data;
using Game.Enemy;
using Game.Enemy.Interfaces;
using Game.Pickups;
using Game.Pickups.Interfaces;
using Game.Player;
using Game.Player.Interfaces;
using UnityEngine;
using Zenject;

namespace Architecture.Installers
{
    public class ServiceInstaller : MonoInstaller, ICoroutineRunner
    {
        [SerializeField] private GameSettings _gameSettings;

        public override void InstallBindings()
        {
            BindGameSettings();
            BindCoroutineRunner();
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
            Container
                .BindInterfacesTo<ServiceInstaller>()
                .FromInstance(this)
                .AsSingle()
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
