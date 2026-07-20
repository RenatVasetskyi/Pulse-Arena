using Game.Common;
using NUnit.Framework;

namespace SlingRing.Tests.EditMode.Common
{
    /// <summary>
    ///     EditMode unit tests for <see cref="ActorHealth" /> — the shared plain-C# hit-point model used by
    ///     the player and every enemy. Covers the full public contract: damage/heal clamping, the
    ///     invulnerability window (largest-window-wins, counted down only by <c>Tick</c>), death via lethal
    ///     damage and <c>Kill</c> (idempotent, fires <c>Died</c> once), and <c>Initialize</c> re-arming a
    ///     dead instance for pool reuse. Deterministic: elapsed time is passed explicitly, no scene state.
    /// </summary>
    [TestFixture]
    public class ActorHealthTests
    {
        private ActorHealth _actorHealth;

        [SetUp]
        public void CreateActorHealth()
        {
            // hitInvulnerability defaults to 0 — damage does not self-grant i-frames unless a test opts in.
            _actorHealth = new ActorHealth();
            _actorHealth.Initialize(100);
        }

        [Test]
        public void Initialize_SetsCurrentToMax()
        {
            Assert.That(_actorHealth.Current, Is.EqualTo(100));
            Assert.That(_actorHealth.Max, Is.EqualTo(100));
            Assert.That(_actorHealth.IsDepleted, Is.False);
        }

        [TestCase(0)]
        [TestCase(-5)]
        public void Initialize_NonPositiveMax_ClampsMaxToOne(int maxHealth)
        {
            _actorHealth.Initialize(maxHealth);

            Assert.That(_actorHealth.Max, Is.EqualTo(1));
            Assert.That(_actorHealth.Current, Is.EqualTo(1));
        }

        [Test]
        public void Initialize_RaisesChangedWithFullHealth()
        {
            int currentHp = 0;
            int maxHp = 0;
            _actorHealth.Changed += (current, max) =>
            {
                currentHp = current;
                maxHp = max;
            };

            _actorHealth.Initialize(50);

            Assert.That(currentHp, Is.EqualTo(50));
            Assert.That(maxHp, Is.EqualTo(50));
        }

        [Test]
        public void Initialize_AfterKill_ResetsDeadState()
        {
            // Pool-reuse contract: a recycled actor must accept damage again after re-init.
            _actorHealth.Kill();

            _actorHealth.Initialize(100);

            Assert.That(_actorHealth.IsDepleted, Is.False);
            Assert.That(_actorHealth.TakeDamage(10), Is.True);
        }

        [Test]
        public void Initialize_ClearsInvulnerability()
        {
            _actorHealth.GrantInvulnerability(5f);

            _actorHealth.Initialize(100);

            Assert.That(_actorHealth.IsInvulnerable, Is.False);
        }

        [Test]
        public void TakeDamage_ReducesCurrentAndReturnsTrue()
        {
            bool result = _actorHealth.TakeDamage(30);

            Assert.That(_actorHealth.Current, Is.EqualTo(70));
            Assert.That(result, Is.True);
        }

        [Test]
        public void TakeDamage_WhileInvulnerable_DoesNothingAndReturnsFalse()
        {
            _actorHealth.GrantInvulnerability(1f);

            bool result = _actorHealth.TakeDamage(30);

            Assert.That(_actorHealth.Current, Is.EqualTo(100));
            Assert.That(result, Is.False);
        }

        [Test]
        public void TakeDamage_AfterInvulnerabilityExpires_LandsAgain()
        {
            _actorHealth.GrantInvulnerability(1f);
            _actorHealth.Tick(1.1f);

            bool result = _actorHealth.TakeDamage(30);

            Assert.That(_actorHealth.Current, Is.EqualTo(70));
            Assert.That(result, Is.True);
        }

        [Test]
        public void TakeDamage_LethalAmount_FiresDied()
        {
            bool died = false;
            _actorHealth.Died += () => died = true;

            _actorHealth.TakeDamage(100);

            Assert.That(died, Is.True);
        }

        [Test]
        public void TakeDamage_RepeatedLethalHits_FiresDiedOnce()
        {
            int diedCount = 0;
            _actorHealth.Died += () => diedCount++;

            _actorHealth.TakeDamage(100);
            _actorHealth.TakeDamage(100);

            Assert.That(diedCount, Is.EqualTo(1));
        }

        [Test]
        public void TakeDamage_RaisesChangedWithNewCurrent()
        {
            int currentHp = 0;
            int maxHp = 0;
            _actorHealth.Changed += (current, max) =>
            {
                currentHp = current;
                maxHp = max;
            };

            _actorHealth.TakeDamage(30);

            Assert.That(currentHp, Is.EqualTo(70));
            Assert.That(maxHp, Is.EqualTo(100));
        }

        [TestCase(30, 70)]
        [TestCase(100, 0)]
        [TestCase(150, 0)]
        public void TakeDamage_NeverGoesBelowZero(int damage, int expectedCurrent)
        {
            _actorHealth.TakeDamage(damage);

            Assert.That(_actorHealth.Current, Is.EqualTo(expectedCurrent));
        }

        [Test]
        public void TakeDamage_NegativeAmount_DealsNoDamage()
        {
            // A negative amount is clamped to 0 damage — it must never act as a hidden heal.
            _actorHealth.TakeDamage(-30);

            Assert.That(_actorHealth.Current, Is.EqualTo(100));
        }

