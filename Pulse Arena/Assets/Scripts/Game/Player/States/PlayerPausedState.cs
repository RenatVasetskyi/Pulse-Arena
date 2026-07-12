using Game.Common.StateMachine;
using UnityEngine;

namespace Game.Player.States
{
    /// <summary>
    ///     Mechanical pause, overlaid on the running state via
    ///     <see cref="Game.Common.StateMachine.ActorStateMachine.Pause" />: Enter caches + freezes the Rigidbody
    ///     (kinematic, so PhysX stops integrating) and gates the visual; Exit restores them exactly. It ticks
    ///     nothing, so the suspended state keeps its fields + delta-timers and the game resumes from the same
    ///     frame + sample.
    /// </summary>
    public class PlayerPausedState : ActorState
    {
        private readonly PlayerContext _context;
        private Vector3 _cachedAngularVelocity;
        private bool _cachedIsKinematic;
        private Vector3 _cachedLinearVelocity;

        public PlayerPausedState(PlayerContext context)
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
                rigidbody.isKinematic = true;
            }

            _context.Visual?.SetPaused(true);
        }

        public override void Exit()
        {
            Rigidbody rigidbody = _context.Rigidbody;

            // Restore the body BEFORE un-gating the visual — the visual's move-blend reads linearVelocity.
            if (rigidbody != null)
            {
                rigidbody.isKinematic = _cachedIsKinematic;
                rigidbody.linearVelocity = _cachedLinearVelocity;
                rigidbody.angularVelocity = _cachedAngularVelocity;
                rigidbody.WakeUp();
            }

            _context.Visual?.SetPaused(false);
        }
    }
}
