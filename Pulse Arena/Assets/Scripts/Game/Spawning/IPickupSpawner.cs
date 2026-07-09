namespace Game.Spawning
{
    using System;
    using UnityEngine;

    public interface IPickupSpawner
    {
        event Action<string, float> RarePickupSpawned;

        void Initialize(Transform[] spawnPoints, Transform spawnParent, float spawnHeightOffset);
        void StartSpawn();
        void StopSpawn();
    }
}