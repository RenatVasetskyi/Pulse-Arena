using Zenject;

namespace Bootstrap
{
    /// <summary>
    ///     Installer on the menu scene's SceneContext — the mirror of <see cref="GameInstaller" />. The menu needs no
    ///     scene-scoped services of its own (it spawns no DI-dependent actors), so this binds exactly one thing. It
    ///     exists so that the menu scene composes and disposes ITSELF, the same way the game scene does, which makes
    ///     the whole app follow ONE rule: a flow state picks WHICH scene, and that scene's builder composes it.
    ///     The SceneContext is also what lets <see cref="MainMenuBuilder" /> receive its ProjectContext services —
    ///     Zenject only injects scene objects through a scene's own context.
    /// </summary>
    public class MenuInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            BindMenuComposition();
        }

        private void BindMenuComposition()
        {
            // NonLazy + IInitializable/IDisposable: the SceneContext builds the menu on load and tears it down on
            // unload, automatically — the flow state no longer owns the presenter's lifetime.
            Container
                .BindInterfacesAndSelfTo<MainMenuBuilder>()
                .AsSingle()
                .NonLazy();
        }
    }
}
