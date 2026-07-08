using Zenject;

namespace Bootstrap
{
    /// <summary>
    /// Installer on the game scene's SceneContext. It is intentionally empty: the whole game world is now
    /// composed in the ProjectContext (see <see cref="ServiceInstaller"/>.BindGameWorld) and driven explicitly
    /// by the state machine (LoadGameState → IGameWorldBuilder.Build). Nothing about the scene needs scene-scoped
    /// bindings, so this stays a no-op — kept only because the SceneContext references it.
    /// </summary>
    public class GameInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
        }
    }
}
