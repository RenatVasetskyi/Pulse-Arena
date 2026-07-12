using Game.Common.StateMachine;
using Game.Enemy;
using UnityEngine;

namespace Game.Enemy.States
{
    /// <summary>
    ///     Terminal state: stops the enemy (disable the agent, clear the shared flags + knockback/stasis timers,
    ///     freeze the rigidbody), plays the death clip, then triggers the pool-return. The pool-return coroutine
    ///     itself must live on the controller MonoBehaviour, so <see cref="EnemyContext.StartDeathReturn" /> is a
    ///     thin one-liner into it; everything else runs here through the context.
    /// </summary>
    public class EnemyDeadState : ActorState
    {
        private readonly EnemyContext _context;

        public EnemyDeadState(EnemyContext context)
        {
            _context = context;
        }

        public override void Enter()
        {
            StopForDeath();
            _context.Visual?.PlayDeath();
            _context.StartDeathReturn();
        }

        private void StopForDeath()
        {
            _context.Movement.DisableAgent();
            _context.ClearFlags();
            _context.Timers.Knockback.Clear();
            _context.Timers.Stasis.Clear();

            Rigidbody rigidbody = _context.Rigidbody;

            if (rigidbody == null)
                return;

            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            rigidbody.isKinematic = true;
        }
    }
}
