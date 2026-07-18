using Game.Common.StateMachine;
using Game.Enemy;

namespace Game.Enemy.States
{
    /// <summary>
    ///     While the shared stasis timer runs the enemy just falls under extra gravity (no sweep, no recovery, no
    ///     steering). The countdown is NOT decremented here — it ticks in <see cref="EnemyTimers.TickFixed" /> (every
    ///     non-dead FixedUpdate, state-independent), so this state only READS it.
    ///     PRESERVED, INTENTIONALLY UNREACHABLE: every recovery path (Knockback / Grab / Launch) zeroes the stasis
    ///     timer before transitioning, so it is always 0 at entry and nothing ever transitions INTO this state. Kept
    ///     as a first-class re-armable state (not deleted as dead code) for a future feature that sets a non-zero
    ///     stasis duration. Do not delete it as "unreachable".
    /// </summary>
    public class EnemyStasisState : ActorState
    {
        private readonly EnemyContext _context;

        public EnemyStasisState(EnemyContext context)
        {
            _context = context;
        }

        public override void FixedTick()
        {
            if (_context.Target == null)
                return;

            if (_context.Timers.Stasis.Remaining > 0f)
                _context.ApplyExtraGravity();
        }
    }
}