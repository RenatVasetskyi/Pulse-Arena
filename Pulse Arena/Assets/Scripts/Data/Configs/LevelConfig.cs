using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "Level Config", menuName = "Pulse Arena/Configs/Level Config")]
    public class LevelConfig : ScriptableObject
    {
        public SpawnData Spawn;
        public WaveData[] Waves;
        public PickupData Pickup;
        public PoolData Pool = new();
    }
}
