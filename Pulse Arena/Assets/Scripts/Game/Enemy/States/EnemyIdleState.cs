using Game.Common.StateMachine;
using Game.Enemy;

namespace Game.Enemy.States
{
    /// <summary>
    ///     No one to hunt (the player died, so the target reads null): halt in place and wait. The moment a target
    ///     returns it hands back to <see cref="EnemyChaseState" />. The idle animation is the visual's speed blend
    ///     (velocity ~0), so this state drives no clip.
    /// </summary>
    public class EnemyIdleState : ActorState
    {
        private readonly EnemyContext _context;

        public EnemyIdleState(EnemyContext context)
        {
            _context = context;
        }

        public override void Enter()
        {
            _context.Movement.StopInPlace();
        }

        public override void FixedTick()
        {
            if (_context.Target != null)
            {
                _context.ChangeToChaseState();
                return;
            }

            _context.Movement.StopInPlace(); // idempotent — hold the halt while there's no target
        }
    }
}
