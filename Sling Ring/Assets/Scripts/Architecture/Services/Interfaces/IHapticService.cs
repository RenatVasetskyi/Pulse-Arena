namespace Architecture.Services.Interfaces
{
    /// <summary>
    ///     Native haptics seam. Plays the lightest tap a device can give — a Taptic light impact on iOS, a short
    ///     one-shot on Android — and is a silent no-op in the Editor and on desktop. Owns the settings toggle, so
    ///     callers never touch platform code.
    /// </summary>
    public interface IHapticService
    {
        void PlayLight();
    }
}
