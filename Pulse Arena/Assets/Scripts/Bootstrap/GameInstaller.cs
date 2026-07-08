using Game.Arena;
using Game.Arena.Interfaces;
using Game.Enemy;
using Game.Scene;
using Game.Spawning;
using Zenject;

namespace Bootstrap
{
    /// <summary>
    /// Installer on the game scene's SceneContext. It composes the whole match: the spawners + the three
    /// collaborators + the <see cref="GameWorldBuilder"/>. The builder is bound NonLazy as IInitializable, so the
    /// SceneContext's kernel runs its Build() on scene load and — the whole reason we use a SceneContext — calls
    /// its Dispose()/Teardown() automatically when the scene unloads. No manual lifecycle, no leak-prone teardown.
    /// The parent ProjectContext (global services) is never touched by this scope.
    /// </summary>
    public class GameInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            BindSpawners();
            BindWorldComposition();
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

            Container
                .Bind<IPitFactory>()
                .To<PitFactory>()
                .AsSingle();

            Container
                .Bind<IPitSpawner>()
                .To<PitSpawner>()
                .AsSingle();
        }

        private void BindWorldComposition()
        {
            Container
                .Bind<GameplayFeedbackDirector>()
                .AsSingle();

            Container
                .Bind<HudPresenter>()
                .AsSingle();

            Container
                .Bind<GameFlowController>()
                .AsSingle();

            // NonLazy + IInitializable/IDisposable: the SceneContext builds the world on load and tears it down
            // on unload, automatically and in isolation from the ProjectContext.
            Container
                .BindInterfacesAndSelfTo<GameWorldBuilder>()
                .AsSingle()
                .NonLazy();
        }
    }
}
