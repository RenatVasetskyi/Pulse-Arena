using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "Level Config", menuName = "Sling Ring/Configs/Level Config")]
    public class LevelConfig : ScriptableObject
    {
        public SpawnData Spawn;
        public SpawnAreaData SpawnArea = new();
        public TurretData Turret = new();

        [Tooltip("The linear campaign — ordered levels the player unlocks one by one. The legacy single Waves set below is Level 1's fallback until every level is authored here.")]
        public LevelDefinition[] Levels;

        [Tooltip("Difficulty-scaling knobs shared by every Survival (IsEndless) level.")]
        public SurvivalData Survival = new();

        [Tooltip("Health-ratio thresholds that map a campaign clear to a 1..3 star reward.")]
        public StarThresholdData StarThresholds = new();
        public WaveData[] Waves;
        public PickupData Pickup;
        public PoolData Pool = new();
    }
}