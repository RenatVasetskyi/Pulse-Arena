using System;
using UnityEngine;

namespace Data
{
    /// <summary>
    ///     Shared game-feel values that apply to BOTH actors (player + enemy), so they can never silently drift
    ///     apart. Lives in the cross-actor <c>Combat Config</c> SO and is reached via <c>GameSettings.Feel</c>.
    /// </summary>
    [Serializable]
    public class FeelData
    {
        public float RingoutHeight = -2.5f;
        public float HitFlashDuration = 0.12f;

        [Tooltip("Authored flat material swapped onto an actor's renderers on hit (the flash colour lives on the asset).")]
        public Material HitFlashMaterial;
    }
}