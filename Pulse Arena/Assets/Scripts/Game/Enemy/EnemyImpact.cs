using System;
using System.Collections.Generic;
using Data;
using UnityEngine;

namespace Game.Enemy
{
    /// <summary>
    /// The "thrown enemy damages OTHER enemies" system: tracks who was already hit this flight,
    /// sweeps along the trajectory, applies damage + knockback. The controller keeps the collision
    /// callbacks, ground bounce and shared cooldown timer (those are coupled to physics recovery).
    /// </summary>
    public class EnemyImpact
    {
        private static readonly RaycastHit[] SweepBuffer = new RaycastHit[32];
        private static readonly Collider[] OverlapBuffer = new Collider[32];

        private readonly Dictionary<EnemyController, float> _hitTimers = new();
        private EnemyController _owner;
        private Transform _transform;
        private Rigidbody _rigidbody;
        private EnemyData _data;
        private Func<EnemyTypeData> _type;
        private Vector3 _lastPosition;

        public void Initialize(EnemyController owner, Transform transform, Rigidbody rigidbody,
            EnemyData data, Func<EnemyTypeData> typeProvider)
        {
            _owner = owner;
            _transform = transform;
            _rigidbody = rigidbody;
            _data = data;
            _type = typeProvider;
        }

        public void ResetSweepOrigin()
        {
            _lastPosition = _transform.position;
        }

        public void Clear()
        {
            _hitTimers.Clear();
        }

        public void Tick(float deltaTime)
        {
            if (_hitTimers.Count == 0)
                return;

            List<EnemyController> enemies = new(_hitTimers.Keys);

            foreach (EnemyController enemy in enemies)
            {
                if (enemy == null)
                {
                    _hitTimers.Remove(enemy);
                    continue;
                }

                _hitTimers[enemy] -= deltaTime;

                if (_hitTimers[enemy] <= 0f)
                    _hitTimers.Remove(enemy);
            }
        }

        /// <summary>Collision-based hit. Returns true if it damaged another enemy.</summary>
        public bool TryDamageOnCollision(Collision collision)
        {
            Rigidbody otherBody = collision.rigidbody;
            EnemyController other = otherBody != null ? otherBody.GetComponent<EnemyController>() : null;
            return TryHit(other);
        }

        /// <summary>Sweep along the current velocity. Returns true if it damaged any enemy this tick.</summary>
        public bool DamageDuringSweep()
        {
            if (_rigidbody.linearVelocity.magnitude < _data.ImpactDamageMinSpeed)
            {
                _lastPosition = _transform.position;
                return false;
            }

            Vector3 velocity = _rigidbody.linearVelocity;
            Vector3 currentPosition = _transform.position;
            Vector3 sweepStart = _lastPosition;
            Vector3 sweepEnd = currentPosition;

            if (velocity.sqrMagnitude > 0.001f)
                sweepEnd += velocity.normalized * _data.ImpactDamageForwardOffset;

            Vector3 sweep = sweepEnd - sweepStart;
            bool damaged = false;

            if (sweep.sqrMagnitude > 0.001f)
            {
                int sweepHitCount = Physics.SphereCastNonAlloc(sweepStart, _data.ImpactDamageRadius,
                    sweep.normalized, SweepBuffer, sweep.magnitude, ~0, QueryTriggerInteraction.Ignore);

                for (int i = 0; i < sweepHitCount; i++)
                {
                    if (TryHit(ResolveEnemy(SweepBuffer[i].collider)))
                        damaged = true;
                }
            }

            int overlapCount = Physics.OverlapSphereNonAlloc(sweepEnd, _data.ImpactDamageRadius, OverlapBuffer,
                ~0, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < overlapCount; i++)
            {
                if (TryHit(ResolveEnemy(OverlapBuffer[i])))
                    damaged = true;
            }

            _lastPosition = currentPosition;
            return damaged;
        }

        private static EnemyController ResolveEnemy(Collider hit)
        {
            Rigidbody body = hit.attachedRigidbody;
            return body != null ? body.GetComponent<EnemyController>() : null;
        }

        private bool TryHit(EnemyController other)
        {
            if (other == null || other == _owner || _hitTimers.ContainsKey(other))
                return false;

            ApplyKnockback(other);
            other.TakeDamage(GetImpactDamage());

            // Flag the target for the WHOLE flight (the set is cleared on the next throw) so a
            // thrown enemy damages each enemy exactly once — no rapid re-hits every cooldown when
            // it lingers in contact (e.g. pinning a target against a wall).
            _hitTimers[other] = float.MaxValue;
            return true;
        }

        private void ApplyKnockback(EnemyController other)
        {
            Vector3 direction = other.transform.position - _transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.001f)
                direction = _rigidbody.linearVelocity;

            direction.y = 0f;

            Vector3 knockbackDirection = (direction.normalized + Vector3.up * _data.ImpactKnockbackUpwardRatio).normalized;
            other.Knockback(knockbackDirection * (_data.ImpactKnockbackForce * _type().ImpactKnockbackMultiplier));
        }

        private int GetImpactDamage()
        {
            return Mathf.Max(1, Mathf.RoundToInt(_data.ImpactDamage * _type().ImpactDamageMultiplier));
        }
    }
}
