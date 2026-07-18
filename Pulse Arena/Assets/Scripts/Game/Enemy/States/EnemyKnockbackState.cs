using Game.Common.StateMachine;
using Game.Enemy;
using UnityEngine;

namespace Game.Enemy.States
{
    /// <summary>
    ///     The "enemy got flung" state — both Knockback and Launch enter here. Enter disables the agent, marks
    ///     ground-recovery, shows the thrown visual and wakes the body. FixedTick owns the knockback timer (this is
    ///     the ONE place it ticks — not the controller's generic timer loop): the countdown runs target-independently
    ///     at the top, while gravity / impact sweep / ground-recovery handoff below are target-guarded. On expiry it
    ///     hands off to ground recovery and drives one ground-recovery tick on the same physics frame so recovery
    ///     isn't deferred a step.
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
            // Target-independent slice: the countdown must advance even when the target is null, otherwise it
            // freezes mid-flight. OnKnockbackExpired is a no-op in practice — ground recovery is always pending here.
            if (_context.Timers.Knockback.Remaining > 0f)
                _context.Timers.Knockback.Set(_context.Timers.Knockback.Remaining - Time.fixedDeltaTime);
            else
                OnKnockbackExpired();

            // Target-guarded slice: the null-target return skips only gravity / sweep / the ground-recovery
            // handoff, never the countdown above.
            if (_context.Target == null)
                return;

            if (_context.Timers.Knockback.Remaining > 0f)
            {
                _context.ApplyExtraGravity();

                if (_context.IsImpactProjectile)
                    _context.SweepImpactDamage();

                return;
            }

            // Knockback expired: hand off to ground recovery (the projectile-flag/thrown-visual drop already ran
            // above via OnKnockbackExpired).
            _context.ChangeToGroundRecoveryState();

            // Drive one ground-recovery tick on the SAME frame so recovery isn't deferred a physics step. Guarded by
            // IsDead so a dead enemy (ChangeTo* is a no-op, knockback still active) can't re-enter this method.
            if (!_context.IsDead)
                _groundRecoveryState.FixedTick();
        }

        // Drop the projectile flag + thrown visual UNLESS ground recovery is pending. Called every frame the
        // knockback timer is <= 0; a no-op in practice because recovery is always pending here.
        private void OnKnockbackExpired()
        {
            if (_context.NeedsGroundRecovery)
                return;

            _context.IsImpactProjectile = false;
            _context.Visual?.SetThrown(false);
        }
    }
}