using Game.Common.StateMachine;
using Game.Enemy;
using UnityEngine;

namespace Game.Enemy.States
{
    /// <summary>
    ///     The knockback slice of the old physics-recovery tick, and the entry point for every "the enemy
    ///     got flung" flow (Knockback / Launch both transition here). Enter reproduces the old
    ///     EnterPhysicsRecoveryState body (disable agent, clear grabbed, mark ground-recovery, thrown visual,
    ///     wake body) — the body that used to live on the controller as ContextOnEnterPhysicsRecovery.
    ///     FixedTick owns the knockback timer AND its expiry side effect (this is the one place it lives —
    ///     it is NOT ticked by the controller's generic timer loop). The decrement + its else-expiry side
    ///     effect run TARGET-INDEPENDENTLY at the top of FixedTick, reproducing the old
    ///     EnemyController.TickTimers knockback block that advanced every non-dead FixedUpdate with no
    ///     target guard; only the gravity / sweep / ground-recovery-handoff work below is target-guarded,
    ///     exactly as the old FixedTickPhysicsRecoveryState's own `target == null` early-return was. While
    ///     the timer runs the enemy falls under extra gravity and, if it is a thrown projectile, sweeps for
    ///     impact damage. When the timer expires it hands off to the ground-recovery state, and — to match
    ///     the ORIGINAL single-state tick byte-for-byte — immediately drives ONE ground-recovery tick on
    ///     the SAME physics frame (in the old FixedTickPhysicsRecoveryState the expiry frame fell straight
    ///     through the knockback branch into the ground-recovery branch, so gravity, the sweep-damage check,
    ///     the recovery-timer increment and even the earliest possible finish all happened that same frame).
    /// </summary>
    public class EnemyKnockbackState : ActorState
    {
        private readonly EnemyContext _context;
        private readonly EnemyGroundRecoveryState _groundRecoveryState;

        public EnemyKnockbackState(EnemyContext context, EnemyGroundRecoveryState groundRecoveryState)
        {
            _context = context;
            _groundRecoveryState = groundRecoveryState;
        }

        public override void Enter()
        {
            _context.Movement.DisableAgent();
            _context.IsGrabbed = false;
            _context.NeedsGroundRecovery = true;
            _context.Visual?.SetGrabbed(false);
            _context.Visual?.SetThrown(_context.IsImpactProjectile);

            if (_context.Rigidbody == null)
                return;

            _context.Rigidbody.useGravity = true;
            _context.Rigidbody.isKinematic = false;
            _context.Rigidbody.WakeUp();
        }

        public override void FixedTick()
        {
            // TARGET-INDEPENDENT slice — reproduces the old EnemyController.TickTimers knockback block,
            // which ran every non-dead FixedUpdate with NO target guard (orig lines 826-832). The
            // decrement AND its else-expiry side effect must advance even when the target transform is
            // null, otherwise the countdown would freeze mid-flight (byte-parity divergence). Structured
            // exactly like the original: "if (timer > 0) decrement; else if (!needsGroundRecovery) drop
            // flags". OnKnockbackExpired itself guards on NeedsGroundRecovery, so the else-branch fires
            // every frame the timer is <= 0 — matching the original else-if — and is a no-op in practice
            // because ground recovery is always pending here.
            if (_context.Timers.Knockback.Remaining > 0f)
                _context.Timers.Knockback.Set(_context.Timers.Knockback.Remaining - Time.fixedDeltaTime);
            else
                OnKnockbackExpired();

            // TARGET-GUARDED slice — reproduces the old FixedTickPhysicsRecoveryState body, whose own
            // `if (_target == null) return;` (orig line 688) skipped ONLY gravity / sweep / the
            // ground-recovery handoff, never the countdown above.
            if (_context.Target == null)
                return;

            if (_context.Timers.Knockback.Remaining > 0f)
            {
                _context.ApplyExtraGravity();

                if (_context.IsImpactProjectile)
                    _context.SweepImpactDamage();

                return;
            }

            // Knockback expired: hand off to ground recovery. (The projectile-flag/thrown-visual drop
            // already ran above via OnKnockbackExpired, matching the original TickTimers else-branch.)
            _context.ChangeToGroundRecoveryState();

            // BYTE-EXACT PARITY: in the old single-state tick the SAME frame that zeroed the knockback
            // timer fell through into the ground-recovery branch (increment the recovery timer, apply
            // gravity, sweep, and possibly finish + ChangeToChase). ChangeToGroundRecoveryState above made
            // the ground-recovery state active, so ticking that SAME instance once here runs that block on
            // the expiry frame instead of deferring it a physics step. Guarded by IsDead so a dead enemy
            // (where ChangeTo* is a no-op and knockback would still be active) can't re-enter this method —
            // matching the old dead FixedUpdate path, which never ran recovery.
            if (!_context.IsDead)
                _groundRecoveryState.FixedTick();
        }

        // The old TickTimers "else if (!_needsGroundRecovery)" expiry side effect (orig lines 828-832):
        // drop the projectile flag + thrown visual UNLESS ground recovery is pending. Called every frame
        // the knockback timer is <= 0 (from the else-branch above), target-independent, exactly like the
        // original. Ground recovery is always pending here, so this is a no-op in practice — preserved
        // exactly for parity.
        private void OnKnockbackExpired()
        {
            if (_context.NeedsGroundRecovery)
                return;

            _context.IsImpactProjectile = false;
            _context.Visual?.SetThrown(false);
        }
    }
}