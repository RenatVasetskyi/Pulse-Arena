using Game.Common.StateMachine;
using Game.Enemy;
using UnityEngine;

namespace Game.Enemy.States
{
    /// <summary>
    /// The ground-recovery slice of the old physics-recovery tick: after the knockback timer has run
    /// out the enemy keeps falling under extra gravity (and keeps sweeping for impact damage while it
    /// is still a thrown projectile) until it has touched ground and can snap onto it. Counts up the
    /// physics-recovery timer, then finishes (clears flags, zeroes vertical velocity, snaps to ground —
    /// the old FinishPhysicsRecovery body) and returns to chasing.
    /// </summary>
    public class EnemyGroundRecoveryState : ActorState
    {
        private readonly EnemyContext _context;

        public EnemyGroundRecoveryState(EnemyContext context)
        {
            _context = context;
        }

        public override void FixedTick()
        {
            if (_context.Target == null)
                return;

            if (!_context.NeedsGroundRecovery)
                return;

            _context.Timers.PhysicsRecoveryElapsed += Time.fixedDeltaTime;
            _context.ApplyExtraGravity();

            if (_context.IsImpactProjectile)
                _context.SweepImpactDamage();

            if (!_context.GroundRecovery.CanFinish())
                return;

            FinishPhysicsRecovery();
            _context.ChangeToChaseState();
        }

        // The old controller FinishPhysicsRecovery body: clear the recovery/projectile flags + thrown
        // visual, then run the pure-physics snap-to-ground on the recovery controller.
        private void FinishPhysicsRecovery()
        {
            _context.NeedsGroundRecovery = false;
            _context.IsImpactProjectile = false;
            _context.Visual?.SetThrown(false);
            _context.GroundRecovery.Finish();
        }
    }
}
