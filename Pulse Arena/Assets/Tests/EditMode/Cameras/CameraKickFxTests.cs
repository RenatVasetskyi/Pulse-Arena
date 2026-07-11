using Game.Cameras;
using NUnit.Framework;
using UnityEngine;

namespace PulseArena.Tests.EditMode.Cameras
{
    /// <summary>
    ///     Unit tests for <see cref="CameraKickFx" /> — the pure-math transient camera juice (sine position
    ///     kick + attack/spring FOV punch). Time is fed through <c>Tick(deltaTime)</c>, so the curves are
    ///     sampled deterministically at hand-picked points: the sine peak at half duration, the FOV value
    ///     mid-attack, and the guaranteed decay back to exactly zero once a pulse has run its course.
    ///     Note: <c>Tick</c> evaluates the curve at the CURRENT timer and only then advances it, so a sample
    ///     at progress p needs one tick to reach p and one more tick to read it.
    /// </summary>
    [TestFixture]
    public class CameraKickFxTests
    {
        private CameraKickFx _fx;

        [SetUp]
        public void CreateFx()
        {
            _fx = new CameraKickFx();
        }

        [Test]
        public void New_Instance_HasNoOffsetAndNoFovKick()
        {
            Assert.That(_fx.CurrentOffset, Is.EqualTo(Vector3.zero));
            Assert.That(_fx.CurrentFovKick, Is.EqualTo(0f));
        }

        [Test]
        public void KickOffset_PeaksAtFullStrength_AtHalfDuration()
        {
            Vector3 kick = new Vector3(0f, 0f, -0.6f);
            _fx.KickOffset(kick, 1f);

            _fx.Tick(0.5f); // evaluates at progress 0 (sin 0 = 0), advances timer to 0.5
            _fx.Tick(0.01f); // evaluates at progress 0.5 → sin(π/2) = 1 → full kick

            Assert.That(_fx.CurrentOffset.z, Is.EqualTo(kick.z).Within(1e-4f));
        }

        [Test]
        public void KickOffset_DecaysToExactlyZero_AfterDuration()
        {
            _fx.KickOffset(new Vector3(1f, 1f, 1f), 0.5f);

            _fx.Tick(0.6f); // advances past the full duration
            _fx.Tick(0.01f); // expired guard → hard zero, no residual drift

            Assert.That(_fx.CurrentOffset, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void FovPunch_RampsDuringAttackWindow()
        {
            _fx.FovPunch(10f, 1f); // attack window = 30% of duration = 0.3s

            _fx.Tick(0.15f); // evaluates at t=0 (kick 0), advances to mid-attack
            _fx.Tick(0.01f); // evaluates at t=0.15 → ease-out: 10 * (1 - (1-0.5)²) = 7.5

            Assert.That(_fx.CurrentFovKick, Is.EqualTo(7.5f).Within(1e-3f));
        }

        [Test]
        public void FovPunch_DecaysToExactlyZero_AfterDuration()
        {
            _fx.FovPunch(-3f, 0.2f);

            _fx.Tick(0.25f);
            _fx.Tick(0.01f);

            Assert.That(_fx.CurrentFovKick, Is.EqualTo(0f));
        }

        [Test]
        public void KickOffset_ZeroDuration_IsFloorClampedAndDoesNotProduceNaN()
        {
            // The 0.01s duration floor protects the progress division; a zero request must not blow up.
            _fx.KickOffset(new Vector3(1f, 0f, 0f), 0f);

            _fx.Tick(0.02f);
            _fx.Tick(0.01f);

            Assert.That(float.IsNaN(_fx.CurrentOffset.x), Is.False);
            Assert.That(_fx.CurrentOffset, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void KickOffset_NewPulse_RestartsTheEnvelope()
        {
            _fx.KickOffset(new Vector3(1f, 0f, 0f), 1f);
            _fx.Tick(0.9f); // deep into the first pulse

            _fx.KickOffset(new Vector3(0f, 2f, 0f), 1f); // re-trigger resets the timer
            _fx.Tick(0.5f);
            _fx.Tick(0.01f);

            // The second pulse peaks on its own timeline — proof the timer restarted.
            Assert.That(_fx.CurrentOffset.y, Is.EqualTo(2f).Within(1e-4f));
        }
    }
}
