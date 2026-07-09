using Game.Common.StateMachine;
using Game.Enemy;

namespace Game.Enemy.States
{
    /// <summary>
    ///     The stasis slice of the old physics-recovery tick: while the shared stasis timer is still running
    ///     the enemy just falls under extra gravity (no sweep, no recovery, no steering). The countdown
    ///     itself is NOT decremented here — it is ticked in <see cref="EnemyTimers.TickFixed" /> (every
    ///     non-dead FixedUpdate, state-independent), exactly as the original TickTimers did, so this state
    ///     only READS the timer. Together that keeps the branch genuinely re-armable: arm a non-zero stasis
    ///     duration and it counts down to 0 and exits, byte-identical to the old behaviour.
    ///     PRESERVED, INTENTIONALLY UNREACHABLE: every path into recovery today (Knockback / Grab / Launch)
    ///     zeroes the stasis timer before transitioning, so it is ALWAYS 0 at entry and no code ever
    ///     transitions INTO this state. It is kept as a first-class state (rather than deleted as dead code)
    ///     so the branch — and its priority above knockback — is faithfully reproduced and ready to be
    ///     re-armed if a future feature sets a non-zero stasis duration. Do not delete it as "unreachable".
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