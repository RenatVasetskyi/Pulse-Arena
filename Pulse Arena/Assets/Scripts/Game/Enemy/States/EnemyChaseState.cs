using Game.Common.StateMachine;
using Game.Enemy;
using UnityEngine;

namespace Game.Enemy.States
{
    /// <summary>
    ///     The pursue ("run") state: drive toward the target (via <see cref="EnemyContext.DriveToTarget" />). When the
    ///     player is in range and the attack is off cooldown it hands off to <see cref="EnemyAttackState" /> — the
    ///     enemy only ever commits to an attack FROM the chase, so a swing is never started mid-chase and the chase
    ///     never runs while a swing plays. On models that support it, <see cref="EnemyFlourish" /> occasionally rolls
    ///     a somersault mid-approach and hands off to <see cref="EnemyFlipState" />. When the target is lost (the
    ///     player died) it drops to <see cref="EnemyIdleState" />.
    /// </summary>
    public class EnemyChaseState : ActorState
    {
        private readonly EnemyContext _context;
        private readonly EnemyFlourish _flourish;

        public EnemyChaseState(EnemyContext context)
        {
            _context = context;
            _flourish = new EnemyFlourish(context);
        }

        public override void Enter()
        {
            _context.IsGrabbed = false;
            _context.NeedsGroundRecovery = false;
            _context.IsImpactProjectile = false;
            _context.Visual?.SetGrabbed(false);
            _context.Visual?.SetThrown(false);
            _context.TryEnableAgentControl();
            _flourish.Rearm();
        }

        public override void FixedTick()
        {
            if (_context.Target == null)
            {
                _context.ChangeToIdleState();
                return;
            }

            if (_context.CanAttack())
            {
                _context.ChangeToAttackState();
                return;
            }

            if (_flourish.ShouldFlip(Time.fixedDeltaTime))
            {
                _context.ChangeToFlipState();
                return;
            }

            _context.DriveToTarget();
        }
    }
}
