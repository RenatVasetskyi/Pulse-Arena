using Game.Common;
using NUnit.Framework;

namespace SlingRing.Tests.EditMode.Common
{
    /// <summary>
    ///     EditMode unit tests for <see cref="Cooldown" /> — the pure value-type countdown timer shared by
    ///     actor helpers. No Unity or scene dependencies, so every test is a deterministic Arrange-Act-Assert
    ///     over the struct's public API: <c>Set</c> / <c>SetMax</c> / <c>Clear</c> / <c>Tick</c> and the
    ///     <c>Remaining</c> / <c>IsActive</c> reads, including the single-step overshoot edge.
    /// </summary>
    [TestFixture]
    public class CooldownTests
    {
        private const float Tolerance = 1e-5f;

        private Cooldown _cooldown;

        [SetUp]
        public void CreateCooldown()
        {
            _cooldown = new Cooldown();
        }

        [Test]
        public void NewCooldown_IsInactiveWithZeroRemaining()
        {
            Assert.That(_cooldown.IsActive, Is.False);
            Assert.That(_cooldown.Remaining, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Set_PositiveSeconds_BecomesActiveWithThatTime()
        {
            _cooldown.Set(1.5f);

            Assert.That(_cooldown.IsActive, Is.True);
            Assert.That(_cooldown.Remaining, Is.EqualTo(1.5f).Within(Tolerance));
        }

        [TestCase(0f)]
        [TestCase(-0.5f)]
        public void Set_NonPositiveSeconds_StaysInactive(float seconds)
        {
            _cooldown.Set(seconds);

            Assert.That(_cooldown.IsActive, Is.False);
        }

        [Test]
        public void Set_OverwritesPreviousValue()
        {
            // Unlike SetMax, Set must overwrite even with a smaller value.
            _cooldown.Set(5f);

            _cooldown.Set(1f);

            Assert.That(_cooldown.Remaining, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void Tick_WhileRunning_SubtractsElapsedTime()
        {
            _cooldown.Set(1f);

            _cooldown.Tick(0.3f);

            Assert.That(_cooldown.Remaining, Is.EqualTo(0.7f).Within(Tolerance));
        }

        [Test]
        public void Tick_WhenAlreadyExpired_StaysAtZero()
        {
            // The internal "> 0" guard must block subtraction on an expired timer.
            _cooldown.Tick(0.5f);

            Assert.That(_cooldown.Remaining, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Tick_LargerThanRemaining_OvershootsBelowZeroInOneStep()
        {
            // Real edge: a running timer subtracts the FULL delta, so a single step can land negative.
            // IsActive reads "> 0", so the negative result still reports inactive.
            _cooldown.Set(0.1f);

            _cooldown.Tick(0.25f);

            Assert.That(_cooldown.Remaining, Is.EqualTo(-0.15f).Within(Tolerance));
            Assert.That(_cooldown.IsActive, Is.False);
        }

        [Test]
        public void Tick_MultipleSteps_AccumulatesSubtraction()
        {
            _cooldown.Set(1f);

            _cooldown.Tick(0.4f);
            _cooldown.Tick(0.4f);

            Assert.That(_cooldown.Remaining, Is.EqualTo(0.2f).Within(Tolerance));
        }

        [Test]
        public void SetMax_SmallerThanRemaining_KeepsCurrentValue()
        {
            // The keep-alive contract: SetMax must never shorten a longer running timer.
            _cooldown.Set(1f);

            _cooldown.SetMax(0.5f);

            Assert.That(_cooldown.Remaining, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void SetMax_LargerThanRemaining_OverwritesRemaining()
        {
            _cooldown.Set(1f);

            _cooldown.SetMax(2f);

            Assert.That(_cooldown.Remaining, Is.EqualTo(2f).Within(Tolerance));
        }

        [Test]
        public void SetMax_OnExpiredTimer_ArmsTimer()
        {
            _cooldown.SetMax(1f);

            Assert.That(_cooldown.IsActive, Is.True);
            Assert.That(_cooldown.Remaining, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void Clear_ActiveTimer_ResetsToInactiveZero()
        {
            _cooldown.Set(5f);

            _cooldown.Clear();

            Assert.That(_cooldown.IsActive, Is.False);
            Assert.That(_cooldown.Remaining, Is.EqualTo(0f).Within(Tolerance));
        }
    }
}
