using Data;
using UnityEngine;

namespace Game.Spawning
{
    /// <summary>
    /// Finds a clear spawn point for a pit: samples the play ring and rejects candidates too close to the
    /// player or overlapping any enemy, so a pit never opens right on top of someone. Returns false if no
    /// clear spot is found within its try budget.
    /// </summary>
    public interface IPitPlacementFinder
    {
        void Initialize(Vector3 center, Transform player, PitData data, LayerMask enemyLayer);
        bool TryFind(float scale, out Vector3 position);
    }
}
