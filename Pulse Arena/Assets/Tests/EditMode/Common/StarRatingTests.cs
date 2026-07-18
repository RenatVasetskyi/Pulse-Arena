using Game.Common;
using NUnit.Framework;

namespace PulseArena.Tests.EditMode.Common
{
    /// <summary>
    ///     Verifies <see cref="StarRating.Compute" /> — the pure end-of-level health-ratio to 1..3 star mapping
    ///     extracted from the win flow. Tests lock the three thresholds and the non-positive-maxHealth guard.
    /// </summary>
    [TestFixture]
    public class StarRatingTests
    {
        private const float ThreeStar = 0.999f;
        private const float TwoStar = 0.5f;

        [Test]
        public void FullHealth_IsThreeStars()
        {
            Assert.That(StarRating.Compute(10, 10, ThreeStar, TwoStar), Is.EqualTo(3));
        }

        // 9/10 = 0.9: below the 3-star ratio but at/above the 2-star ratio.
        [Test]
        public void BelowThreeStarButAboveHalf_IsTwoStars()
        {
            Assert.That(StarRating.Compute(9, 10, ThreeStar, TwoStar), Is.EqualTo(2));
        }

        // Boundary: exactly half must still earn 2 stars (the check is >=, not >).
        [Test]
        public void ExactlyHalf_IsTwoStars()
        {
            Assert.That(StarRating.Compute(5, 10, ThreeStar, TwoStar), Is.EqualTo(2));
        }

        [Test]
        public void JustBelowHalf_IsOneStar()
        {
            Assert.That(StarRating.Compute(4, 10, ThreeStar, TwoStar), Is.EqualTo(1));
        }

        [Test]
        public void NoHealthLeft_IsOneStar()
        {
            Assert.That(StarRating.Compute(0, 10, ThreeStar, TwoStar), Is.EqualTo(1));
        }

        // Guard: no ratio to measure, so the lowest reward rather than a divide-by-zero.
        [TestCase(0, 0)]
        [TestCase(5, -1)]
        public void NonPositiveMaxHealth_IsOneStar(int health, int maxHealth)
        {
            Assert.That(StarRating.Compute(health, maxHealth, ThreeStar, TwoStar), Is.EqualTo(1));
        }
    }
}
