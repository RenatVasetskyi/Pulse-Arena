using Architecture.Services;
using NUnit.Framework;
using UnityEngine;

namespace PulseArena.Tests.EditMode.Services
{
    /// <summary>
    ///     Verifies <see cref="OnboardingState" /> — the PlayerPrefs-backed first-run seen-flag split out of the
    ///     progress service. Each test starts and ends on a clean store so no key leaks across runs or into the
    ///     editor's real PlayerPrefs.
    /// </summary>
    [TestFixture]
    public class OnboardingStateTests
    {
        private OnboardingState _onboarding;

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteAll();
            _onboarding = new OnboardingState();
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteAll();
        }

        [Test]
        public void OnboardingSeen_OnAFreshInstall_IsFalse()
        {
            Assert.That(_onboarding.OnboardingSeen, Is.False);
        }

        [Test]
        public void MarkOnboardingSeen_FlipsSeenToTrue()
        {
            _onboarding.MarkOnboardingSeen();

            Assert.That(_onboarding.OnboardingSeen, Is.True);
        }

        // The whole point of the flag is that it survives — a new instance (a later session) reads the same store.
        [Test]
        public void OnboardingSeen_PersistsAcrossInstances()
        {
            _onboarding.MarkOnboardingSeen();

            OnboardingState reloaded = new OnboardingState();

            Assert.That(reloaded.OnboardingSeen, Is.True);
        }
    }
}
