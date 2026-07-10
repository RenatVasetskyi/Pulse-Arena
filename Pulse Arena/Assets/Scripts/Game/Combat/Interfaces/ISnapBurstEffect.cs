using System;
using Data;
using UnityEngine;

namespace Game.Combat.Interfaces
{
    /// <summary>
    ///     The rope-snap particle burst: lazily builds a procedural <see cref="ParticleSystem" /> from
    ///     <see cref="VfxData" /> and emits it at a world position when the rope breaks.
    /// </summary>
    public interface ISnapBurstEffect
    {
        void Initialize(Transform parent, VfxData vfx, SlingshotData data, Func<Material> materialProvider);
        void Play(Vector3 position);

        // Mechanical pause: freeze / resume a mid-flight burst.
        void PauseEffect();
        void ResumeEffect();
    }
}