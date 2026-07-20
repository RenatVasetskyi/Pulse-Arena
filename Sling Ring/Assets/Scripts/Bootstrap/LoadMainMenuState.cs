using Architecture.Services.Interfaces;
using Architecture.States.Interfaces;
using Data;

namespace Architecture.States
{
    /// <summary>
    ///     Loads the menu scene, then hands off — nothing more, exactly like <see cref="LoadGameState" />.
    ///     Composition and teardown live in the menu scene's SceneContext (MenuInstaller → MainMenuBuilder), which
    ///     builds on load and disposes on unload automatically.
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
            // Just loads the scene — the menu scene's SceneContext (MenuInstaller → MainMenuBuilder.Build) composes
            // the menu on load. Read MainMenuBuilder.Build() top-to-bottom for the menu setup.
            _sceneLoader.Load(_gameSettings.MainMenuSceneName);
        }

        public void Exit()
        {
        }
    }
}
