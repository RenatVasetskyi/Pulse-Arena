using Data;
using UnityEngine;

namespace Game.Combat.Interfaces
{
    /// <summary>
    ///     Drives the on-ground hook-target marker: while the lasso is idle it highlights the nearest enemy
    ///     (green when grabbable, grey when out of range / on cooldown) and hides it otherwise.
    /// </summary>
    public interface IHookTargetMarkerPresenter
    {
        void Initialize(Transform parent, SlingshotData data, GameObject prefab, IEnemyTargetFinder finder);
        void Tick(bool lassoIdle, bool onCooldown);
    }
}