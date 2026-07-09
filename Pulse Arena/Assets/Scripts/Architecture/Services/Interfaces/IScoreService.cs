using System;

namespace Architecture.Services.Interfaces
{
    public interface IScoreService
    {
        event Action<int> ScoreChanged;
        int Score { get; }

        void Add(int value);
        void Reset();
    }
}