using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "Enemy Config", menuName = "Sling Ring/Configs/Enemy Config")]
    public class EnemyConfig : ScriptableObject
    {
        public EnemyData Data;
        public EnemyTypeData[] Types = { new EnemyTypeData() };
    }
}