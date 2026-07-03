using Game.Enemy;
using Game.Cameras;
using Game.Scene;
using Game.Spawning;
using UnityEngine;
using Zenject;

namespace Game.Installers
{
    public class GameInstaller : MonoInstaller
    {
        [SerializeField] private GameSceneReferences _sceneReferences;

        public override void InstallBindings()
        {
            BindSceneReferences();
            BindBattleCamera();
            BindSpawners();
            BindSceneStarter();
        }

        private void BindSceneReferences()
        {
            GameSceneReferences sceneReferences = GetSceneReferences();

            Container
                .Bind<GameSceneReferences>()
                .FromInstance(sceneReferences)
                .AsSingle();
        }

        private void BindBattleCamera()
        {
            Container
                .Bind<IBattleCamera>()
                .FromInstance(GetBattleCamera())
                .AsSingle();
        }

        private void BindSpawners()
        {
            Container
                .Bind<IEnemySpawner>()
                .To<EnemySpawner>()
                .AsSingle();

            Container
                .Bind<IPickupSpawner>()
                .To<PickupSpawner>()
                .AsSingle();
        }

        private void BindSceneStarter()
        {
            Container
                .BindInterfacesTo<GameSceneStarter>()
                .AsSingle()
                .NonLazy();
        }

        private GameSceneReferences GetSceneReferences()
        {
            if (_sceneReferences == null)
                throw new MissingReferenceException($"{nameof(GameInstaller)} requires scene references.");

            return _sceneReferences;
        }

        private BattleCamera GetBattleCamera()
        {
            BattleCamera battleCamera = GetSceneReferences().BattleCamera;

            if (battleCamera == null)
                throw new MissingReferenceException($"{nameof(BattleCamera)} was not found on the game scene.");

            return battleCamera;
        }
    }
}
