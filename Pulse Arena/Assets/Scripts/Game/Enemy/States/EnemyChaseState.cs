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
        private float _attackWindup;

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
            _attackWindup = 0f;
        }

        public override void FixedTick()
        {
            if (_context.Target == null)
            {
                _context.Movement.StopInPlace();
                return;
            }

            if (_context.TryEnableAgentControl())
            {
                _context.Movement.MoveToTarget(_context.Target);
            }
            else
            {
                _context.ApplyExtraGravity();
                _context.Movement.MoveDirectlyToTarget(_context.Target);
            }

            TickAttack();
        }

        // Two-phase melee: start the lunge telegraph now, land the damage at the middle of the lunge
        // (AttackHitDelay later) — so the hit reads as the pounce connecting, not before the animation.
        private void TickAttack()
        {
            if (_attackWindup > 0f)
            {
                _attackWindup -= Time.fixedDeltaTime;

                if (_attackWindup <= 0f)
                    StrikeTarget();

                return;
            }

            TryStartAttack();
        }

        private void TryStartAttack()
        {
            if (_context.PlayerTarget == null || _context.Timers.AttackCooldown.Remaining > 0f || !InAttackRange())
                return;

            _context.Visual?.PlayAttack();
            _context.Timers.AttackCooldown.Set(_context.Data.AttackCooldown);
            _attackWindup = Mathf.Max(0.01f, _context.Data.AttackHitDelay);
        }

        // The strike lands only if the player is still in range — they can dodge during the wind-up.
        private void StrikeTarget()
        {
            if (_context.PlayerTarget != null && InAttackRange())
                _context.PlayerTarget.TakeDamage(_context.Data.ContactDamage, _context.Transform.position);
        }

        private bool InAttackRange()
        {
            if (_context.PlayerTarget == null)
                return false;

            Vector3 offset = _context.PlayerTarget.transform.position - _context.Transform.position;
            offset.y = 0f;

            return offset.sqrMagnitude <= _context.Data.AttackRange * _context.Data.AttackRange;
        }
    }
}