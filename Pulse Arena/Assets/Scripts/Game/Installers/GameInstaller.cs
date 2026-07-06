using Game.Enemy;
using Game.Scene;
using Game.Spawning;
using Zenject;

namespace Game.Installers
{
    public class GameInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            BindSpawners();
            BindSceneStarter();
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
            // GameSceneStarter creates the arena via IArenaFactory and pulls the scene references +
            // battle camera off the instantiated arena — nothing scene-bound is needed here anymore.
            Container
                .BindInterfacesTo<GameSceneStarter>()
                .AsSingle()
                .NonLazy();
        }
    }
}
