using UnityEngine;

namespace Game.Spawning
{
    public interface IPitSpawner
    {
        void Initialize(Vector3 center, Transform player, Transform parent);
        void StartSpawn();
        void StopSpawn();
    }
}
