using UnityEngine;

namespace Data
{
    /// <summary>
    ///     Combat-wide tuning asset: slingshot/lasso feel, combo windows, bullet-time, pits, super meter and
    ///     the shared cross-actor game-feel (<see cref="FeelData" /> lives here once so player and enemy can't
    ///     drift). One of the five sub-configs referenced by the <see cref="GameSettings" /> facade.
    /// </summary>
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