using Data;
using UnityEngine;

namespace Game.Enemy.Interfaces
{
    public interface IEnemyFactory
    {
        void Clear();

        EnemyController Create(Vector3 at, Quaternion rotation, Transform parent, Transform target,
            EnemyTypeData typeData = null);

        void Preload();
    }
}