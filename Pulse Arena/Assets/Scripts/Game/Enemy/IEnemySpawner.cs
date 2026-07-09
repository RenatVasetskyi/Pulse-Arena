namespace Game.Enemy
{
    using System;
    using UnityEngine;

    public interface IEnemySpawner
    {
        event Action AllWavesCleared;
        event Action<int, int> WaveChanged;

        void Initialize(Transform target, Transform[] spawnPoints, Transform spawnParent, float spawnHeightOffset);
        void StartSpawn();
        void StopSpawn();
    }
}