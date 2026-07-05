using Data;
using UnityEngine;

namespace Game.Enemy.Interfaces
{
    public interface IEnemyFactory
    {
        void Preload();
        EnemyController Create(Vector3 at, Quaternion rotation, Transform parent, Transform target,
            EnemyTypeData typeData = null);
    }
}
