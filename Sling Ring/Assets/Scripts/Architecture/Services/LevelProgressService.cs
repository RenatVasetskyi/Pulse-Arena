using Architecture.Services.Interfaces;
using UnityEngine;

namespace Architecture.Services
{
    /// <summary>
    ///     <see cref="ILevelProgressService" /> backed by <see cref="PlayerPrefs" />: one int for the unlock frontier
    ///     plus one int per level for its best stars. Plain C#, ProjectContext-scoped so progress is shared across the
    ///     menu + match scenes and survives app restarts.
    /// </summary>
    public class LevelProgressService : ILevelProgressService
    {
        private const string UnlockedKey = "SlingRing.Levels.HighestUnlocked";
        private const string StarsKeyPrefix = "SlingRing.Levels.Stars.";
        private const string SurvivalBestKey = "SlingRing.Survival.Best";

        public int HighestUnlockedIndex => Mathf.Max(0, PlayerPrefs.GetInt(UnlockedKey, 0));

        public bool IsUnlocked(int levelIndex)
        {
            return levelIndex >= 0 && levelIndex <= HighestUnlockedIndex;
        }

        public int GetStars(int levelIndex)
        {
            return levelIndex < 0 ? 0 : PlayerPrefs.GetInt(StarsKeyPrefix + levelIndex, 0);
        }

        public bool Complete(int levelIndex, int stars)
        {
            if (levelIndex < 0)
                return false;

            SaveBestStars(levelIndex, stars);

            // Only the frontier clear moves the unlock line — replaying an old level never re-unlocks anything.
            if (levelIndex != HighestUnlockedIndex)
                return false;

            PlayerPrefs.SetInt(UnlockedKey, levelIndex + 1);
            PlayerPrefs.Save();
            return true;
        }

        public int GetSurvivalBest()
        {
            return Mathf.Max(0, PlayerPrefs.GetInt(SurvivalBestKey, 0));
        }

        public bool SubmitSurvivalScore(int score)
        {
            if (score <= GetSurvivalBest())
                return false;

            PlayerPrefs.SetInt(SurvivalBestKey, score);
            PlayerPrefs.Save();
            return true;
        }

        // DeleteAll wipes the whole store — unlock frontier, all stars, survival best, onboarding flag — a
        // clean-slate reset behind a menu confirmation; afterward the game reads first-run defaults.
        public void ResetProgress()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }

        private void SaveBestStars(int levelIndex, int stars)
        {
            int clamped = Mathf.Clamp(stars, 0, 3);

            if (clamped <= GetStars(levelIndex))
                return;

            PlayerPrefs.SetInt(StarsKeyPrefix + levelIndex, clamped);
            PlayerPrefs.Save();
        }
    }
}
