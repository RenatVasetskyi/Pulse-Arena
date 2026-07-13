namespace Architecture.Services.Interfaces
{
    /// <summary>
    ///     Persists linear-campaign progress (PlayerPrefs-backed): which levels are unlocked and each level's best
    ///     star rating. Level 0 is always unlocked; clearing level N unlocks N+1. Consumed by the level-select UI
    ///     (to lock/star buttons) and the win flow (to record a completion + detect a fresh unlock).
    /// </summary>
    public interface ILevelProgressService
    {
        /// <summary>Highest level index the player has unlocked (0 = only the first level).</summary>
        int HighestUnlockedIndex { get; }

        bool IsUnlocked(int levelIndex);

        /// <summary>Best star rating (0..3) the player has earned on this level; 0 if never cleared.</summary>
        int GetStars(int levelIndex);

        /// <summary>
        ///     Records a clear: keeps the best star count and, if this was the frontier level, unlocks the next one.
        ///     Returns true when a NEW level was just unlocked, so the win screen can highlight it.
        /// </summary>
        bool Complete(int levelIndex, int stars);

        void ResetProgress();
    }
}
