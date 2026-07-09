using Game.Enemy;
using UnityEngine;

namespace Game.Combat.Interfaces
{
    /// <summary>
    ///     Finds the nearest grabbable enemy around the slingshot: an overlap-sphere query filtered by
    ///     grabbed/dead state and a line-of-sight check against obstacles, returning the closest survivor.
    /// </summary>
    public interface IEnemyTargetFinder
    {
        EnemyController FindNearest(float radius);
        void Initialize(Transform origin, Data.SlingshotData data);
    }
}