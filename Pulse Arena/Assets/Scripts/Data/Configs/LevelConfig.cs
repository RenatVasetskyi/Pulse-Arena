using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "Level Config", menuName = "Pulse Arena/Configs/Level Config")]
    public class LevelConfig : ScriptableObject
    {
        public SpawnData Spawn;
        public SpawnAreaData SpawnArea = new();
        public WaveData[] Waves;
        public PickupData Pickup;
        public PoolData Pool = new();
    }
}