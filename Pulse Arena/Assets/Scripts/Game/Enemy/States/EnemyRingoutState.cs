using Game.Common;
using Game.Common.StateMachine;
using Game.Enemy;
using UnityEngine;

namespace Game.Enemy.States
{
    /// <summary>
    ///     The enemy fell off the arena (or was flung into a pit): mark it dead, clear the flung-state flags
    ///     and timers, stop the agent, zero the health bar and run the ringout feedback (award kill, sting,
    ///     burst — via <see cref="EnemyContext.ResolveRingout" />), then free the rigidbody constraints and
    ///     tumble it out. FixedTick counts up the ringout timer, keeps applying extra gravity, shrinks the
    ///     scale, and returns the enemy to the pool once the shrink completes. Carries the old
    ///     EnterRingoutState / FixedTickRingoutState bodies (the controller-coupled slice — set dead, health
    ///     bar, RingoutHandler — runs through the single ResolveRingout callback in the exact original order).
    /// </summary>
    public class EnemyRingoutState : ActorState
    {
        private readonly EnemyContext _context;

        public EnemyRingoutState(EnemyContext context)
        {
            _context = context;
        }

        public override void Enter()
        {
            ResetFlungState();

            // Controller-coupled slice, in original order: mark dead, zero the health bar, resolve the ringout.
            _context.ResolveRingout();

            TumbleOffEdge();
        }

        public override void FixedTick()
        {
            _context.Timers.RingoutElapsed += Time.fixedDeltaTime;
            ActorPhysicsUtility.ApplyExtraGravity(_context.Rigidbody, _context.Data.ExtraGravity);

            float progress =
                Mathf.Clamp01(_context.Timers.RingoutElapsed / Mathf.Max(0.1f, _context.Data.RingoutDuration));
            _context.Transform.localScale = Vector3.one * Mathf.Lerp(1f, _context.Data.RingoutShrinkScale, progress);

            if (progress >= 1f)
                _context.ReturnToPool();
        }

        private void ResetFlungState()
        {
            _context.IsGrabbed = false;
            _context.IsImpactProjectile = false;
            _context.NeedsGroundRecovery = false;
            _context.Visual?.SetGrabbed(false); // an enemy rung out WHILE held must tumble, not keep the struggle anim
            _context.Timers.Knockback.Clear();
            _context.Timers.Stasis.Clear();
            _context.Timers.RingoutElapsed = 0f;
            _context.Movement.DisableAgent();
        }

        private void TumbleOffEdge()
        {
            if (_context.Rigidbody == null)
                return;

            _context.Rigidbody.isKinematic = false;
            _context.Rigidbody.useGravity = true;

            // Ragdoll-ish tumble: free the upright constraints and spin it as it flies off the edge.
            _context.Rigidbody.constraints = RigidbodyConstraints.None;
            _context.Rigidbody.angularVelocity = new Vector3(
                Random.Range(-10f, 10f),
                Random.Range(-4f, 4f),
                Random.Range(-10f, 10f));
        }
    }
}