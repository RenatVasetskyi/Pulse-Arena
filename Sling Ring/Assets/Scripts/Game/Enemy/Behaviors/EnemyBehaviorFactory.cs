using Data;

namespace Game.Enemy.Behaviors
{
    /// <summary>
    ///     Maps an <see cref="EnemyBehaviorId" /> to a fresh <see cref="IEnemyBehavior" /> — the single place that
    ///     knows every archetype, so registering a new enemy behaviour is one line here. <c>EnemyController</c>
    ///     caches the result per pooled instance, so this allocates once per (instance, behaviour), never per spawn.
    /// </summary>
    public static class EnemyBehaviorFactory
    {
        public static IEnemyBehavior Create(EnemyBehaviorId id)
        {
            return id switch
            {
                EnemyBehaviorId.MeleeChaser => new MeleeChaseBehavior(),
                _ => new MeleeChaseBehavior()
            };
        }
    }
}
