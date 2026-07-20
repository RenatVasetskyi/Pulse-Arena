using Zenject;

namespace Bootstrap
{
    /// <summary>
    ///     Installer on the menu scene's SceneContext — the mirror of <see cref="GameInstaller" />. The menu needs no
    ///     scene-scoped services (it spawns no DI-dependent actors), so this binds exactly one thing, keeping the
    ///     app's one rule: a flow state picks WHICH scene, that scene's builder composes it. The SceneContext is also
    ///     what lets <see cref="MainMenuBuilder" /> receive its ProjectContext services.
    /// </summary>
    public class MenuInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            BindMenuComposition();
        }

        private void BindMenuComposition()
        {
            // NonLazy IInitializable/IDisposable: the SceneContext builds the menu on load, tears it down on unload.
            Container
                .BindInterfacesAndSelfTo<MainMenuBuilder>()
                .AsSingle()
                .NonLazy();
        }
    }
}
