using UnityEngine;

namespace Game.Spawning
{
    /// <summary>
    ///     Shared placement geometry for the spawn finders. Holds the flat-plane distance check that
    ///     <see cref="SafeSpawnFinder" /> and <see cref="PitPlacementFinder" /> used to carry as byte-identical
    ///     private copies.
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
