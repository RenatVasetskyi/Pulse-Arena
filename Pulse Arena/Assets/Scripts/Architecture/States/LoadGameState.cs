using Architecture.Services.Interfaces;
using Architecture.States.Interfaces;
using Data;

namespace Architecture.States
{
    /// <summary>
    /// Loads the game scene (showing the loading screen), then builds the whole world and hands control to
    /// <see cref="GameLoopState"/>. This is where "starting a run" lives — the flow is readable top-to-bottom:
    /// load → build → play. Building is delegated to <see cref="IGameWorldBuilder"/> so this state stays thin.
    /// Re-entering it (restart) rebuilds in place: the scene loader is a no-op when its scene is already active,
    /// and <see cref="GameLoopState"/> has already torn the old world down on exit.
    /// </summary>
    public class LoadGameState : IState
    {
        private readonly ISceneLoader _sceneLoader;
        private readonly IGameWorldBuilder _worldBuilder;
        private readonly IStateMachine _stateMachine;
        private readonly GameSettings _gameSettings;

        public LoadGameState(ISceneLoader sceneLoader, IGameWorldBuilder worldBuilder,
            IStateMachine stateMachine, GameSettings gameSettings)
        {
            _sceneLoader = sceneLoader;
            _worldBuilder = worldBuilder;
            _stateMachine = stateMachine;
            _gameSettings = gameSettings;
        }

        public void Enter()
        {
            _sceneLoader.Load(_gameSettings.GameSceneName, BuildAndRun);
        }

        public void Exit()
        {
        }

        private void BuildAndRun()
        {
            _worldBuilder.Build();
            _stateMachine.Enter<GameLoopState>();
        }
    }
}
