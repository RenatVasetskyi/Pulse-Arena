using System;
using UnityEngine;

namespace Game.Enemy.Interfaces
{
    public interface IEnemySpawner
    {
        event Action AllWavesCleared;
        event Action<int, int> WaveChanged;
        event Action WaveCleared;

        void Initialize(Transform target, Vector3 center, Transform spawnParent, float spawnHeightOffset);
        void StartSpawn();
        void StopSpawn();
    }
}
