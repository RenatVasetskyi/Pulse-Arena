using UnityEngine;

namespace Game.Turrets
{
    public interface ITurretSpawner
    {
        void Initialize(Vector3 center, Transform player, Transform parent);
        void StartSpawn();
        void StopSpawn();
    }
}
