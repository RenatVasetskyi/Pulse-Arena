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

        // Hide/show the whole bar — the enemy hides it the moment death (or ring-out) starts so an empty bar
        // doesn't hang over the dying body, and shows it again on pooled respawn.
        void SetVisible(bool visible);
    }
}