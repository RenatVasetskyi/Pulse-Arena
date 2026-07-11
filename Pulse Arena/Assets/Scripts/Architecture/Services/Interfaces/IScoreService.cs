using System;

namespace Architecture.Services.Interfaces
{
    /// <summary>
    ///     The run-score contract: gameplay adds points, UI observes <see cref="ScoreChanged" /> (carrying
    ///     the new total) to re-render. One score per run — <see cref="Reset" /> is called on match start.
    /// </summary>
    public interface IScoreService
    {
        event Action<int> ScoreChanged;
        int Score { get; }

        void Add(int value);
        void Reset();
    }
}