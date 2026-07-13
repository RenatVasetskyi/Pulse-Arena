using System;
using UnityEngine;

namespace Data
{
    /// <summary>
    ///     One playable level in the linear campaign: its display name, its wave script, and per-level difficulty
    ///     knobs. <see cref="LevelConfig.Levels" /> holds the ordered list; the player unlocks the next level by
    ///     beating the current one (persisted by <c>ILevelProgressService</c>). At match build the spawners read
    ///     the SELECTED level's waves + toggles (via <c>ILevelService</c>) instead of a single global wave set.
    /// </summary>
    [Serializable]
    public class LevelDefinition
    {
        [Tooltip("Shown on the level-select button (e.g. \"Level 1\", \"The Gauntlet\").")]
        public string DisplayName = "Level";

        [Tooltip("Survival mode: ignore Waves and spawn endlessly with escalating difficulty until the player dies. No win condition.")]
        public bool IsEndless;

        [Tooltip("Max enemies alive at once during this level — later levels raise the pressure.")]
        [Min(1)] public int MaxEnemiesOnScreen = 8;

        [Tooltip("Whether arena pits open during this level (later levels add hazards).")]
        public bool EnablePits = true;

        [Tooltip("Whether hazard turrets spawn during this level.")]
        public bool EnableTurrets = true;

        [Tooltip("The wave script — each wave lists its enemy mix. Clearing the last wave completes the level.")]
        public WaveData[] Waves;
    }
}
