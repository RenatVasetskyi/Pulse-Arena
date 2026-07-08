using System;
using UnityEngine;

namespace Data
{
    /// <summary>
    /// Shared game-feel values that apply to BOTH actors (player + enemy), so they can never silently drift
    /// apart. Lives in the cross-actor <c>Combat Config</c> SO and is reached via <c>GameSettings.Feel</c>.
    /// </summary>
    [Serializable]
    public class FeelData
    {
        public float RingoutHeight = -2.5f;
        public float HitFlashDuration = 0.12f;
        public Color HitFlashColor = new(1f, 0.08f, 0.03f, 1f);
    }
}
