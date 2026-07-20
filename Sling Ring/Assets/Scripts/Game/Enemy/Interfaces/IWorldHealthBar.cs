namespace Game.Enemy.Interfaces
{
    /// <summary>
    ///     Contract the enemy drives its floating HP bar through. Implemented by the UI-layer WorldHealthBar,
    ///     so gameplay (EnemyController) never has to reference the UI namespace.
    /// </summary>
    public interface IWorldHealthBar
    {
        void Initialize(int maxHealth, float height);
        void SetHealth(int health, int maxHealth);

        // Enemy hides the bar the instant death/ring-out starts so an empty bar doesn't hang over the dying body; shown again on pooled respawn.
        void SetVisible(bool visible);
    }
}