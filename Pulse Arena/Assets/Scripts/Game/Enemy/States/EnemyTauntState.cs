using Game.Common.StateMachine;
using Game.Enemy;
using UnityEngine;

namespace Game.Enemy.States
{
    /// <summary>
    ///     The spawn taunt as its own state: on entry it HALTS the enemy (stands its ground) and plays the model's
    ///     spawn-taunt clip, then after the clip's duration hands off to <see cref="EnemyChaseState" />. Only entered
    ///     for models that have a taunt (<see cref="IEnemyVisual.HasSpawnTaunt" />); the rest spawn straight into the
    ///     chase. Freezes under mechanical pause for free (the controller's FixedUpdate early-returns).
    /// </summary>
    public class EnemyTauntState : ActorState
    {
        private readonly EnemyContext _context;
        private float _remaining;

        public EnemyTauntState(EnemyContext context)
        {
            _context = context;
        }

        public override void Enter()
        {
            _context.Movement.StopInPlace();
            _context.Visual?.PlaySpawnTaunt();
            _remaining = _context.Visual?.SpawnTauntDuration ?? 0f;
        }

        public override void FixedTick()
        {
            _context.Movement.StopInPlace(); // stand its ground for the whole taunt (idempotent)
            _remaining -= Time.fixedDeltaTime;

            if (_remaining <= 0f)
                _context.ChangeToChaseState();
        }
    }
}
