using Architecture.Services.Interfaces;
using Architecture.States.Interfaces;

namespace Architecture.States
{
    /// <summary>
    /// Represents "a run is active". It is intentionally thin: the world is already built and runs itself while
    /// this state is current. Its job is the exit boundary — when the flow leaves gameplay (restart → back to
    /// <see cref="LoadGameState"/>, or quit → LoadMainMenuState), it tears the world down so the next state
    /// starts from a clean slate.
    /// </summary>
    public class GameLoopState : IState
    {
        private readonly IGameWorldBuilder _worldBuilder;

        public GameLoopState(IGameWorldBuilder worldBuilder)
        {
            _worldBuilder = worldBuilder;
        }

        public void Enter()
        {
        }

        public void Exit()
        {
            _worldBuilder.Teardown();
        }
    }
}
