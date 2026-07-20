using UnityEngine;

namespace Game.Spawning
{
    /// <summary>
    ///     Shared placement geometry for the spawn finders: the flat-plane distance check used by
    ///     <see cref="SafeSpawnFinder" /> and <see cref="PitPlacementFinder" />.
    /// </summary>
    public static class SpawnGeometry
    {
        /// <summary>Distance between two points ignoring height (Y), so spawn clearance is measured on the flat arena plane.</summary>
        public static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;

            return Vector3.Distance(a, b);
        }
    }
}
