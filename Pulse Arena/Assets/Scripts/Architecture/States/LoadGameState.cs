using Architecture.Services.Interfaces;
using Architecture.States.Interfaces;
using Data;

namespace Architecture.States
{
    /// <summary>
    ///     Loads the game scene, then hands off. Match composition and teardown live in the scene's SceneContext
    ///     (GameInstaller → GameWorldBuilder), which builds the world on load and disposes it on unload. A pure
    ///     "which scene" flow step; restart is just a scene reload.
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
            // Only loads the scene; the game SceneContext's GameWorldBuilder.Build() (NonLazy IInitializable)
            // composes the whole match — that is the real composition root to read.
            _sceneLoader.Load(_gameSettings.GameSceneName);
        }

        public void Exit()
        {
        }
    }
}