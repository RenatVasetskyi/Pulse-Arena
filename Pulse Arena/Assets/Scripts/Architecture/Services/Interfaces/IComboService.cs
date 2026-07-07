using System;

namespace Architecture.Services.Interfaces
{
    /// <summary>
    /// Tracks a kill chain: each kill within the combo window bumps the score multiplier. Subscribe to
    /// <see cref="ComboChanged"/> (fires the new combo count; 0 means the chain reset) for HUD/audio.
    /// </summary>
    public interface IComboService
    {
        int Combo { get; }
        int Multiplier { get; }

        event Action<int> ComboChanged;

        /// <summary>Registers a kill and returns the score multiplier to apply.</summary>
        int RegisterKill();
        void Reset();
    }
}
