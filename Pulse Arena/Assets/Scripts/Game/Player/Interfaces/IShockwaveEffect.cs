using UnityEngine;

namespace Game.Player.Interfaces
{
    /// <summary>
    ///     The ultimate's ground shockwave VFX: an expanding ring of particles kicked out at the player's feet
    ///     when the ultimate fires. Spawns an authored prefab on first use and triggers it (the visual counterpart
    ///     to the gameplay fling in <see cref="PlayerUltimate" />, mirroring <c>ISnapBurstEffect</c>).
    /// </summary>
    public interface IShockwaveEffect
    {
        void Initialize(Transform parent, GameObject ringPrefab);
        void Play(Vector3 position);
    }
}
