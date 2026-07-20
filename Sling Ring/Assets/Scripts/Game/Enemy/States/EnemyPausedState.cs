using Game.Common.StateMachine;
using UnityEngine;

namespace Game.Enemy.States
{
    /// <summary>
    ///     Mechanical pause, overlaid on the running state via
    ///     <see cref="Game.Common.StateMachine.ActorStateMachine.Pause" />: Enter caches + freezes the Rigidbody
    ///     (velocity + gravity + kinematic, so PhysX stops), halts the NavMeshAgent, gates the visual and pauses
    ///     the ringout effect; Exit restores them exactly. It ticks nothing, so the suspended state keeps its exact
    ///     frame + countdowns (the controller's ambient tick is gated on the machine's paused flag, so it freezes
    ///     too) — the game resumes from the same frame + sample.
    /// </summary>
    public class EnemyPausedState : ActorState
    {
        private readonly EnemyContext _context;
        private Vector3 _cachedAngularVelocity;
        private bool _cachedIsKinematic;
        private Vector3 _cachedLinearVelocity;
        private bool _cachedUseGravity;

        public EnemyPausedState(EnemyContext context)
        {
            _context = context;
        }

        public override void Enter()
        {
            Rigidbody rigidbody = _context.Rigidbody;

            if (rigidbody != null)
            {
                _cachedLinearVelocity = rigidbody.linearVelocity;
                _cachedAngularVelocity = rigidbody.angularVelocity;
                _cachedIsKinematic = rigidbody.isKinematic;
                _cachedUseGravity = rigidbody.useGravity;
                rigidbody.linearVelocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
                rigidbody.isKinematic = true;
            }

            _context.Movement.Pause();
            _context.Visual?.SetPaused(true);
            _context.Ringout.PauseEffect();
        }

        public override void Exit()
        {
            _context.Visual?.SetPaused(false);
            _context.Ringout.ResumeEffect();
            _context.Movement.Resume();

            Rigidbody rigidbody = _context.Rigidbody;

            if (rigidbody != null)
            {
                rigidbody.isKinematic = _cachedIsKinematic;
                rigidbody.useGravity = _cachedUseGravity;
                rigidbody.linearVelocity = _cachedLinearVelocity;
                rigidbody.angularVelocity = _cachedAngularVelocity;

                if (!rigidbody.isKinematic)
                    rigidbody.WakeUp();
            }
        }
    }
}
