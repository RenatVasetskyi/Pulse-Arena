namespace Game.Enemy.Behaviors
{
    /// <summary>
    ///     An enemy's swappable combat "brain": the per-type pursue + attack decision-making that
    ///     <c>EnemyChaseState</c> delegates each physics tick. Universal reactions (grab, knockback, ringout, death,
    ///     ground-recovery) live in the shared FSM states, not here — only the offense archetype varies. Selected per
    ///     <c>EnemyTypeData.Behavior</c> via <see cref="EnemyBehaviorFactory" />; a new archetype is a new impl + a
    ///     factory case + a data value, no <c>EnemyController</c> change.
    /// </summary>
    public interface IEnemyBehavior
    {
        void Initialize(EnemyContext context);

        void OnEnterChase();

        void Tick();
    }
}
