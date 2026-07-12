using Game.Common.StateMachine;
using Game.Enemy;
using UnityEngine;

namespace Game.Enemy.States
{
    /// <summary>
    ///     The pursue ("run") state: drive toward the target (NavMeshAgent when it can be placed on the mesh,
    ///     physics fallback otherwise). When the player is in range and the attack is off cooldown it hands off to
    ///     <see cref="EnemyAttackState" /> — the enemy only ever commits to an attack FROM the chase, so a swing is
    ///     never started mid-chase and the chase never runs while a swing plays. When the target is lost (the
    ///     player died) it hands off to <see cref="EnemyIdleState" />.
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
            {
                _context.ChangeToIdleState();
                return;
            }

            if (CanAttack())
            {
                _context.ChangeToAttackState();
                return;
            }

            DriveToTarget();
        }

        private bool CanAttack()
        {
            return _context.PlayerTarget != null
                   && _context.Timers.AttackCooldown.Remaining <= 0f
                   && InAttackRange();
        }

        private void DriveToTarget()
        {
            if (_context.TryEnableAgentControl())
            {
                _context.Movement.MoveToTarget(_context.Target);
            }
            else
            {
                _context.ApplyExtraGravity();
                _context.Movement.MoveDirectlyToTarget(_context.Target);
            }
        }

        private bool InAttackRange()
        {
            Vector3 offset = _context.PlayerTarget.transform.position - _context.Transform.position;
            offset.y = 0f;

            return offset.sqrMagnitude <= _context.Data.AttackRange * _context.Data.AttackRange;
        }
    }
}
