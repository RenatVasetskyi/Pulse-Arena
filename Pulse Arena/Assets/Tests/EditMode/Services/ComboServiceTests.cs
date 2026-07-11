using System.Collections.Generic;
using Architecture.Services;
using Architecture.Services.Interfaces;
using Data;
using NSubstitute;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace PulseArena.Tests.EditMode.Services
{
    /// <summary>
    ///     Tests <see cref="ComboService" /> — the kill-chain service where each kill inside the combo window
    ///     grows the combo count and the score multiplier (capped at the configured maximum). The service reads
    ///     its tuning from <see cref="GameSettings" />, so the fixture builds real in-memory
    ///     <see cref="CombatConfig" /> / <see cref="GameSettings" /> assets and wires the private sub-config slot
    ///     through <see cref="SerializedObject" /> — the test-side equivalent of Inspector wiring. Time.time is
    ///     frozen within an EditMode test, so every kill lands inside the window by construction; window expiry
    ///     and the pause-shift need real elapsed time and are deliberately out of scope here.
    /// </summary>
    [TestFixture]
    public class ComboServiceTests
    {
        private const int DefaultMaxMultiplier = 8;

        private CombatConfig _combatConfig;
        private GameSettings _gameSettings;
        private IPauseService _pauseService;

        [SetUp]
        public void SetUp()
        {
            _pauseService = Substitute.For<IPauseService>();
            _combatConfig = ScriptableObject.CreateInstance<CombatConfig>();
            _gameSettings = ScriptableObject.CreateInstance<GameSettings>();
            AssignCombatConfig(_gameSettings, _combatConfig);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameSettings);
            Object.DestroyImmediate(_combatConfig);
        }

        [Test]
        public void Constructor_RegistersServiceWithPauseService()
        {
            ComboService service = CreateService(DefaultMaxMultiplier);

            _pauseService.Received(1).Register(service);
        }

        [Test]
        public void Combo_BeforeAnyKill_IsZero()
        {
            ComboService service = CreateService(DefaultMaxMultiplier);

            Assert.That(service.Combo, Is.EqualTo(0));
        }

        [Test]
        public void Multiplier_BeforeAnyKill_IsOne()
        {
            ComboService service = CreateService(DefaultMaxMultiplier);

            // Clamped to at least 1 so score math never multiplies by zero.
            Assert.That(service.Multiplier, Is.EqualTo(1));
        }

        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(5, 5)]
        public void RegisterKill_ConsecutiveKillsWithinWindow_ReturnsChainLengthAsMultiplier(int kills, int expectedMultiplier)
        {
            ComboService service = CreateService(DefaultMaxMultiplier);

            int multiplier = 0;
            for (int i = 0; i < kills; i++)
                multiplier = service.RegisterKill();

            Assert.That(multiplier, Is.EqualTo(expectedMultiplier));
        }

        [Test]
        public void RegisterKill_ChainExceedsMaxMultiplier_ReturnsCappedMultiplier()
        {
            ComboService service = CreateService(3);

            int multiplier = 0;
            for (int i = 0; i < 5; i++)
                multiplier = service.RegisterKill();

            Assert.That(multiplier, Is.EqualTo(3));
        }

        [Test]
        public void RegisterKill_ChainExceedsMaxMultiplier_ComboKeepsCounting()
        {
            ComboService service = CreateService(3);

            for (int i = 0; i < 5; i++)
                service.RegisterKill();

            // Only the multiplier is capped; the HUD still shows the full chain length.
            Assert.That(service.Combo, Is.EqualTo(5));
        }

        [Test]
        public void RegisterKill_ConsecutiveKills_RaisesComboChangedWithGrowingComboCount()
        {
            ComboService service = CreateService(DefaultMaxMultiplier);
            List<int> reported = new List<int>();
            service.ComboChanged += combo => reported.Add(combo);

            service.RegisterKill();
            service.RegisterKill();
            service.RegisterKill();

            Assert.That(reported, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void Reset_AfterKills_SetsComboToZero()
        {
            ComboService service = CreateService(DefaultMaxMultiplier);
            service.RegisterKill();
            service.RegisterKill();

            service.Reset();

            Assert.That(service.Combo, Is.EqualTo(0));
        }

        [Test]
        public void Reset_AfterKills_RaisesComboChangedWithZero()
        {
            ComboService service = CreateService(DefaultMaxMultiplier);
            service.RegisterKill();
            List<int> reported = new List<int>();
            service.ComboChanged += combo => reported.Add(combo);

            service.Reset();

            Assert.That(reported, Is.EqualTo(new[] { 0 }));
        }

        [Test]
        public void Reset_WhenComboIsZero_DoesNotRaiseComboChanged()
        {
            ComboService service = CreateService(DefaultMaxMultiplier);
            bool raised = false;
            service.ComboChanged += _ => raised = true;

            service.Reset();

            Assert.That(raised, Is.False);
        }

        [Test]
        public void RegisterKill_AfterReset_RestartsChainAtOne()
        {
            ComboService service = CreateService(DefaultMaxMultiplier);
            service.RegisterKill();
            service.RegisterKill();
            service.Reset();

            int multiplier = service.RegisterKill();

            Assert.That(multiplier, Is.EqualTo(1));
        }

        [Test]
        public void RegisterKill_MaxMultiplierConfiguredBelowOne_ClampsMultiplierToOne()
        {
            ComboService service = CreateService(0);

            service.RegisterKill();
            int multiplier = service.RegisterKill();

            Assert.That(multiplier, Is.EqualTo(1));
        }

        [Test]
        public void RegisterKill_NullComboData_FallsBackToDefaultCapOfEight()
        {
            // A missing config block must not break scoring; the service falls back to hardcoded defaults.
            _combatConfig.Combo = null;
            ComboService service = new ComboService(_gameSettings, _pauseService);

            int multiplier = 0;
            for (int i = 0; i < 10; i++)
                multiplier = service.RegisterKill();

            Assert.That(multiplier, Is.EqualTo(8));
        }

        private ComboService CreateService(int maxMultiplier)
        {
            // A wide window guarantees same-frame kills chain; expiry itself needs real time and is not tested here.
            _combatConfig.Combo = new ComboData { Window = 100f, MaxMultiplier = maxMultiplier };

            return new ComboService(_gameSettings, _pauseService);
        }

        private static void AssignCombatConfig(GameSettings settings, CombatConfig combat)
        {
            // _combat is [SerializeField] private and normally wired in the Inspector; SerializedObject is the test-side equivalent.
            using (SerializedObject serialized = new SerializedObject(settings))
            {
                serialized.FindProperty("_combat").objectReferenceValue = combat;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
