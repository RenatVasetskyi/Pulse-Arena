namespace Game.Enemy
{
    using System;
    using UnityEngine;

    public interface IEnemySpawner
    {
        event Action<int, int> WaveChanged;
        event Action AllWavesCleared;

        void Initialize(Transform target, Transform[] spawnPoints, Transform spawnParent, float spawnHeightOffset);
        void StartSpawn();
        void StopSpawn();
    }
}
