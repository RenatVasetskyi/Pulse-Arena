using Data;
using Game.Common;
using UnityEngine;

namespace Game.Enemy
{
    /// <summary>
    ///     The pure-physics side of an enemy landing after being flung: remembering recent ground contact,
    ///     deciding when it may stop recovering, and the snap-to-ground when it does. Owns no game state — the
    ///     recovery/projectile flags and thrown visual flip on the controller. The ground-contact memory and
    ///     recovery-elapsed counter live in <see cref="EnemyTimers" />, which this reads and writes.
    /// </summary>
    public sealed class GroundRecoveryController
    {
        private EnemyData _data;
        private GroundingData _grounding;
        private Rigidbody _rigidbody;
        private EnemyTimers _timers;
        private Transform _transform;

        public void Initialize(Transform transform, Rigidbody rigidbody, EnemyData data, EnemyTimers timers,
            GroundingData grounding)
        {
            _transform = transform;
            _rigidbody = rigidbody;
            _data = data;
            _timers = timers;
            _grounding = grounding;
        }

        /// <summary>
        ///     True once the enemy has touched ground recently AND can snap onto it: a null rigidbody finishes
        ///     immediately, no recent ground contact never finishes, otherwise it must find physical ground to
        ///     snap to.
        /// </summary>
        public bool CanFinish()
        {
            if (_rigidbody == null)
                return true;

            if (_timers.GroundContact.Remaining <= 0f)
                return false;

            return TrySnapToPhysicalGround();
        }

        /// <summary>
        ///     Zero the recovery counter, flatten vertical velocity, kill spin, and snap to the ground. The
        ///     recovery / projectile flags and the thrown visual are flipped by the caller — this is pure physics.
        /// </summary>
        public void Finish()
        {
            _timers.PhysicsRecoveryElapsed = 0f;

            if (_rigidbody == null)
                return;

            Vector3 velocity = _rigidbody.linearVelocity;
            _rigidbody.linearVelocity = new Vector3(velocity.x, 0f, velocity.z);
            _rigidbody.angularVelocity = Vector3.zero;
            ActorGroundingUtility.SnapToGround(_transform, _grounding, _data.GroundRecoveryProbeDistance);
        }

        /// <summary>Arms the ground-contact memory window.</summary>
        public void MarkGroundContact()
        {
            _timers.GroundContact.Set(_data.GroundContactMemory);
        }

        private bool TrySnapToPhysicalGround()
        {
            return ActorGroundingUtility.SnapToGround(_transform, _grounding, _data.GroundRecoveryProbeDistance);
        }
    }
}