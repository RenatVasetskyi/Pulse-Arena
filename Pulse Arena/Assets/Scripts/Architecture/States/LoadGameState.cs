using Architecture.Services.Interfaces;
using Architecture.States.Interfaces;
using Data;

namespace Architecture.States
{
    /// <summary>
    ///     Loads the game scene, then hands off — nothing more. Composition and teardown live in the scene's
    ///     SceneContext (GameInstaller → GameWorldBuilder), which builds the world on load and disposes it on unload
    ///     automatically. This state stays a pure "which scene" flow step; restart is just a scene reload.
    /// </summary>
    public class LoadGameState : IState
    {
        private readonly GameSettings _gameSettings;
        private readonly ISceneLoader _sceneLoader;

        public LoadGameState(ISceneLoader sceneLoader, GameSettings gameSettings)
        {
            _sceneLoader = sceneLoader;
            _gameSettings = gameSettings;
        }

        public void Enter()
        {
            // Just loads the scene. What happens NEXT: the game scene's SceneContext (GameInstaller) instantiates
            // GameWorldBuilder (NonLazy IInitializable), whose Build() composes the whole match. To read the
            // game-scene setup top-to-bottom, open GameWorldBuilder.Build() — THAT is the composition root.
            _sceneLoader.Load(_gameSettings.GameSceneName);
        }

        public void Exit()
        {
        }
    }
}