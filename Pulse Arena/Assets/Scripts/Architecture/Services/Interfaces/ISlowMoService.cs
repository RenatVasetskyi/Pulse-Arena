namespace Architecture.Services.Interfaces
{
    /// <summary>
    /// Brief bullet-time: dips Time.timeScale for a moment then eases back. Runs on real (unscaled)
    /// time. Pause/end-game must call <see cref="Stop"/> first so the restore never fights their freeze.
    /// </summary>
    public interface ISlowMoService
    {
        void Trigger(float scale, float duration);
        void Stop();
    }
}
