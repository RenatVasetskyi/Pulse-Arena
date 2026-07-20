namespace Architecture.Services.Interfaces
{
    /// <summary>
    ///     Brief bullet-time: dips Time.timeScale for a moment then eases back. Runs on real (unscaled)
    ///     time. Pause/end-game must call <see cref="Stop" /> first so the restore never fights their freeze.
    /// </summary>
    public interface ISlowMoService
    {
        void Stop();
        void Trigger(float scale, float duration);

        // Mechanical pause: suspend an in-progress dip and continue it from the exact same point on resume.
        void Pause();
        void Resume();
    }
}