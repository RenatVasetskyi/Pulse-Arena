using Game.Common;

namespace Game.Enemy
{
    /// <summary>
    ///     Every per-enemy timer. Each countdown is a <see cref="Cooldown" /> (raw seconds behind
    ///     <c>.Remaining</c>); the two up-counters — physics-recovery elapsed and ringout elapsed — stay plain
    ///     floats. <see cref="TickFixed" /> decrements the shared cooldowns that tick in every non-dead state;
    ///     <see cref="Stasis" /> is ticked here (state-independent, EnemyStasisState only reads it). It does NOT
    ///     touch <see cref="Knockback" /> or <see cref="HeldDamage" /> — those tick inside their owning states
    ///     (EnemyKnockbackState / EnemyGrabbedState, where the knockback-expiry side effect also lives) — nor the
    ///     up-counters (the recovery/ringout states advance those).
    /// </summary>
    public sealed class EnemyTimers
    {
        public Cooldown AttackCooldown;
        public Cooldown GroundBounceCooldown;
        public Cooldown GroundContact;
        public Cooldown HeldDamage;

        public Cooldown ImpactDamageCooldown;

        // --- countdown cooldowns ---
        public Cooldown Knockback;

        // --- up-counters (elapsed time, counted up by their owning state) ---
        public float PhysicsRecoveryElapsed;
        public float RingoutElapsed;
        public Cooldown Stasis;

        /// <summary>Zeroes every timer and both up-counters (pool reuse / spawn reset).</summary>
        public void ResetAll()
        {
            Knockback.Clear();
            Stasis.Clear();
            HeldDamage.Clear();
            AttackCooldown.Clear();
            ImpactDamageCooldown.Clear();
            GroundBounceCooldown.Clear();
            GroundContact.Clear();
            PhysicsRecoveryElapsed = 0f;
            RingoutElapsed = 0f;
        }

        /// <summary>
        ///     Decrement the cooldowns that tick in every non-dead state. Stasis is included because it counts
        ///     down in every non-dead FixedUpdate independent of the active state (EnemyStasisState only reads it).
        /// </summary>
        public void TickFixed(float deltaTime)
        {
            Stasis.Tick(deltaTime);
            AttackCooldown.Tick(deltaTime);
            ImpactDamageCooldown.Tick(deltaTime);
            GroundBounceCooldown.Tick(deltaTime);
            GroundContact.Tick(deltaTime);
        }
    }
}