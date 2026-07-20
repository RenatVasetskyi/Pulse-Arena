using System;
using Architecture.Services.Interfaces;

namespace Architecture.Services
{
    /// <summary>
    ///     Global run-score accumulator (ProjectContext singleton). Kill/combo systems push points through
    ///     <see cref="Add" />; the HUD score view re-renders from <see cref="ScoreChanged" />. Reset once per
    ///     match start so scores never leak across runs.
    /// </summary>
    public class ScoreService : IScoreService
    {
        public event Action<int> ScoreChanged;

        public int Score { get; private set; }

        public void Add(int value)
        {
            Score += value;
            ScoreChanged?.Invoke(Score);
        }

        public void Reset()
        {
            Score = 0;
            ScoreChanged?.Invoke(Score);
        }
    }
}