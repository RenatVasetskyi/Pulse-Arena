using System;

namespace Architecture.Services.Interfaces
{
    /// <summary>
    ///     Tracks a kill chain: each kill within the combo window bumps the score multiplier. Subscribe to
    ///     <see cref="ComboChanged" /> (fires the new combo count; 0 means the chain reset) for HUD/audio.
    /// </summary>
    public interface IComboService
    {
        event Action<int> ComboChanged;
        int Combo { get; }
        int Multiplier { get; }

        /// <summary>Registers a kill and returns the score multiplier to apply.</summary>
        int RegisterKill();

        void Reset();
    }
}