using Data;
using Game.Common;
using UnityEngine;

namespace Game.Enemy
{
    /// <summary>
    /// The pure-physics side of an enemy landing after being flung: remembering that it touched the
    /// ground recently, deciding when it may stop recovering, and the actual snap-to-ground when it
    /// does. Carries the old MarkGroundContact / CanFinishPhysicsRecovery / FinishPhysicsRecovery bodies
    /// verbatim.
    ///
    /// It owns NO game state — the recovery/projectile flags and the thrown visual still flip on the
    /// controller (ContextOnRecoveryFinished). The ground-contact memory and the recovery elapsed
    /// counter live in <see cref="EnemyTimers"/>, which this class is handed a reference to; it reads
    /// and writes them so the numbers stay identical to the old controller fields.
    /// </summary>
    public sealed class GroundRecoveryController
    {
        private Transform _transform;
        private Rigidbody _rigidbody;
        private EnemyData _data;
        private EnemyTimers _timers;

        public void Initialize(Transform transform, Rigidbody rigidbody, EnemyData data, EnemyTimers timers)
        {
            _transform = transform;
            _rigidbody = rigidbody;
            _data = data;
            _timers = timers;
        }

        /// <summary>Arms the ground-contact memory. Old body: "_groundContactTimer = _data.GroundContactMemory".</summary>
        public void MarkGroundContact()
        {
            _timers.GroundContact.Set(_data.GroundContactMemory);
        }

        /// <summary>
        /// True once the enemy has touched ground recently AND can snap onto it. Old CanFinishPhysicsRecovery:
        /// a null rigidbody finishes immediately, no recent ground contact never finishes, otherwise it must
        /// find physical ground to snap to.
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
        /// The physics half of the old FinishPhysicsRecovery: zero the recovery counter, flatten vertical
        /// velocity, kill spin, and snap to the ground. The recovery / projectile flags and the thrown
        /// visual are flipped by the caller (ContextOnRecoveryFinished) — this method is pure physics.
        /// </summary>
        public void Finish()
        {
            _timers.PhysicsRecoveryElapsed = 0f;

            if (_rigidbody == null)
                return;

            Vector3 velocity = _rigidbody.linearVelocity;
            _rigidbody.linearVelocity = new Vector3(velocity.x, 0f, velocity.z);
            _rigidbody.angularVelocity = Vector3.zero;
            ActorGroundingUtility.SnapToGround(_transform, _data.GroundRecoveryProbeDistance);
        }

        private bool TrySnapToPhysicalGround()
        {
            return ActorGroundingUtility.SnapToGround(_transform, _data.GroundRecoveryProbeDistance);
        }
    }
}
