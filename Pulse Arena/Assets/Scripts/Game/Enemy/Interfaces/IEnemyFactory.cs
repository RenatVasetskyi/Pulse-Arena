using UnityEngine;

namespace Game.Enemy.Interfaces
{
    public interface IEnemyFactory
    {
        EnemyController Create(Vector3 at, Quaternion rotation, Transform parent, Transform target);
    }
}
