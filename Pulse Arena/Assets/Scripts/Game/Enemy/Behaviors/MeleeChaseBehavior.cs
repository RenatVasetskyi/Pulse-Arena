using UnityEngine;

namespace Game.Enemy.Behaviors
{
    /// <summary>
    ///     The default enemy brain (skeleton / wolf / karate and every stat-variant of them): run at the player via
    ///     <see cref="EnemyContext.DriveToTarget" />, commit to a melee swing when in range and off cooldown, and on
    ///     models that support it occasionally roll a mid-approach flourish. This is the concrete archetype the
    ///     <see cref="IEnemyBehavior" /> seam was extracted from — a new enemy (ranged, flyer, summoner, …) is a
    ///     sibling <see cref="IEnemyBehavior" /> in this folder, not an edit here.
    /// </summary>
    public class MeleeChaseBehavior : IEnemyBehavior
    {
        private EnemyContext _context;
        private EnemyFlourish _flourish;

        public void Initialize(EnemyContext context)
        {
            _context = context;
            _flourish = new EnemyFlourish(context);
        }

        public void OnEnterChase()
        {
            _flourish.Rearm();
        }

        public void Tick()
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
