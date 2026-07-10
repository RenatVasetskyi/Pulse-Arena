using Data;
using UnityEngine;

namespace Game.Spawning
{
    /// <summary>
    ///     Finds a clear spawn point inside the play ring for enemies and pickups: samples random spots in a ring
    ///     around the arena center and rejects any that sit too close to the player or whose clearance sphere
    ///     overlaps a blocker (wall/box, suck-hole pit, another pickup or an enemy). Returns false when no clear
    ///     spot is found within its try budget. Keeps the placement rules as one focused, reusable unit — the
    ///     spawn cadence lives in the spawners.
    /// </summary>
    public interface ISafeSpawnFinder
    {
        void Initialize(Vector3 center, Transform player, SpawnAreaData area, LayerMask blockerMask);
        bool TryFind(out Vector3 position);
    }
}
