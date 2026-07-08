using Game.Common.StateMachine;
using Game.Enemy;

namespace Game.Enemy.States
{
    /// <summary>
    /// Terminal state: stop the enemy (the old StopForDeath body — disable agent, clear flags, clear
    /// knockback/stasis timers, freeze the rigidbody), play the death pop, then trigger the pool-return
    /// coroutine. The coroutine itself must live on the controller MonoBehaviour, so
    /// <see cref="EnemyContext.StartDeathReturn"/> is a thin one-liner into it; everything else runs here.
    /// Carries the old EnterDeadState body.
    /// </summary>
    public class EnemyDeadState : ActorState
    {
        private readonly EnemyContext _context;

        public EnemyDeadState(EnemyContext context)
        {
            _context = context;
        }

        public override void Enter()
        {
            _context.StopForDeath();
            _context.Visual?.PlayDeath();
            _context.StartDeathReturn();
        }
    }
}
