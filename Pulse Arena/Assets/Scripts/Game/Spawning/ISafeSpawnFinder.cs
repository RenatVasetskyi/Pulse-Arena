using Data;
using UnityEngine;

namespace Game.Spawning
{
    /// <summary>
    ///     Finds a clear spawn point in the play ring for enemies and pickups: samples random ring spots and
    ///     rejects any too close to the player or whose clearance sphere overlaps a blocker (wall, pit, pickup or
    ///     enemy). Returns false if no clear spot is found within its try budget; spawn cadence lives in the spawners.
    /// </summary>
    public interface ISafeSpawnFinder
    {
        void Initialize(Vector3 center, Transform player, SpawnAreaData area, LayerMask blockerMask);
        bool TryFind(out Vector3 position);
    }
}
