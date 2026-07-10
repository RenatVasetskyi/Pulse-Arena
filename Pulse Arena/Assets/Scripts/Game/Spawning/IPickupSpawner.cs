namespace Game.Spawning
{
    using System;
    using UnityEngine;

    public interface IPickupSpawner
    {
        event Action<string, float> RarePickupSpawned;

        void Initialize(Vector3 center, Transform player, Transform spawnParent, float spawnHeightOffset);
        void StartSpawn();
        void StopSpawn();
    }
}