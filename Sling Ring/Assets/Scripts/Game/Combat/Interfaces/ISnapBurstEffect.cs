using UnityEngine;

namespace Game.Combat.Interfaces
{
    /// <summary>
    ///     The rope-snap particle burst: spawns the authored one-shot burst prefab and triggers it at a world
    ///     position when the rope breaks. The effect is defined entirely by the prefab (look, material and the
    ///     emission burst that says how many particles a break throws).
    /// </summary>
    public interface ISnapBurstEffect
    {
        void Initialize(Transform parent, GameObject burstPrefab);
        void Play(Vector3 position);

        // Mechanical pause: freeze / resume a mid-flight burst.
        void PauseEffect();
        void ResumeEffect();
    }
}
