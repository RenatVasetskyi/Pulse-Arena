using Game.Common;

namespace Game.Enemy
{
    /// <summary>
    /// Every per-enemy timer, gathered off the controller. Each countdown is a <see cref="Cooldown"/>
    /// (raw seconds behind <c>.Remaining</c>, so state code that used to read a bare float field is
    /// byte-identical); the two things that count UP instead of down — the physics-recovery elapsed
    /// time and the ringout elapsed time — stay plain floats.
    ///
    /// <see cref="TickFixed"/> reproduces the old <c>TickTimers</c> body EXACTLY: it decrements the five
    /// shared cooldowns that ticked in every non-dead state (attack, impact-damage, ground-bounce,
    /// ground-contact, and <see cref="Stasis"/>). The original <c>TickTimers</c> ran in every non-dead
    /// FixedUpdate and unconditionally counted <c>_stasisTimer</c> down regardless of the active state,
    /// so <see cref="Stasis"/> is ticked here (state-independent) to keep that byte-identical — the
    /// EnemyStasisState branch only READS it. It deliberately does NOT touch <see cref="Knockback"/> or
    /// <see cref="HeldDamage"/> — those are decremented inside their owning states
    /// (EnemyKnockbackState / EnemyGrabbedState), and the knockback-expiry side effect lives in
    /// EnemyKnockbackState, not here. It also does NOT advance the up-counters (the recovery/ringout
    /// states increment those themselves).
    /// </summary>
    public sealed class EnemyTimers
    {
        // --- countdown cooldowns ---
        public Cooldown Knockback;
        public Cooldown Stasis;
        public Cooldown HeldDamage;
        public Cooldown AttackCooldown;
        public Cooldown ImpactDamageCooldown;
        public Cooldown GroundBounceCooldown;
        public Cooldown GroundContact;

        // --- up-counters (elapsed time, counted up by their owning state) ---
        public float PhysicsRecoveryElapsed;
        public float RingoutElapsed;

        /// <summary>
        /// The old TickTimers body: decrement the cooldowns that ticked in every non-dead state.
        /// Stasis is included because the original counted it down in every non-dead FixedUpdate,
        /// independent of the active state (EnemyStasisState only reads it).
        /// (EnemyImpact.Tick still runs on the controller — it owns a dictionary, not a float.)
        /// </summary>
        public void TickFixed(float deltaTime)
        {
            Stasis.Tick(deltaTime);
            AttackCooldown.Tick(deltaTime);
            ImpactDamageCooldown.Tick(deltaTime);
            GroundBounceCooldown.Tick(deltaTime);
            GroundContact.Tick(deltaTime);
        }

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
    }
}
