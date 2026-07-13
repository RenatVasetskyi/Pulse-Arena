using Game.Common.StateMachine;
using Game.Enemy;
using UnityEngine;

namespace Game.Enemy.States
{
    /// <summary>
    ///     Terminal state: stops the enemy (disable the agent, clear the shared flags + knockback/stasis timers),
    ///     plays the death clip, then triggers the pool-return. Crucially it does NOT freeze the body in place — an
    ///     enemy killed mid-flight (thrown then dies) must DROP to the ground so the death clip plays on the floor,
    ///     not hang in the air; so the corpse is left non-kinematic under gravity with only its horizontal drift +
    ///     spin killed (it falls straight down, upright, and the capsule settles on the floor). The pool-return
    ///     coroutine must live on the controller MonoBehaviour, so <see cref="EnemyContext.StartDeathReturn" /> is a
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

        // Keep the corpse falling under gravity every physics step so a high mid-air kill drops promptly to the
        // floor (the extra gravity matches the throw arc's feel); once it rests on its capsule this just presses down.
        public override void FixedTick()
        {
            _context.ApplyExtraGravity();
        }

        private void StopForDeath()
        {
            _context.Movement.DisableAgent(); // leaves the rigidbody non-kinematic so it can still fall
            _context.ClearFlags();
            _context.Timers.Knockback.Clear();
            _context.Timers.Stasis.Clear();

            Rigidbody rigidbody = _context.Rigidbody;

            if (rigidbody == null)
                return;

            // Drop straight down: kill horizontal drift + spin (no fly-off, no roll) but keep it non-kinematic
            // under gravity + upright so a mid-air death lands on the ground instead of freezing in the air.
            rigidbody.angularVelocity = Vector3.zero;
            rigidbody.linearVelocity = new Vector3(0f, Mathf.Min(0f, rigidbody.linearVelocity.y), 0f);
            rigidbody.useGravity = true;
            rigidbody.isKinematic = false;
        }
    }
}
