using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "Player Config", menuName = "Sling Ring/Configs/Player Config")]
    public class PlayerConfig : ScriptableObject
    {
        public PlayerData Data;
        public PlayerVisualData Visuals = new();
    }
}