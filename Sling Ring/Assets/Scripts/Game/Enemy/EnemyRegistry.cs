using System.Collections.Generic;
using Game.Enemy.Interfaces;
using UnityEngine;

namespace Game.Enemy
{
    /// <summary>
    ///     O(1) reverse lookup from a physics body/collider to its <see cref="EnemyController" />. Populated by
    ///     each enemy's pool lifecycle (register on spawn, unregister on pool-return / destroy) so the hot impact
    ///     sweep resolves targets from a dictionary instead of a per-hit <c>GetComponent</c> every FixedUpdate.
    ///     Only live enemies are ever in the map, so a resolved controller is always a valid target. Scene-scoped:
    ///     a fresh registry per match, discarded with the SceneContext.
    /// </summary>
    public sealed class EnemyRegistry : IEnemyRegistry
    {
        private readonly Dictionary<Rigidbody, EnemyController> _byBody = new();

        public void Register(Rigidbody body, EnemyController enemy)
        {
            if (body == null || enemy == null)
                return;

            _byBody[body] = enemy;
        }

        public bool TryResolve(Rigidbody body, out EnemyController enemy)
        {
            if (body != null)
                return _byBody.TryGetValue(body, out enemy);

            enemy = null;
            return false;
        }

        public bool TryResolve(Collider collider, out EnemyController enemy)
        {
            if (collider != null)
                return TryResolve(collider.attachedRigidbody, out enemy);

            enemy = null;
            return false;
        }

        public void Unregister(Rigidbody body)
        {
            if (body == null)
                return;

            _byBody.Remove(body);
        }
    }
}