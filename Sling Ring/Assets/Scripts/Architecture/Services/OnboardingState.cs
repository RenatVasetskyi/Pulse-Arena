using Architecture.Services.Interfaces;
using UnityEngine;

namespace Architecture.Services
{
    /// <summary>
    ///     <see cref="IOnboardingState" /> backed by <see cref="PlayerPrefs" />: a single seen-flag. Plain C#,
    ///     ProjectContext-scoped so the flag is shared across the menu + match scenes and survives app restarts.
    /// </summary>
    public class OnboardingState : IOnboardingState
    {
        private const string OnboardingKey = "SlingRing.Onboarding.Seen";

        public bool OnboardingSeen => PlayerPrefs.GetInt(OnboardingKey, 0) == 1;

        public void MarkOnboardingSeen()
        {
            PlayerPrefs.SetInt(OnboardingKey, 1);
            PlayerPrefs.Save();
        }
    }
}
