using Architecture.Services;
using NUnit.Framework;
using UnityEngine;

namespace SlingRing.Tests.EditMode.Services
{
    /// <summary>
    ///     Verifies <see cref="LevelProgressService" /> — the PlayerPrefs-backed campaign persistence: the unlock
    ///     frontier (only a frontier clear advances it), monotonic per-level star saves, and the survival
    ///     high-score. Each test runs on a clean store so nothing leaks across runs or into the editor's prefs.
    /// </summary>
    [TestFixture]
    public class LevelProgressServiceTests
    {
        private LevelProgressService _progress;

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteAll();
            _progress = new LevelProgressService();
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteAll();
        }

        [Test]
        public void FreshInstall_OnlyTheFirstLevelIsUnlocked()
        {
            Assert.That(_progress.HighestUnlockedIndex, Is.EqualTo(0));
            Assert.That(_progress.IsUnlocked(0), Is.True);
            Assert.That(_progress.IsUnlocked(1), Is.False);
        }

        [Test]
        public void CompletingTheFrontier_UnlocksTheNextLevel_AndReportsIt()
        {
            bool newUnlock = _progress.Complete(0, 3);

            Assert.That(newUnlock, Is.True, "clearing the frontier should report a fresh unlock");
            Assert.That(_progress.HighestUnlockedIndex, Is.EqualTo(1));
            Assert.That(_progress.IsUnlocked(1), Is.True);
        }

        [Test]
        public void ReplayingAClearedLevel_DoesNotAdvanceTheFrontier()
        {
            _progress.Complete(0, 3); // frontier advances to 1

            bool newUnlock = _progress.Complete(0, 3); // replay the old level

            Assert.That(newUnlock, Is.False, "replaying a cleared level must not re-unlock anything");
            Assert.That(_progress.HighestUnlockedIndex, Is.EqualTo(1));
        }

        [Test]
        public void Stars_KeepTheBest_AndNeverRegressOnAWorseReplay()
        {
            _progress.Complete(0, 3);
            _progress.Complete(0, 1);

            Assert.That(_progress.GetStars(0), Is.EqualTo(3), "a worse replay must not lower the recorded stars");
        }

        [Test]
        public void Stars_AreClampedToThree()
        {
            _progress.Complete(0, 99);

            Assert.That(_progress.GetStars(0), Is.EqualTo(3));
        }

        [Test]
        public void SubmitSurvivalScore_BanksOnlyANewRecord()
        {
            Assert.That(_progress.SubmitSurvivalScore(100), Is.True);
            Assert.That(_progress.GetSurvivalBest(), Is.EqualTo(100));

            Assert.That(_progress.SubmitSurvivalScore(50), Is.False, "a lower score is not a record");
            Assert.That(_progress.GetSurvivalBest(), Is.EqualTo(100));

            Assert.That(_progress.SubmitSurvivalScore(150), Is.True);
            Assert.That(_progress.GetSurvivalBest(), Is.EqualTo(150));
        }

        [Test]
        public void Complete_WithANegativeIndex_IsANoOp()
        {
            bool newUnlock = _progress.Complete(-1, 3);

            Assert.That(newUnlock, Is.False);
            Assert.That(_progress.HighestUnlockedIndex, Is.EqualTo(0));
        }
    }
}
