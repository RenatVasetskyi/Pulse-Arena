using System;

namespace Game.Player.Interfaces
{
    /// <summary>
    ///     The player's hit-point + invulnerability state. Pure C# (no Rigidbody, no MonoBehaviour) so the
    ///     damage / heal / i-frame rules are unit-testable in isolation. The controller reacts to
    ///     <see cref="Changed" />; death is driven by the controller because it has two triggers — HP depletion
    ///     here AND ring-out off the arena edge.
    /// </summary>
    public interface IPlayerHealth
    {
        event Action<int, int> Changed; // (current, max)
        int Current { get; }
        bool IsDepleted { get; }
        bool IsInvulnerable { get; }
        int Max { get; }

        /// <summary>Extends the i-frame window to at least <paramref name="seconds" /> (used by the dash dodge).</summary>
        void GrantInvulnerability(float seconds);

        void Initialize(int maxHealth, float hitInvulnerability);

        void Kill();

        /// <summary>Applies damage. Returns false (no-op) if invulnerable or already dead.</summary>
        bool TakeDamage(int amount);

        void Tick(float deltaTime);

        /// <summary>Heals up to max. Returns false (no-op) if already full or dead.</summary>
        bool TryHeal(int amount);
    }
}