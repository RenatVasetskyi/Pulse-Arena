using Architecture.Services;
using Architecture.Services.Interfaces;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace PulseArena.Tests.EditMode.Services
{
    /// <summary>
    ///     Verifies <see cref="SlowMoService" /> — bullet-time driven off a coroutine. The runner is stubbed (so the
    ///     ease-back coroutine never runs) and the tests assert the SYNCHRONOUS contract: Trigger clamps and sets
    ///     Time.timeScale/fixedDeltaTime, Stop restores them, and Pause caches the exact dip so Resume continues it.
    ///     TearDown always restores global time so a failing test cannot poison the rest of the suite.
    /// </summary>
    [TestFixture]
    public class SlowMoServiceTests
    {
        private const float DefaultFixedDelta = 0.02f;

        private ICoroutineRunner _runner;
        private SlowMoService _slowMo;

        [SetUp]
        public void SetUp()
        {
            _runner = Substitute.For<ICoroutineRunner>();
            _slowMo = new SlowMoService(_runner);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = DefaultFixedDelta;
        }

        [Test]
        public void Trigger_SetsTimeScaleAndFixedDeltaToTheScale()
        {
            _slowMo.Trigger(0.3f, 1f);

            Assert.That(Time.timeScale, Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(Time.fixedDeltaTime, Is.EqualTo(DefaultFixedDelta * 0.3f).Within(0.0001f));
        }

        [Test]
        public void Trigger_ClampsScaleBelowTheFloor()
        {
            _slowMo.Trigger(0.01f, 1f);

            Assert.That(Time.timeScale, Is.EqualTo(0.05f).Within(0.0001f), "scale must clamp up to the 0.05 floor");
        }

        [Test]
        public void Trigger_ClampsScaleAboveOne()
        {
            _slowMo.Trigger(5f, 1f);

            Assert.That(Time.timeScale, Is.EqualTo(1f).Within(0.0001f), "scale must clamp down to 1 (no fast-forward)");
        }

        [Test]
        public void Stop_RestoresNormalTime()
        {
            _slowMo.Trigger(0.3f, 1f);

            _slowMo.Stop();

            Assert.That(Time.timeScale, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(Time.fixedDeltaTime, Is.EqualTo(DefaultFixedDelta).Within(0.0001f));
        }

        // A mechanical pause freezes time while a dip is in progress; Resume must put the exact scaled time back,
        // not snap to normal, so the bullet-time continues from where it was suspended.
        [Test]
        public void ResumeAfterPause_RestoresTheSuspendedDip()
        {
            _slowMo.Trigger(0.3f, 1f);
            _slowMo.Pause();
            Time.timeScale = 1f; // the pause gate froze time while suspended

            _slowMo.Resume();

            Assert.That(Time.timeScale, Is.EqualTo(0.3f).Within(0.0001f));
        }

        // Nothing to resume when no dip was active — Resume must be a no-op, not force a slow-mo on.
        [Test]
        public void Resume_WithNoActiveDip_LeavesTimeAlone()
        {
            _slowMo.Resume();

            Assert.That(Time.timeScale, Is.EqualTo(1f).Within(0.0001f));
        }
    }
}
