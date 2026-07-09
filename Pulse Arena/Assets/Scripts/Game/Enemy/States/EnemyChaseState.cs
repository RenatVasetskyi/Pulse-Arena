using Game.Common.StateMachine;
using Game.Enemy;
using UnityEngine;

namespace Game.Enemy.States
{
    /// <summary>
    ///     The default hunting state: drive toward the target (NavMeshAgent when it can be placed on the
    ///     mesh, physics fallback otherwise) and melee the player when in range. Carries the old
    ///     EnterChaseState / FixedTickChaseState / TryAttackTarget logic that used to live on the controller.
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
        }

        public override void FixedTick()
        {
            if (_context.Target == null)
                return;

            if (_context.TryEnableAgentControl())
            {
                _context.Movement.MoveToTarget(_context.Target);
            }
            else
            {
                _context.ApplyExtraGravity();
                _context.Movement.MoveDirectlyToTarget(_context.Target);
            }

            TryAttackTarget();
        }

        private void TryAttackTarget()
        {
            if (_context.PlayerTarget == null || _context.Timers.AttackCooldown.Remaining > 0f)
                return;

            Vector3 offset = _context.PlayerTarget.transform.position - _context.Transform.position;
            offset.y = 0f;

            if (offset.sqrMagnitude > _context.Data.AttackRange * _context.Data.AttackRange)
                return;

            if (_context.PlayerTarget.TakeDamage(_context.Data.ContactDamage, _context.Transform.position))
                _context.Timers.AttackCooldown.Set(_context.Data.AttackCooldown);
        }
    }
}