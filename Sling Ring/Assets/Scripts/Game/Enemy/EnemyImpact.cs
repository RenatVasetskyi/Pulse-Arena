using System;
using System.Collections.Generic;
using Data;
using Game.Enemy.Interfaces;
using UnityEngine;

namespace Game.Enemy
{
    /// <summary>
    ///     The "thrown enemy damages OTHER enemies" system: tracks who was already hit this flight,
    ///     sweeps along the trajectory, applies damage + knockback. The controller keeps the collision
    ///     callbacks, ground bounce and shared cooldown timer (those are coupled to physics recovery).
    /// </summary>
    public class EnemyImpact
    {
        private static readonly Collider[] OverlapBuffer = new Collider[32];
        private static readonly RaycastHit[] SweepBuffer = new RaycastHit[32];

        private readonly HashSet<EnemyController> _hitEnemies = new();
        private EnemyData _data;
        private Vector3 _lastPosition;
        private EnemyController _owner;
        private IEnemyRegistry _registry;
        private Rigidbody _rigidbody;
        private Transform _transform;
        private Func<EnemyTypeData> _type;

        public void Initialize(EnemyController owner, Transform transform, Rigidbody rigidbody,
            EnemyData data, Func<EnemyTypeData> typeProvider, IEnemyRegistry registry)
        {
            _owner = owner;
            _transform = transform;
            _rigidbody = rigidbody;
            _data = data;
            _type = typeProvider;
            _registry = registry;
        }

        public void Clear()
        {
            _hitEnemies.Clear();
        }

        /// <summary>Sweep along the current velocity. Returns true if it damaged any enemy this tick.</summary>
        public bool DamageDuringSweep()
        {
            if (!TryBuildSweepSegment(out Vector3 sweepStart, out Vector3 sweepEnd, out Vector3 currentPosition))
                return false;

            bool damagedBySweep = DamageAlongSweepCast(sweepStart, sweepEnd);
            bool damagedByOverlap = DamageAtSweepEndOverlap(sweepEnd);

            _lastPosition = currentPosition;
            return damagedBySweep || damagedByOverlap;
        }

        public void ResetSweepOrigin()
        {
            _lastPosition = _transform.position;
        }

        /// <summary>Collision-based hit. Returns true if it damaged another enemy.</summary>
        public bool TryDamageOnCollision(Collision collision)
        {
            EnemyController other = _registry.TryResolve(collision.rigidbody, out EnemyController resolved)
                ? resolved
                : null;
            return TryHit(other);
        }

        // The velocity gate + trajectory geometry: false (and parks _lastPosition) when too slow to damage.
        private bool TryBuildSweepSegment(out Vector3 sweepStart, out Vector3 sweepEnd, out Vector3 currentPosition)
        {
            currentPosition = _transform.position;

            if (_rigidbody.linearVelocity.magnitude < _data.ImpactDamageMinSpeed)
            {
                _lastPosition = currentPosition;
                sweepStart = default;
                sweepEnd = default;
                return false;
            }

            Vector3 velocity = _rigidbody.linearVelocity;
            sweepStart = _lastPosition;
            sweepEnd = currentPosition;

            if (velocity.sqrMagnitude > 0.001f)
                sweepEnd += velocity.normalized * _data.ImpactDamageForwardOffset;

            return true;
        }

        private bool DamageAlongSweepCast(Vector3 sweepStart, Vector3 sweepEnd)
        {
            Vector3 sweep = sweepEnd - sweepStart;

            if (sweep.sqrMagnitude <= 0.001f)
                return false;

            bool damaged = false;
            int sweepHitCount = Physics.SphereCastNonAlloc(sweepStart, _data.ImpactDamageRadius,
                sweep.normalized, SweepBuffer, sweep.magnitude, ~0, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < sweepHitCount; i++)
            {
                if (TryHit(ResolveEnemy(SweepBuffer[i].collider)))
                    damaged = true;
            }

            return damaged;
        }

        private bool DamageAtSweepEndOverlap(Vector3 sweepEnd)
        {
            bool damaged = false;
            int overlapCount = Physics.OverlapSphereNonAlloc(sweepEnd, _data.ImpactDamageRadius, OverlapBuffer,
                ~0, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < overlapCount; i++)
            {
                if (TryHit(ResolveEnemy(OverlapBuffer[i])))
                    damaged = true;
            }

            return damaged;
        }

        // O(1) map lookup instead of a per-hit GetComponent — this runs for every sweep/overlap collider
        // every FixedUpdate the enemy is a live projectile.
        private EnemyController ResolveEnemy(Collider hit)
        {
            return _registry.TryResolve(hit, out EnemyController enemy) ? enemy : null;
        }

        private bool TryHit(EnemyController other)
        {
            if (other == null || other == _owner || _hitEnemies.Contains(other))
                return false;

            ApplyKnockback(other);
            other.TakeDamage(GetImpactDamage());

            // Flag the target for the WHOLE flight (the set is cleared on the next throw) so a thrown enemy
            // damages each enemy exactly once — no rapid re-hits when it lingers in contact (e.g. pinning a
            // target against a wall).
            _hitEnemies.Add(other);
            return true;
        }

        private void ApplyKnockback(EnemyController other)
        {
            Vector3 direction = other.transform.position - _transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.001f)
                direction = _rigidbody.linearVelocity;

            direction.y = 0f;

            Vector3 knockbackDirection =
                (direction.normalized + Vector3.up * _data.ImpactKnockbackUpwardRatio).normalized;
            other.Knockback(knockbackDirection * (_data.ImpactKnockbackForce * _type().ImpactKnockbackMultiplier));
        }

        private int GetImpactDamage()
        {
            return Mathf.Max(1, Mathf.RoundToInt(_data.ImpactDamage * _type().ImpactDamageMultiplier));
        }
    }
}