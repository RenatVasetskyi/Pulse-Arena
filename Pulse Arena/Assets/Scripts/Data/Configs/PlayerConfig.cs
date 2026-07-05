using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "Player Config", menuName = "Pulse Arena/Configs/Player Config")]
    public class PlayerConfig : ScriptableObject
    {
        public PlayerData Data;
        public PlayerVisualData Visuals = new();
    }
}
