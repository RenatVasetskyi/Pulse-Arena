namespace Game.Enemy.Behaviors
{
    /// <summary>
    ///     An enemy's swappable combat "brain": the per-type pursue + attack decision-making that
    ///     <c>EnemyChaseState</c> delegates to each physics tick. The UNIVERSAL reactions (grab, knockback, ringout,
    ///     death, ground-recovery) live in the shared FSM states and are NOT here — only the offense archetype (how
    ///     this enemy hunts and strikes) varies. Selected per <c>EnemyTypeData.Behavior</c> via
    ///     <see cref="EnemyBehaviorFactory" />; a new archetype (ranged, flyer, …) is a new impl + a factory case + a
    ///     data value, with no change to <c>EnemyController</c>'s core.
    /// </summary>
    public interface IEnemyBehavior
    {
        /// <summary>One-time wiring: hand the behaviour the context it drives (target reads, movement, transitions).</summary>
        void Initialize(EnemyContext context);

        /// <summary>Called when the enemy (re)enters its pursue state — reset any per-approach state (e.g. the flourish roll).</summary>
        void OnEnterChase();

        /// <summary>The per-physics-tick decision: pursue the target, commit to an attack, or trigger a flourish.</summary>
        void Tick();
    }
}
