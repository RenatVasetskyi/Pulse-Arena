namespace Game.Enemy
{
    using UnityEngine;

    public interface IEnemySpawner
    {
        void Initialize(Transform target, Transform[] spawnPoints, Transform spawnParent, float spawnHeightOffset);
        void StartSpawn();
        void StopSpawn();
    }
}
