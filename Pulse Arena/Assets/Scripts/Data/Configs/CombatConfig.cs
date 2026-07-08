using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "Combat Config", menuName = "Pulse Arena/Configs/Combat Config")]
    public class CombatConfig : ScriptableObject
    {
        public SlingshotData Slingshot;
        public ComboData Combo = new();
        public SlowMoData SlowMo = new();
        public PitData Pit = new();
        public SuperData Super = new();
        public FeelData Feel = new();
    }
}
