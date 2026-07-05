using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "Combat Config", menuName = "Pulse Arena/Configs/Combat Config")]
    public class CombatConfig : ScriptableObject
    {
        public SlingshotData Slingshot;
    }
}
