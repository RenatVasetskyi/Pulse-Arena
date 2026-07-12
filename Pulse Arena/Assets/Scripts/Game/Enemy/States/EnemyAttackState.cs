using Game.Common.StateMachine;
using Game.Enemy;
using UnityEngine;

namespace Game.Enemy.States
{
    /// <summary>
    ///     A committed melee swing as its own state: on entry it HALTS the enemy (no chasing while it swings —
    ///     the whole reason this is a state, not a timer inside <see cref="EnemyChaseState" />), snaps to face the
    ///     player, and plays the model's attack clip. Damage lands once at the model's contact frame
    ///     (<c>AttackHitDelay</c>, re-checking range so the player can dodge during the wind-up), then a short
    ///     recovery tail plays before it returns to the chase. On exit it tells the visual to leave the attack
    ///     clip (<see cref="IEnemyVisual.EndAttack" />) so the Animator never lingers mid-swing once the chase
    ///     resumes. Freezes under mechanical pause for free (the controller's FixedUpdate early-returns).
    /// </summary>
    public class EnemyAttackState : ActorState
    {
        private readonly EnemyContext _context;
        private float _duration;
        private float _elapsed;
        private float _hitDelay;
        private bool _struck;

        public EnemyAttackState(EnemyContext context)
        {
            _context = context;
        }

        public override void Enter()
        {
            _context.Movement.StopInPlace();
            FaceTarget();
            _context.Visual?.PlayAttack();
            _context.Timers.AttackCooldown.Set(_context.Data.AttackCooldown);

            _hitDelay = ResolveHitDelay();
            _duration = _hitDelay + _context.Data.AttackRecovery;
            _elapsed = 0f;
            _struck = false;
        }

        public override void FixedTick()
        {
            _context.Movement.StopInPlace(); // hold position for the whole swing (idempotent)
            _elapsed += Time.fixedDeltaTime;

            if (!_struck && _elapsed >= _hitDelay)
                Strike();

            if (_elapsed >= _duration)
                _context.ChangeToChaseState();
        }

        public override void Exit()
        {
            _context.Visual?.EndAttack();
        }

        // The strike connects only if the player is still in range — dodging out during the wind-up whiffs it.
        private void Strike()
        {
            _struck = true;

            if (_context.PlayerTarget != null && InAttackRange())
                _context.PlayerTarget.TakeDamage(_context.Data.ContactDamage, _context.Transform.position);
        }

        // The active model's own strike moment (its swing's contact frame) wins so damage syncs to what's visible;
        // a model with no opinion (negative) falls back to the shared gameplay default.
        private float ResolveHitDelay()
        {
            float visualDelay = _context.Visual?.AttackHitDelay ?? -1f;

            return visualDelay >= 0f ? visualDelay : _context.Data.AttackHitDelay;
        }

        private void FaceTarget()
        {
            if (_context.PlayerTarget != null)
                _context.Movement.FaceTarget(_context.PlayerTarget.transform);
        }

        private bool InAttackRange()
        {
            Vector3 offset = _context.PlayerTarget.transform.position - _context.Transform.position;
            offset.y = 0f;

            return offset.sqrMagnitude <= _context.Data.AttackRange * _context.Data.AttackRange;
        }
    }
}
