using System;
using System.Collections.Generic;
using Architecture.Services;
using Architecture.Services.Interfaces;
using Data;
using NSubstitute;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SlingRing.Tests.EditMode.Services
{
    /// <summary>
    ///     Verifies <see cref="SuperMeterService" /> — the ultimate-charge meter that gains one notch per
    ///     kill and unlocks <c>TryConsume</c> when full. Approach: a substituted <see cref="IComboService" />
    ///     raises <c>ComboChanged</c> to simulate kills, and a real <see cref="GameSettings" /> facade is
    ///     built from EditMode ScriptableObject instances (the sub-config slot wired through its serialized
    ///     property, destroyed in TearDown) so the service reads <c>KillsToCharge</c> exactly as in production.
    /// </summary>
    [TestFixture]
    public class SuperMeterServiceTests
    {
        private const int KillsToCharge = 3;
        private const float Tolerance = 0.0001f;

        private readonly List<Object> _createdAssets = new();

        private IComboService _comboService;
        private SuperMeterService _service;

        [SetUp]
        public void SetUp()
        {
            _comboService = Substitute.For<IComboService>();
            GameSettings settings = CreateSettings(KillsToCharge);
            _service = new SuperMeterService(settings, _comboService);
        }

        [TearDown]
        public void TearDown()
        {
            _service.Dispose();
            DestroyCreatedAssets();
        }

        [Test]
        public void Charge01_WithoutKills_IsZero()
        {
            Assert.That(_service.Charge01, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void IsFull_WithoutKills_IsFalse()
        {
            Assert.That(_service.IsFull, Is.False);
        }

        [Test]
        public void ComboChanged_PositiveCombo_AddsOneNotchOfCharge()
        {
            RaiseKills(1);

            Assert.That(_service.Charge01, Is.EqualTo(1f / KillsToCharge).Within(Tolerance));
        }

        // Combo 0 is the chain-reset signal (and negatives are malformed) — neither must feed the meter.
        [TestCase(0)]
        [TestCase(-1)]
        public void ComboChanged_NonPositiveCombo_DoesNotCharge(int combo)
        {
            _comboService.ComboChanged += Raise.Event<Action<int>>(combo);

            Assert.That(_service.Charge01, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void ComboChanged_EachKill_RaisesChargeChangedWithNormalizedCharge()
        {
            List<float> charges = new List<float>();
            _service.ChargeChanged += charge => charges.Add(charge);

            RaiseKills(KillsToCharge);

            Assert.That(charges, Has.Count.EqualTo(KillsToCharge));
            Assert.That(charges[0], Is.EqualTo(1f / KillsToCharge).Within(Tolerance));
            Assert.That(charges[1], Is.EqualTo(2f / KillsToCharge).Within(Tolerance));
            Assert.That(charges[2], Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void IsFull_AfterKillsToChargeKills_IsTrue()
        {
            RaiseKills(KillsToCharge);

            Assert.That(_service.IsFull, Is.True);
        }

        [Test]
        public void ComboChanged_WhenMeterFull_IsIgnored()
        {
            List<float> charges = new List<float>();
            _service.ChargeChanged += charge => charges.Add(charge);
            RaiseKills(KillsToCharge);

            RaiseKills(1);

            Assert.That(charges, Has.Count.EqualTo(KillsToCharge));
            Assert.That(_service.Charge01, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void Ready_WhenMeterFills_IsRaisedOnce()
        {
            int readyCount = 0;
            _service.Ready += () => readyCount++;

            RaiseKills(KillsToCharge);

            Assert.That(readyCount, Is.EqualTo(1));
        }

        [Test]
        public void Ready_AfterConsumeAndRefill_IsRaisedAgain()
        {
            int readyCount = 0;
            _service.Ready += () => readyCount++;
            RaiseKills(KillsToCharge);
            _service.TryConsume();

            RaiseKills(KillsToCharge);

            Assert.That(readyCount, Is.EqualTo(2));
        }

        [Test]
        public void TryConsume_WhenFull_ReturnsTrue()
        {
            RaiseKills(KillsToCharge);

            bool consumed = _service.TryConsume();

            Assert.That(consumed, Is.True);
        }

        [Test]
        public void TryConsume_WhenFull_EmptiesMeter()
        {
            RaiseKills(KillsToCharge);

            _service.TryConsume();

            Assert.That(_service.Charge01, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(_service.IsFull, Is.False);
        }

        [Test]
        public void TryConsume_WhenFull_RaisesChargeChangedWithZero()
        {
            RaiseKills(KillsToCharge);
            List<float> charges = new List<float>();
            _service.ChargeChanged += charge => charges.Add(charge);

            _service.TryConsume();

            Assert.That(charges, Has.Count.EqualTo(1));
            Assert.That(charges[0], Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void TryConsume_WhenNotFull_ReturnsFalse()
        {
            RaiseKills(1);

            bool consumed = _service.TryConsume();

            Assert.That(consumed, Is.False);
        }

        [Test]
        public void TryConsume_WhenNotFull_KeepsCharge()
        {
            RaiseKills(1);

            _service.TryConsume();

            Assert.That(_service.Charge01, Is.EqualTo(1f / KillsToCharge).Within(Tolerance));
        }

        [Test]
        public void Reset_EmptiesMeter()
        {
            RaiseKills(KillsToCharge);

            _service.Reset();

            Assert.That(_service.Charge01, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(_service.IsFull, Is.False);
        }

        [Test]
        public void Reset_RaisesChargeChangedWithZero()
        {
            RaiseKills(1);
            List<float> charges = new List<float>();
            _service.ChargeChanged += charge => charges.Add(charge);

            _service.Reset();

            Assert.That(charges, Has.Count.EqualTo(1));
            Assert.That(charges[0], Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Dispose_UnsubscribesFromComboEvents()
        {
            // TearDown disposes again; removing an already-removed handler is a no-op, so double Dispose is safe.
            _service.Dispose();

            RaiseKills(1);

            Assert.That(_service.Charge01, Is.EqualTo(0f).Within(Tolerance));
        }

        [TestCase(0)]
        [TestCase(-5)]
        public void Constructor_NonPositiveKillsToCharge_TreatsSingleKillAsFull(int configured)
        {
            IComboService comboService = Substitute.For<IComboService>();
            GameSettings settings = CreateSettings(configured);
            SuperMeterService service = new SuperMeterService(settings, comboService);

            comboService.ComboChanged += Raise.Event<Action<int>>(1);

            Assert.That(service.IsFull, Is.True);

            service.Dispose();
        }

        private void RaiseKills(int count)
        {
            // The combo service fires ComboChanged with the new chain count on every kill; any value >= 1 is a kill.
            for (int i = 0; i < count; i++)
            {
                _comboService.ComboChanged += Raise.Event<Action<int>>(1);
            }
        }

        private GameSettings CreateSettings(int killsToCharge)
        {
            CombatConfig combat = ScriptableObject.CreateInstance<CombatConfig>();
            combat.Super.KillsToCharge = killsToCharge;

            GameSettings settings = ScriptableObject.CreateInstance<GameSettings>();
            AssignCombatConfig(settings, combat);

            _createdAssets.Add(combat);
            _createdAssets.Add(settings);
            return settings;
        }

        private static void AssignCombatConfig(GameSettings settings, CombatConfig combat)
        {
            // The sub-config slot is a private [SerializeField] wired in the Inspector in production; tests
            // wire it through the serialized property so the facade pass-through works without new API surface.
            SerializedObject serialized = new SerializedObject(settings);
            serialized.FindProperty("_combat").objectReferenceValue = combat;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private void DestroyCreatedAssets()
        {
            foreach (Object asset in _createdAssets)
            {
                Object.DestroyImmediate(asset);
            }

            _createdAssets.Clear();
        }
    }
}
