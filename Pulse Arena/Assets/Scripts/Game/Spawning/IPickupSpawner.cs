namespace Game.Spawning
{
    using UnityEngine;

    public interface IPickupSpawner
    {
        void Initialize(Transform[] spawnPoints, Transform spawnParent, float spawnHeightOffset);
        void StartSpawn();
        void StopSpawn();
    }
}
