namespace Architecture.Services.Interfaces
{
    /// <summary>
    ///     Tracks whether the first-run onboarding hints have already been shown (PlayerPrefs-backed), so they
    ///     never repeat across sessions. Kept separate from campaign progression — a tutorial-seen flag is a
    ///     tutorial concern, not a level-unlock one.
    /// </summary>
    public interface IOnboardingState
    {
        /// <summary>True once the first-run onboarding hints have been shown.</summary>
        bool OnboardingSeen { get; }

        /// <summary>Records that the onboarding has run (persisted) — call once the player has learned the core loop.</summary>
        void MarkOnboardingSeen();
    }
}
