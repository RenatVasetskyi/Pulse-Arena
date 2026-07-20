using System;

namespace Architecture.Services.Interfaces
{
    /// <summary>
    ///     The player's ultimate charge. Fills as enemies are killed; when full the ultimate can be spent.
    ///     Subscribe to <see cref="ChargeChanged" /> (0..1) for the HUD bar and <see cref="Ready" /> for the
    ///     "full" flourish.
    /// </summary>
    public interface ISuperMeterService
    {
        event Action<float> ChargeChanged;
        event Action Ready;
        float Charge01 { get; }
        bool IsFull { get; }
        void Reset();

        /// <summary>Spends a full meter (empties it) and returns true; returns false if not full.</summary>
        bool TryConsume();
    }
}