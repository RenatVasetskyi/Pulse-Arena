using Game.Common.StateMachine;
using Game.Enemy;
using UnityEngine;

namespace Game.Enemy.States
{
    /// <summary>
    ///     The mid-run somersault as its own state: plays the model's flip clip while still DRIVING toward the target
    ///     (the flourish happens while advancing), then hands back to <see cref="EnemyChaseState" />. It still yields —
    ///     an in-range target off cooldown cuts to <see cref="EnemyAttackState" />, a lost target drops to
    ///     <see cref="EnemyIdleState" />. Entered from the chase when <see cref="EnemyFlourish" /> rolls a flip.
    /// </summary>
    public class EnemyFlipState : ActorState
    {
        private readonly EnemyContext _context;
        private float _remaining;

        public EnemyFlipState(EnemyContext context)
        {
            _context = context;
        }

        public override void Enter()
        {
            _context.Visual?.PlayRunFlourish();
            _remaining = _context.Visual?.FlourishDuration ?? 0f;
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

            _remaining -= Time.fixedDeltaTime;

            if (_remaining <= 0f)
            {
                _context.ChangeToChaseState();
                return;
            }

            _context.DriveToTarget();
        }
    }
}
