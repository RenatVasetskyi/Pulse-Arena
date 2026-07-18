using UnityEngine;

namespace Game.Enemy.Interfaces
{
    /// <summary>
    ///     Reverse lookup from a physics body/collider to the owning <see cref="EnemyController" />, so the
    ///     thrown-enemy impact sweep resolves collider→controller from a dictionary instead of a per-hit
    ///     <c>GetComponent</c>. Each enemy self-registers while live in the pool.
    /// </summary>
    public interface IEnemyRegistry
    {
        void Register(Rigidbody body, EnemyController enemy);

        bool TryResolve(Rigidbody body, out EnemyController enemy);

        bool TryResolve(Collider collider, out EnemyController enemy);

        void Unregister(Rigidbody body);
    }
}