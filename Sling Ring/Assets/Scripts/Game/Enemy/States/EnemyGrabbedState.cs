using Game.Common.StateMachine;
using Game.Enemy;
using UnityEngine;

namespace Game.Enemy.States
{
    /// <summary>
    ///     The enemy is held by the slingshot. The rigidbody is driven directly (see MoveGrabbed on the controller);
    ///     this state handles entry (stop the agent, set the held flags + held-damage grace, grabbed visual, wake the
    ///     body) and the "damages the player while held" behaviour for the enemy types that have it, throttled by a
    ///     grace + interval.
    /// </summary>
    public class EnemyGrabbedState : ActorState
    {
        private readonly EnemyContext _context;
        private RigidbodyConstraints _cachedConstraints;
        private RigidbodyInterpolation _cachedInterpolation;

        public EnemyGrabbedState(EnemyContext context)
        {
            _context = context;
        }

        public override void Enter()
        {
            _context.Movement.DisableAgent();
            _context.IsGrabbed = true;
            _context.NeedsGroundRecovery = false;
            _context.IsImpactProjectile = false;
            _context.Timers.HeldDamage.Set(_context.Data.HeldDamageGrace);
            _context.Visual?.SetGrabbed(true);
            _context.Visual?.SetThrown(false);

            ConfigureHeldRigidbody();
        }

        // Configure the held body — upright, freeze rotation, interpolate — all restored on Exit. Ordering is
        // load-bearing: level upright BEFORE freezing, and interpolation is safe only because Enter disabled the agent.
        private void ConfigureHeldRigidbody()
        {
            if (_context.Rigidbody == null)
                return;

            _context.Rigidbody.useGravity = true;
            _context.Rigidbody.isKinematic = false;

            // Level the body upright (keep only its heading) BEFORE freezing — a grab caught mid-tumble/knockback
            // would otherwise stay held sideways, and the horizontal wrap ring then sits off the tilted body.
            _context.Rigidbody.rotation = Quaternion.Euler(0f, _context.Transform.eulerAngles.y, 0f);

            // Freeze ALL rotation while held: the lasso drives the body's POSITION directly, so bumping into
            // another enemy must not spin it. The struggle animation is purely visual (a child transform), so the
            // physics body stays steady. The pre-grab constraints are restored on Exit (launch / drop / death).
            _cachedConstraints = _context.Rigidbody.constraints;
            _context.Rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            _context.Rigidbody.angularVelocity = Vector3.zero;

            // Interpolate ONLY while held: physics at 50Hz vs 60fps render makes an orbiting body stutter, and
            // interpolation smooths it. But it lets the rigidbody author the transform, which fights the NavMeshAgent
            // on a chasing enemy — safe here only because Enter disabled the agent, so Exit must restore it.
            _cachedInterpolation = _context.Rigidbody.interpolation;
            _context.Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

            _context.Rigidbody.WakeUp();
        }

        public override void Exit()
        {
            if (_context.Rigidbody == null)
                return;

            _context.Rigidbody.constraints = _cachedConstraints;
            _context.Rigidbody.interpolation = _cachedInterpolation;
        }

        public override void FixedTick()
        {
            if (!_context.TypeData.DamagesPlayerWhileHeld || _context.PlayerTarget == null || _context.IsDead)
                return;

            if (_context.Timers.HeldDamage.Remaining > 0f)
            {
                _context.Timers.HeldDamage.Set(_context.Timers.HeldDamage.Remaining - Time.fixedDeltaTime);
                return;
            }

            Vector3 offset = _context.PlayerTarget.transform.position - _context.Transform.position;
            offset.y = 0f;

            if (offset.sqrMagnitude > _context.Data.AttackRange * _context.Data.AttackRange)
                return;

            if (_context.PlayerTarget.TakeDamage(_context.Data.ContactDamage, _context.Transform.position))
                _context.Timers.HeldDamage.Set(_context.TypeData.HeldDamageInterval);
        }
    }
}