        [Test]
        public void TakeDamage_WhenDead_ReturnsFalse()
        {
            _actorHealth.Kill();

            bool result = _actorHealth.TakeDamage(10);

            Assert.That(result, Is.False);
        }

        [Test]
        public void TakeDamage_WithHitInvulnerabilityConfigured_StartsInvulnerabilityWindow()
        {
            // Player-style config: every landed hit self-grants i-frames; enemies pass 0 and never do.
            _actorHealth.Initialize(100, 0.5f);

            _actorHealth.TakeDamage(10);

            Assert.That(_actorHealth.IsInvulnerable, Is.True);
        }

        [Test]
        public void TryHeal_AfterDamage_RestoresHealthAndReturnsTrue()
        {
            _actorHealth.TakeDamage(50);

            bool result = _actorHealth.TryHeal(20);

            Assert.That(_actorHealth.Current, Is.EqualTo(70));
            Assert.That(result, Is.True);
        }

        [Test]
        public void TryHeal_AmountAboveMissingHealth_ClampsAtMax()
        {
            _actorHealth.TakeDamage(10);

            _actorHealth.TryHeal(50);

            Assert.That(_actorHealth.Current, Is.EqualTo(100));
        }

        [Test]
        public void TryHeal_AtFullHealth_ReturnsFalse()
        {
            bool result = _actorHealth.TryHeal(20);

            Assert.That(result, Is.False);
            Assert.That(_actorHealth.Current, Is.EqualTo(100));
        }

        [Test]
        public void TryHeal_WhenDead_ReturnsFalse()
        {
            // Death is final until re-Initialize — a health orb must not resurrect a corpse.
            _actorHealth.Kill();

            bool result = _actorHealth.TryHeal(20);

            Assert.That(result, Is.False);
            Assert.That(_actorHealth.Current, Is.EqualTo(0));
        }

        [Test]
        public void TryHeal_AfterDamage_RaisesChangedWithNewCurrent()
        {
            _actorHealth.TakeDamage(50);
            int currentHp = 0;
            _actorHealth.Changed += (current, max) => currentHp = current;

            _actorHealth.TryHeal(20);

            Assert.That(currentHp, Is.EqualTo(70));
        }

        [Test]
        public void Kill_FiresDiedEvent()
        {
            bool died = false;
            _actorHealth.Died += () => died = true;

            _actorHealth.Kill();

            Assert.That(died, Is.True);
        }

        [Test]
        public void Kill_ZeroesCurrentAndSetsDepleted()
        {
            _actorHealth.Kill();

            Assert.That(_actorHealth.Current, Is.EqualTo(0));
            Assert.That(_actorHealth.IsDepleted, Is.True);
        }

        [Test]
        public void Kill_RaisesChangedWithZeroCurrent()
        {
            int currentHp = -1;
            _actorHealth.Changed += (current, max) => currentHp = current;

            _actorHealth.Kill();

            Assert.That(currentHp, Is.EqualTo(0));
        }

        [Test]
        public void Kill_CalledTwice_FiresDiedOnce()
        {
            int diedCount = 0;
            _actorHealth.Died += () => diedCount++;

            _actorHealth.Kill();
            _actorHealth.Kill();

            Assert.That(diedCount, Is.EqualTo(1));
        }

        [Test]
        public void GrantInvulnerability_MakesActorInvulnerable()
        {
            _actorHealth.GrantInvulnerability(1f);

            Assert.That(_actorHealth.IsInvulnerable, Is.True);
        }

        [Test]
        public void GrantInvulnerability_SmallerThanRemainingWindow_KeepsLargerWindow()
        {
            // Largest-window-wins: a short dash i-frame must never cut an already longer window.
            // If the 0.5s grant had shrunk the window, ticking 1s would have expired it.
            _actorHealth.GrantInvulnerability(2f);
            _actorHealth.GrantInvulnerability(0.5f);

            _actorHealth.Tick(1f);

            Assert.That(_actorHealth.IsInvulnerable, Is.True);
        }

        [Test]
        public void GrantInvulnerability_LargerThanRemainingWindow_ExtendsWindow()
        {
            _actorHealth.GrantInvulnerability(0.5f);
            _actorHealth.GrantInvulnerability(2f);

            _actorHealth.Tick(1f);

            Assert.That(_actorHealth.IsInvulnerable, Is.True);
        }

        [Test]
        public void Tick_PartialWindowElapsed_KeepsInvulnerabilityActive()
        {
            _actorHealth.GrantInvulnerability(1f);

            _actorHealth.Tick(0.5f);

            Assert.That(_actorHealth.IsInvulnerable, Is.True);
        }

        [Test]
        public void Tick_FullWindowElapsed_EndsInvulnerability()
        {
            _actorHealth.GrantInvulnerability(1f);

            _actorHealth.Tick(1.1f);

            Assert.That(_actorHealth.IsInvulnerable, Is.False);
        }

        [Test]
        public void Tick_DoesNotChangeCurrentHealth()
        {
            // Tick only drives the i-frame timer; HP is untouched no matter how much time passes.
            _actorHealth.TakeDamage(30);

            _actorHealth.Tick(5f);

            Assert.That(_actorHealth.Current, Is.EqualTo(70));
        }
    }
}
