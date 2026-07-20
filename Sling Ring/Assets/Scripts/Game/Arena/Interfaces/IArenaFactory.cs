using UnityEngine;

namespace Game.Arena.Interfaces
{
    /// <summary>
    ///     Creates the battle arena (environment geometry, baked NavMesh, spawn points, parents and the
    ///     battle camera) from a prefab instead of it living in the scene.
    /// </summary>
    public interface IArenaFactory
    {
        GameObject Create();
    }
}