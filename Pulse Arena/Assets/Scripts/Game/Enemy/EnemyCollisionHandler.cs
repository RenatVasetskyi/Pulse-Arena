using Data;
using Game.Common;
using Game.Player;
using UnityEngine;

namespace Game.Enemy
{
    /// <summary>
    ///     The enemy's collision decision tree, lifted off the controller verbatim: ground-contact memory,
    ///     the "thrown projectile damages enemies / walls" branch, the ground-bounce arc (and its counter),
    ///     and the trajectory sweep. The controller KEEPS the Unity <c>OnCollisionEnter</c>/<c>OnCollisionStay</c>
    ///     magic methods as one-line forwarders into this class — they must stay on the MonoBehaviour or
    ///     Unity stops delivering collisions with no compile error.
    ///     Damage is applied by calling back into <c>owner.TakeDamage</c> (the controller still owns health,
    ///     scoring and death). Timers are read/written through <see cref="EnemyTimers" /> so the numbers match
    ///     the old fields exactly; ground contact is remembered via <see cref="GroundRecoveryController" />.
    /// </summary>
    public sealed class EnemyCollisionHandler
    {
        private EnemyData _data;
        private int _groundBounceCount;
        private GroundRecoveryController _groundRecovery;
        private EnemyImpact _impact;
        private EnemyController _owner;
        private Rigidbody _rigidbody;
        private EnemyTimers _timers;
        private Transform _transform;

        public void Initialize(EnemyController owner, Transform transform, Rigidbody rigidbody,
            EnemyData data, EnemyImpact impact, EnemyTimers timers, GroundRecoveryController groundRecovery)
        {
            _owner = owner;
            _transform = transform;
            _rigidbody = rigidbody;
            _data = data;
            _impact = impact;
            _timers = timers;
            _groundRecovery = groundRecovery;
        }

        public void OnCollisionEnter(Collision collision, bool isImpactProjectile, bool isDead)
        {
            bool isGroundCollision = IsGroundCollision(collision);

            if (isGroundCollision)
                _groundRecovery.MarkGroundContact();

            if (isDead || !isImpactProjectile || _timers.Knockback.Remaining <= 0f)
                return;

            Rigidbody collisionBody = collision.rigidbody;

            if (collisionBody != null && collisionBody.TryGetComponent<PlayerController>(out _))
                return;

            if (isGroundCollision)
            {
                TryBounceFromGround();
                return;
            }

            if (_timers.ImpactDamageCooldown.Remaining > 0f)
                return;

            bool hitEnemy = collisionBody != null && collisionBody.TryGetComponent<EnemyController>(out _);

            if (hitEnemy)
            {
                if (_rigidbody.linearVelocity.magnitude < _data.ImpactDamageMinSpeed)
                    return;

                if (!_impact.TryDamageOnCollision(collision))
                    return;

                _timers.ImpactDamageCooldown.Set(_data.ImpactDamageCooldown);
                _owner.TakeDamage(_data.ImpactDamage);
                return;
            }

            if (collision.relativeVelocity.magnitude < _data.ImpactDamageMinSpeed)
                return;

            _timers.ImpactDamageCooldown.Set(_data.ImpactDamageCooldown);
            _owner.TakeDamage(Mathf.Max(1, _data.WallImpactDamage));
        }

        public void OnCollisionStay(Collision collision)
        {
            if (IsGroundCollision(collision))
                _groundRecovery.MarkGroundContact();
        }

        /// <summary>Resets the ground-bounce counter for a fresh spawn / pool reuse.</summary>
        public void ResetForSpawn()
        {
            _groundBounceCount = 0;
        }

        /// <summary>Called by Launch to re-arm a fresh flight (old body zeroed _groundBounceCount).</summary>
        public void ResetGroundBounce()
        {
            _groundBounceCount = 0;
        }

        /// <summary>The sweep-along-trajectory damage tick (old SweepImpactDamage). Runs from the recovery/knockback states.</summary>
        public void SweepImpactDamage()
        {
            if (!_impact.DamageDuringSweep())
                return;

            _timers.ImpactDamageCooldown.Set(_data.ImpactDamageCooldown);
            _owner.TakeDamage(_data.ImpactDamage);
        }

        private void TryBounceFromGround()
        {
            if (_timers.GroundBounceCooldown.Remaining > 0f || _groundBounceCount >= _data.GroundBounceCount)
                return;

            Vector3 velocity = _rigidbody.linearVelocity;
            Vector3 planarVelocity = new(velocity.x, 0f, velocity.z);

            if (Mathf.Abs(velocity.y) < _data.GroundBounceMinVerticalSpeed &&
                planarVelocity.magnitude < _data.ImpactDamageMinSpeed)
                return;

            _groundBounceCount++;
            _timers.GroundBounceCooldown.Set(_data.GroundBounceCooldown);
            _timers.Knockback.SetMax(_data.GroundBounceKeepAliveDuration);

            Vector3 bounceVelocity = planarVelocity * _data.GroundBounceHorizontalDamping;
            bounceVelocity.y = _data.GroundBounceUpwardVelocity;

            _rigidbody.linearVelocity = bounceVelocity;
            _owner.Visual?.PlayGroundBounce();
        }

        private bool IsGroundCollision(Collision collision)
        {
            foreach (ContactPoint contact in collision.contacts)
            {
                if (contact.normal.y > _data.GroundBounceNormalThreshold &&
                    ActorGroundingUtility.IsGroundCollider(contact.otherCollider))
                {
                    return true;
                }
            }

            return false;
        }
    }
}