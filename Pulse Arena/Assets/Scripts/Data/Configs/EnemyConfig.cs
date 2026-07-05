using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "Enemy Config", menuName = "Pulse Arena/Configs/Enemy Config")]
    public class EnemyConfig : ScriptableObject
    {
        public EnemyData Data;
        public EnemyTypeData[] Types = { new EnemyTypeData() };
        public EnemyVisualData Visuals = new();
    }
}
