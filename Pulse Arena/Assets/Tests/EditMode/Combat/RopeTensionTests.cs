using System.Collections.Generic;
using Data;
using Game.Combat;
using NUnit.Framework;

namespace PulseArena.Tests.EditMode.Combat
{
    /// <summary>
    ///     EditMode unit tests for <see cref="RopeTension" /> — the pure tension simulation behind the spun rope.
    ///     Feeds hand-computed weight/type-rate/charge numbers through <c>Tick</c> and verifies the accrual math,
    ///     the 0..1 clamp with <c>IsBroken</c>, the input floor-clamps, the <c>Warning</c> threshold remap, and the
    ///     <c>Changed</c> notification guard. No Unity scene objects — the whole fixture runs in milliseconds.
    /// </summary>
    [TestFixture]
    public class RopeTensionTests
    {
        private const float Tolerance = 1e-4f;

        private RopeTension _rope;
        private SlingshotData _slingshotData;

        [SetUp]
        public void CreateInitializedRope()
        {
            // BreakTime 1 s + zero charge influence make the accrual rate exactly weight * typeRate per second;
            // threshold 0.5 keeps the Warning remap hand-computable. RopeTension holds the SlingshotData by
            // reference, so tests may tweak fields after Initialize.
            _slingshotData = new SlingshotData
            {
                TensionBreakTime = 1f,
                TensionChargeInfluence = 0f,
                TensionWarningThreshold = 0.5f
            };
            _rope = new RopeTension();
            _rope.Initialize(_slingshotData);
        }

        [Test]
        public void Tick_UnitWeightAndTypeRate_AccruesDeltaTimeOverBreakTime()
        {
            _rope.Tick(1f, 1f, 0f, 0.5f);

            Assert.That(_rope.Value, Is.EqualTo(0.5f).Within(Tolerance));
        }

        [Test]
        public void Tick_FullChargeWithHalfInfluence_ScalesAccrualByChargeBoost()
        {
            _slingshotData.TensionChargeInfluence = 0.5f;

            _rope.Tick(1f, 1f, 1f, 0.5f);

            // chargeBoost = 1 + 1 * 0.5 = 1.5 → rate 1.5/s → 0.75 after 0.5 s.
            Assert.That(_rope.Value, Is.EqualTo(0.75f).Within(Tolerance));
        }

        [TestCase(0f, 1f)]
        [TestCase(-2f, 1f)]
        [TestCase(1f, 0f)]
        [TestCase(1f, -2f)]
        public void Tick_NonPositiveWeightOrTypeRate_ClampsFactorToTenth(float weight, float typeRate)
        {
            _rope.Tick(weight, typeRate, 0f, 1f);

            // At rate factor * 1/s over 1 s, Value equals the floor-clamped factor itself.
            Assert.That(_rope.Value, Is.EqualTo(0.1f).Within(Tolerance));
        }

        [TestCase(0f)]
        [TestCase(0.25f)]
        public void Tick_BreakTimeBelowFloor_AccruesUsingHalfSecondFloor(float breakTime)
        {
            _slingshotData.TensionBreakTime = breakTime;

            _rope.Tick(1f, 1f, 0f, 0.25f);

            // Unclamped break times this small would saturate to 1 in one step; 0.5 proves the 0.5 s floor (rate 2/s).
            Assert.That(_rope.Value, Is.EqualTo(0.5f).Within(Tolerance));
        }

        [Test]
        public void Tick_AccrualOvershootsOne_ClampsValueAtOne()
        {
            _rope.Tick(10f, 10f, 0f, 1f);

            Assert.That(_rope.Value, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void IsBroken_TensionReachesOne_ReturnsTrue()
        {
            _rope.Tick(10f, 10f, 0f, 1f);

            Assert.That(_rope.IsBroken, Is.True);
        }

        [Test]
        public void IsBroken_TensionBelowOne_ReturnsFalse()
        {
            _rope.Tick(1f, 1f, 0f, 0.5f);

            Assert.That(_rope.IsBroken, Is.False);
        }

        [TestCase(0.25f, 0f)]
        [TestCase(0.5f, 0f)]
        [TestCase(0.75f, 0.5f)]
        [TestCase(0.9f, 0.8f)]
        [TestCase(1f, 1f)]
        public void Warning_ThresholdAtHalf_RemapsTensionFromThresholdToOne(float tension, float expected)
        {
            // Rate is 1/s under the fixture data, so deltaTime doubles as the target tension.
            _rope.Tick(1f, 1f, 0f, tension);

            Assert.That(_rope.Warning, Is.EqualTo(expected).Within(Tolerance));
        }

        [Test]
        public void Tick_TensionChanges_RaisesChangedOnceWithNewValue()
        {
            List<float> received = new List<float>();
            _rope.Changed += received.Add;

            _rope.Tick(1f, 1f, 0f, 0.5f);

            Assert.That(received, Has.Count.EqualTo(1));
            Assert.That(received[0], Is.EqualTo(0.5f).Within(Tolerance));
        }

        [Test]
        public void Tick_ZeroDeltaTime_DoesNotRaiseChanged()
        {
            int changedCount = 0;
            _rope.Changed += _ => changedCount++;

            _rope.Tick(1f, 1f, 0f, 0f);

            Assert.That(changedCount, Is.EqualTo(0));
        }

        [Test]
        public void Tick_TensionAlreadyClampedAtOne_DoesNotRaiseChanged()
        {
            _rope.Tick(10f, 10f, 0f, 1f);
            int changedCount = 0;
            _rope.Changed += _ => changedCount++;

            _rope.Tick(10f, 10f, 0f, 1f);

            // Set() guards with Mathf.Approximately — a re-clamped 1 is "unchanged" and must not re-notify.
            Assert.That(changedCount, Is.EqualTo(0));
        }

        [Test]
        public void Reset_TensionNonZero_ZeroesTensionAndRaisesChanged()
        {
            _rope.Tick(1f, 1f, 0f, 0.5f);
            List<float> received = new List<float>();
            _rope.Changed += received.Add;

            _rope.Reset();

            Assert.That(_rope.Value, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(received, Has.Count.EqualTo(1));
            Assert.That(received[0], Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Reset_TensionAlreadyZero_DoesNotRaiseChanged()
        {
            int changedCount = 0;
            _rope.Changed += _ => changedCount++;

            _rope.Reset();

            Assert.That(changedCount, Is.EqualTo(0));
        }
    }
}
