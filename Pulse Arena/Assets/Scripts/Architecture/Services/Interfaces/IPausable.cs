namespace Architecture.Services.Interfaces
{
    /// <summary>
    ///     A gameplay system that freezes its own state on <see cref="Pause" /> (caching whatever the engine
    ///     would keep mutating — physics velocity, audio sample, tween/particle progress, agent) and continues
    ///     it exactly on <see cref="Resume" />. Registered with the <see cref="IPauseService" />.
    /// </summary>
    public interface IPausable
    {
        void Pause();
        void Resume();
    }
}
