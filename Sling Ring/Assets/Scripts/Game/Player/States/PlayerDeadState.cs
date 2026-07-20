using Game.Common.StateMachine;

namespace Game.Player.States
{
    /// <summary>
    ///     Terminal state: plays the death animation on entry, then stays inert — the controller's other
    ///     transitions all guard the dead flag, so nothing pulls the player back out.
    /// </summary>
    public class PlayerDeadState : ActorState
    {
        private readonly PlayerContext _context;

        public PlayerDeadState(PlayerContext context)
        {
            _context = context;
        }

        public override void Enter()
        {
            _context.Visual?.PlayDeath();
        }
    }
}
