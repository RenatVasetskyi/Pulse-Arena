using Data;
using Game.Combat.Interfaces;
using Game.Enemy;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    ///     Finds the nearest grabbable enemy around the slingshot origin: an <see cref="Physics.OverlapSphereNonAlloc" />
    ///     query on the enemy layer, filtered by grabbed/dead state and a line-of-sight linecast against obstacles,
    ///     returning the closest survivor. Shared by both the grab attempt and the target-marker highlight.
    /// </summary>
    public class EnemyTargetFinder : IEnemyTargetFinder
    {
        private static readonly Collider[] Buffer = new Collider[32];
        private SlingshotData _data;

        private Transform _origin;

        public void Initialize(Transform origin, SlingshotData data)
        {
            _origin = origin;
            _data = data;
        }

        public EnemyController FindNearest(float radius)
        {
            int hitCount = Physics.OverlapSphereNonAlloc(_origin.position, radius, Buffer, _data.EnemyLayer);
            EnemyController nearestEnemy = null;
            float nearestSqrDistance = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                Rigidbody hitBody = Buffer[i].attachedRigidbody;

                if (hitBody == null || !hitBody.TryGetComponent(out EnemyController enemy))
                    continue;

                if (!enemy.IsTargetable)
                    continue;

                if (!HasLineOfSight(enemy))
                    continue;

                float sqrDistance = (enemy.transform.position - _origin.position).sqrMagnitude;

                if (sqrDistance >= nearestSqrDistance)
                    continue;

                nearestSqrDistance = sqrDistance;
                nearestEnemy = enemy;
            }

            return nearestEnemy;
        }

        private bool HasLineOfSight(EnemyController enemy)
        {
            if (_data.ObstacleLayer.value == 0)
                return true;

            const float height = 1f;
            Vector3 from = _origin.position + Vector3.up * height;
            Vector3 to = enemy.transform.position + Vector3.up * height;
            return !Physics.Linecast(from, to, _data.ObstacleLayer, QueryTriggerInteraction.Ignore);
        }
    }
}