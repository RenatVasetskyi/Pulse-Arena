using System;

namespace Game.Common.Interfaces
{
    /// <summary>
    ///     An actor's hit-point + invulnerability state — the player and every enemy share it. Pure C# (no
    ///     Rigidbody, no MonoBehaviour) so the damage / heal / i-frame rules are unit-testable in isolation.
    ///     Owners react to <see cref="Changed" />; death is driven by the owner because the triggers differ —
    ///     the player dies from two sources (HP depletion here AND ring-out off the arena edge), while an enemy
    ///     keys off HP depletion. Invulnerability is opt-in (enemies pass 0 to <see cref="Initialize" />).
    /// </summary>
    public interface IActorHealth
    {
        event Action<int, int> Changed; // (current, max)
        int Current { get; }
        bool IsDepleted { get; }
        bool IsInvulnerable { get; }
        int Max { get; }

        void Initialize(int maxHealth, float hitInvulnerability);

        /// <summary>Extends the i-frame window to at least <paramref name="seconds" /> (used by the dash dodge).</summary>
        void GrantInvulnerability(float seconds);

        void Kill();

        /// <summary>Applies damage. Returns false (no-op) if invulnerable or already dead.</summary>
        bool TakeDamage(int amount);

        void Tick(float deltaTime);

        /// <summary>Heals up to max. Returns false (no-op) if already full or dead.</summary>
        bool TryHeal(int amount);
    }
}