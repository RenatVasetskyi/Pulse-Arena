namespace Game.Common
{
    /// <summary>
    ///     Maps an end-of-level health ratio to a 1..3 star reward. Pure function (no Unity state), so the win
    ///     flow reads its thresholds from config and this stays unit-testable.
    /// </summary>
    public static class StarRating
    {
        /// <summary>
        ///     Stars for finishing with <paramref name="health" /> of <paramref name="maxHealth" />: 3 at or above
        ///     <paramref name="threeStarRatio" />, 2 at or above <paramref name="twoStarRatio" />, otherwise 1.
        ///     Returns 1 when <paramref name="maxHealth" /> is non-positive (no ratio to measure).
        /// </summary>
        public static int Compute(int health, int maxHealth, float threeStarRatio, float twoStarRatio)
        {
            if (maxHealth <= 0)
                return 1;

            float ratio = health / (float)maxHealth;

            if (ratio >= threeStarRatio)
                return 3;

            return ratio >= twoStarRatio ? 2 : 1;
        }
    }
}
