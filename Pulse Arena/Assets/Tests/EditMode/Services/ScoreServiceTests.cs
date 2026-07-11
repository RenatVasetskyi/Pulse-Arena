using System.Collections.Generic;
using Architecture.Services;
using NUnit.Framework;

namespace PulseArena.Tests.EditMode.Services
{
    /// <summary>
    ///     Verifies <see cref="ScoreService" /> — the running match-score accumulator. Tests drive the
    ///     public <c>Add</c>/<c>Reset</c> API and assert both the <c>Score</c> property and the
    ///     <c>ScoreChanged</c> event contract (the event always carries the new running total).
    /// </summary>
    [TestFixture]
    public class ScoreServiceTests
    {
        private ScoreService _scoreService;

        [SetUp]
        public void SetUp()
        {
            _scoreService = new ScoreService();
        }

        [Test]
        public void Score_BeforeAnyAdd_IsZero()
        {
            Assert.That(_scoreService.Score, Is.EqualTo(0));
        }

        // The negative case locks in the current contract: the raw delta is applied, no clamping.
        [TestCase(10)]
        [TestCase(0)]
        [TestCase(-5)]
        public void Add_FromZero_SetsScoreToValue(int value)
        {
            _scoreService.Add(value);

            Assert.That(_scoreService.Score, Is.EqualTo(value));
        }

        [Test]
        public void Add_AccumulatesAcrossCalls()
        {
            _scoreService.Add(10);
            _scoreService.Add(5);

            Assert.That(_scoreService.Score, Is.EqualTo(15));
        }

        [Test]
        public void Add_RaisesScoreChangedWithRunningTotal()
        {
            List<int> totals = new List<int>();
            _scoreService.ScoreChanged += total => totals.Add(total);

            _scoreService.Add(10);
            _scoreService.Add(5);

            Assert.That(totals, Is.EqualTo(new List<int> { 10, 15 }));
        }

        [Test]
        public void Reset_SetsScoreToZero()
        {
            _scoreService.Add(42);

            _scoreService.Reset();

            Assert.That(_scoreService.Score, Is.EqualTo(0));
        }

        [Test]
        public void Reset_RaisesScoreChangedWithZero()
        {
            _scoreService.Add(42);
            List<int> totals = new List<int>();
            _scoreService.ScoreChanged += total => totals.Add(total);

            _scoreService.Reset();

            Assert.That(totals, Is.EqualTo(new List<int> { 0 }));
        }
    }
}
