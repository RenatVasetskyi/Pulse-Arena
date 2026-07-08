using System;

namespace Game.Player.Interfaces
{
    /// <summary>
    /// The player's hit-point + invulnerability state. Pure C# (no Rigidbody, no MonoBehaviour) so the
    /// damage / heal / i-frame rules are unit-testable in isolation. The controller reacts to
    /// <see cref="Changed"/>; death is driven by the controller because it has two triggers — HP depletion
    /// here AND ring-out off the arena edge.
    /// </summary>
    public interface IPlayerHealth
    {
        int Current { get; }
        int Max { get; }
        bool IsDepleted { get; }
        bool IsInvulnerable { get; }

        event Action<int, int> Changed; // (current, max)

        void Initialize(int maxHealth, float hitInvulnerability);

        /// <summary>Applies damage. Returns false (no-op) if invulnerable or already dead.</summary>
        bool TakeDamage(int amount);

        /// <summary>Heals up to max. Returns false (no-op) if already full or dead.</summary>
        bool TryHeal(int amount);

        void Kill();

        /// <summary>Extends the i-frame window to at least <paramref name="seconds"/> (used by the dash dodge).</summary>
        void GrantInvulnerability(float seconds);

        void Tick(float deltaTime);
    }
}
