using UnityEngine;

namespace Game.Enemy.Interfaces
{
    /// <summary>
    ///     Reverse lookup from a physics body/collider to the <see cref="EnemyController" /> that owns it, so hot
    ///     physics paths (the thrown-enemy impact sweep) resolve "which enemy did this collider hit?" from a
    ///     dictionary instead of a per-hit <c>GetComponent</c> every FixedUpdate. Each enemy self-registers for
    ///     the span it is live in the pool.
    /// </summary>
    public interface IEnemyRegistry
    {
        void Register(Rigidbody body, EnemyController enemy);

        bool TryResolve(Rigidbody body, out EnemyController enemy);

        bool TryResolve(Collider collider, out EnemyController enemy);

        void Unregister(Rigidbody body);
    }
}
