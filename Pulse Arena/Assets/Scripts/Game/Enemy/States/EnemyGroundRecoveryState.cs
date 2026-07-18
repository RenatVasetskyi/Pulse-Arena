using Game.Common.StateMachine;
using Game.Enemy;
using UnityEngine;

namespace Game.Enemy.States
{
    /// <summary>
    ///     After the knockback timer runs out the enemy keeps falling under extra gravity (and, while still a thrown
    ///     projectile, keeps sweeping for impact damage) until it touches ground and can snap onto it. Counts up the
    ///     physics-recovery timer, then finishes (clears flags, zeroes vertical velocity, snaps to ground) and returns
    ///     to chasing.
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

        private void FinishPhysicsRecovery()
        {
            _context.NeedsGroundRecovery = false;
            _context.IsImpactProjectile = false;
            _context.Visual?.SetThrown(false);
            _context.GroundRecovery.Finish();
        }
    }
}