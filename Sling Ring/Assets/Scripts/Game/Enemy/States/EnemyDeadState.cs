using Game.Common.StateMachine;
using Game.Enemy;
using UnityEngine;

namespace Game.Enemy.States
{
    /// <summary>
    ///     Terminal state: stops the enemy (disable agent, clear flags + knockback/stasis timers), plays the death
    ///     clip, then triggers the pool-return. Does NOT freeze the body — an enemy killed mid-flight must DROP to
    ///     the ground (left non-kinematic under gravity, only horizontal drift + spin killed) so the death clip plays
    ///     on the floor, not hanging in the air. The pool-return coroutine lives on the controller via
    ///     <see cref="EnemyContext.StartDeathReturn" />.
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

        // Keep the corpse falling under extra gravity so a high mid-air kill drops promptly to the floor; once it
        // rests on its capsule this just presses down.
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

            // Kill horizontal drift + spin but keep it non-kinematic under gravity so a mid-air death lands on the
            // ground. FreezeRotation pins the body upright (the player can't spin the corpse); the skinned death
            // anim still plays — only the rigidbody's physical rotation is locked.
            rigidbody.angularVelocity = Vector3.zero;
            rigidbody.linearVelocity = new Vector3(0f, Mathf.Min(0f, rigidbody.linearVelocity.y), 0f);
            rigidbody.useGravity = true;
            rigidbody.isKinematic = false;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }
}
