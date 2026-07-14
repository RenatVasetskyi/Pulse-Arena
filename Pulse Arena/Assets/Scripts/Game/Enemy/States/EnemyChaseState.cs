using Game.Common.StateMachine;

namespace Game.Enemy.States
{
    /// <summary>
    ///     The pursue ("run") state: a thin FSM shell that clears the actor's grabbed/thrown/recovery flags on entry,
    ///     hands agent control back to the mover, then delegates the per-tick pursue/attack DECISION to the enemy
    ///     type's <see cref="Game.Enemy.Behaviors.IEnemyBehavior" /> (via <see cref="EnemyContext.Behavior" />).
    ///     What the enemy does while chasing — run + melee for the default brain, or hold-distance + fire for a future
    ///     ranged one — lives in that behaviour, not here, so a new archetype needs no change to this state.
    /// </summary>
    public class EnemyChaseState : ActorState
    {
        private readonly EnemyContext _context;

        public EnemyChaseState(EnemyContext context)
        {
            _context = context;
        }

        public override void Enter()
        {
            _context.IsGrabbed = false;
            _context.NeedsGroundRecovery = false;
            _context.IsImpactProjectile = false;
            _context.Visual?.SetGrabbed(false);
            _context.Visual?.SetThrown(false);
            _context.TryEnableAgentControl();
            _context.Behavior.OnEnterChase();
        }

        public override void FixedTick()
        {
            _context.Behavior.Tick();
        }
    }
}
