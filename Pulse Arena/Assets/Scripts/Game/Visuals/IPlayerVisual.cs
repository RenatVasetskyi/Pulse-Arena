using Data;
using Game.Combat;
using UnityEngine;

namespace Game.Visuals
{
    /// <summary>
    ///     The player's swappable visual seam. <c>PlayerController</c> drives the character through this interface so
    ///     the body can be a procedural primitive (<see cref="PlayerPrimitiveVisual" />) or a skinned/animated model
    ///     (<see cref="CowboyVisual" />) without the controller knowing which. Implementations own the HOW of idle/
    ///     move/hit/death; the controller only signals lifecycle + the hit/death events + mechanical pause.
    /// </summary>
    public interface IPlayerVisual
    {
        void Initialize(Rigidbody rigidbody, EnemySlingshot slingshot, PlayerVisualData visualData);

        void SetPaused(bool value);

        void PlayHit();

        void PlayDash(float dashDuration);

        void PlayDeath();
    }
}
