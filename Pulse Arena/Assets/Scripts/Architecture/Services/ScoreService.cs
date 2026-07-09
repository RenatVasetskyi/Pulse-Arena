using System;
using Architecture.Services.Interfaces;

namespace Architecture.Services
{
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