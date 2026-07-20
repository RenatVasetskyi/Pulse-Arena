namespace Architecture.Services.Interfaces
{
    /// <summary>
    ///     Mechanical pause (NOT Time.timeScale=0): freezes/continues every registered <see cref="IPausable" />
    ///     as a group so a resume picks up from the exact spot. Idempotent.
    /// </summary>
    public interface IPauseService
    {
        bool IsPaused { get; }
        void Register(IPausable pausable);
        void Unregister(IPausable pausable);
        void Pause();
        void Unpause();
        void Clear();
    }
}
