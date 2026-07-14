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
using Game.Turrets;
using Zenject;

namespace Bootstrap
{
    /// <summary>
    ///     Installer on the game scene's SceneContext. It composes the whole match: the spawners + the three
    ///     collaborators + the <see cref="GameWorldBuilder" />. The builder is bound NonLazy as IInitializable, so the
    ///     SceneContext's kernel runs its Build() on scene load and — the whole reason we use a SceneContext — calls
    ///     its Dispose()/Teardown() automatically when the scene unloads. No manual lifecycle, no leak-prone teardown.
    ///     The parent ProjectContext (global services) is never touched by this scope.
    ///     The world factories (arena/player/enemy/pickup) are bound HERE, not in ProjectContext, so the DiContainer
    ///     each factory captures is the SceneContext one — InstantiatePrefabForComponent then resolves both project
    ///     AND scene bindings, which keeps future scene-scoped dependencies on the player/enemy resolvable.
    /// </summary>
    public class GameInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            BindFactories();
            BindSpawners();
            BindWorldComposition();
        }

        private void BindFactories()
        {
            // Scene-scoped so enemies (instantiated with the SceneContext container) can inject it and
            // self-register for O(1) resolution in the impact sweep. A fresh registry per match.
            Container
                .Bind<IEnemyRegistry>()
                .To<EnemyRegistry>()
                .AsSingle();

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

            Container
                .Bind<ITurretFactory>()
                .To<TurretFactory>()
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

            Container
                .Bind<IPitFactory>()
                .To<PitFactory>()
                .AsSingle();

            Container
                .Bind<IPitSpawner>()
                .To<PitSpawner>()
                .AsSingle();

            Container
                .Bind<ITurretSpawner>()
                .To<TurretSpawner>()
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

            Container
                .Bind<OnboardingController>()
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