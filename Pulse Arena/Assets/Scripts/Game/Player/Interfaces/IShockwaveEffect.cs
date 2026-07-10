using Data;
using UnityEngine;

namespace Game.Player.Interfaces
{
    /// <summary>
    ///     The ultimate's ground shockwave VFX: an expanding ring of particles kicked out at the player's feet
    ///     when the ultimate fires. Built procedurally on first use (the visual counterpart to the gameplay
    ///     fling in <see cref="PlayerUltimate" />, mirroring <c>ISnapBurstEffect</c>).
    /// </summary>
    public interface IShockwaveEffect
    {
        void Initialize(Transform parent, SuperData data);
        void Play(Vector3 position);
    }
}
