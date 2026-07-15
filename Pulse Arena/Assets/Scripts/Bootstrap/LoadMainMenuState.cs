using Architecture.Services.Interfaces;
using Architecture.States.Interfaces;
using Data;

namespace Architecture.States
{
    /// <summary>
    ///     Loads the menu scene, then hands off — nothing more, exactly like <see cref="LoadGameState" />.
    ///     Composition and teardown live in the menu scene's SceneContext (MenuInstaller → MainMenuBuilder), which
    ///     builds the menu on load and disposes it on unload automatically. This state stays a pure "which scene"
    ///     flow step.
    /// </summary>
    public class LoadMainMenuState : IState
    {
        private readonly GameSettings _gameSettings;
        private readonly ISceneLoader _sceneLoader;

        public LoadMainMenuState(ISceneLoader sceneLoader, GameSettings gameSettings)
        {
            _sceneLoader = sceneLoader;
            _gameSettings = gameSettings;
        }

        public void Enter()
        {
            // Just loads the scene. What happens NEXT: the menu scene's SceneContext (MenuInstaller) instantiates
            // MainMenuBuilder (NonLazy IInitializable), whose Build() composes the menu. Same rule as LoadGameState —
            // to read the menu setup top-to-bottom, open MainMenuBuilder.Build().
            _sceneLoader.Load(_gameSettings.MainMenuSceneName);
        }

        public void Exit()
        {
        }
    }
}
