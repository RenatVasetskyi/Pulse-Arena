using Game.Enemy;
using NUnit.Framework;

namespace SlingRing.Tests.EditMode.Enemy
{
    /// <summary>
    ///     EditMode unit tests for <see cref="EnemyTimers" /> — the plain-C# bag of per-enemy countdown cooldowns
    ///     and elapsed-time up-counters gathered off <c>EnemyController</c>. Pure field math, no scene objects,
    ///     fully deterministic. The key invariant pinned here: <see cref="EnemyTimers.TickFixed" /> decrements
    ///     ONLY the five state-independent cooldowns; <see cref="EnemyTimers.Knockback" /> and
    ///     <see cref="EnemyTimers.HeldDamage" /> are decremented inside their owning states
    ///     (EnemyKnockbackState / EnemyGrabbedState) and the up-counters are advanced by the recovery/ringout
    ///     states, so a regression that ticks any of them here would double-count time.
    /// </summary>
    [TestFixture]
    public class EnemyTimersTests
    {
        private const float Tolerance = 1e-5f;

        private EnemyTimers _timers;

        [SetUp]
        public void SetUp()
        {
            _timers = new EnemyTimers();
        }

        [TestCase(0.02f)]
        [TestCase(0.5f)]
        public void TickFixed_DecrementsFiveStateIndependentCooldowns(float deltaTime)
        {
            _timers.Stasis.Set(2f);
            _timers.AttackCooldown.Set(2f);
            _timers.ImpactDamageCooldown.Set(2f);
            _timers.GroundBounceCooldown.Set(2f);
            _timers.GroundContact.Set(2f);

            _timers.TickFixed(deltaTime);

            Assert.That(_timers.Stasis.Remaining, Is.EqualTo(2f - deltaTime).Within(Tolerance));
            Assert.That(_timers.AttackCooldown.Remaining, Is.EqualTo(2f - deltaTime).Within(Tolerance));
            Assert.That(_timers.ImpactDamageCooldown.Remaining, Is.EqualTo(2f - deltaTime).Within(Tolerance));
            Assert.That(_timers.GroundBounceCooldown.Remaining, Is.EqualTo(2f - deltaTime).Within(Tolerance));
            Assert.That(_timers.GroundContact.Remaining, Is.EqualTo(2f - deltaTime).Within(Tolerance));
        }

        [Test]
        public void TickFixed_DoesNotDecrementKnockback()
        {
            // Knockback ticks in exactly one place — EnemyKnockbackState.FixedTick; ticking it here would double-count.
            _timers.Knockback.Set(1f);

            _timers.TickFixed(0.5f);

            Assert.That(_timers.Knockback.Remaining, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void TickFixed_DoesNotDecrementHeldDamage()
        {
            // HeldDamage is owned by EnemyGrabbedState; state-independent ticking would double-count while grabbed.
            _timers.HeldDamage.Set(1f);

            _timers.TickFixed(0.5f);

            Assert.That(_timers.HeldDamage.Remaining, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void TickFixed_DoesNotAdvanceUpCounters()
        {
            // The recovery/ringout states increment their own elapsed counters; TickFixed must leave them untouched.
            _timers.PhysicsRecoveryElapsed = 1.25f;
            _timers.RingoutElapsed = 0.75f;

            _timers.TickFixed(0.5f);

            Assert.That(_timers.PhysicsRecoveryElapsed, Is.EqualTo(1.25f).Within(Tolerance));
            Assert.That(_timers.RingoutElapsed, Is.EqualTo(0.75f).Within(Tolerance));
        }

        [Test]
        public void ResetAll_ZeroesEveryCooldownAndUpCounter()
        {
            _timers.Knockback.Set(1f);
            _timers.Stasis.Set(2f);
            _timers.HeldDamage.Set(3f);
            _timers.AttackCooldown.Set(4f);
            _timers.ImpactDamageCooldown.Set(5f);
            _timers.GroundBounceCooldown.Set(6f);
            _timers.GroundContact.Set(7f);
            _timers.PhysicsRecoveryElapsed = 8f;
            _timers.RingoutElapsed = 9f;

            _timers.ResetAll();

            Assert.That(_timers.Knockback.Remaining, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(_timers.Stasis.Remaining, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(_timers.HeldDamage.Remaining, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(_timers.AttackCooldown.Remaining, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(_timers.ImpactDamageCooldown.Remaining, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(_timers.GroundBounceCooldown.Remaining, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(_timers.GroundContact.Remaining, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(_timers.PhysicsRecoveryElapsed, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(_timers.RingoutElapsed, Is.EqualTo(0f).Within(Tolerance));
        }
    }
}
