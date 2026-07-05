using System;
using UnityEngine;

namespace Data
{
    public enum EnemyTypeId
    {
        Standard = 0,
        Light = 1,
        Heavy = 2,
        Fast = 3,
        Spiky = 4
    }

    [Serializable]
    public class EnemyTypeData
    {
        private static EnemyTypeData _default;

        public static EnemyTypeData Default => _default ??= new EnemyTypeData();

        public EnemyTypeId Id = EnemyTypeId.Standard;

        [Header("Spawning")]
        [Min(0f)] public float SpawnWeight = 1f;

        [Header("Stats")]
        [Min(0.1f)] public float HealthMultiplier = 1f;
        [Min(0.1f)] public float MoveSpeedMultiplier = 1f;
        [Min(0.1f)] public float ScoreMultiplier = 1f;

        [Header("Slingshot Feel")]
        [Min(0.1f)] public float Weight = 1f;
        [Min(0.1f)] public float TensionRateMultiplier = 1f;
        [Min(0.1f)] public float LaunchVelocityMultiplier = 1f;
        [Min(0f)] public float ImpactDamageMultiplier = 1f;
        [Min(0f)] public float ImpactKnockbackMultiplier = 1f;

        [Header("Spiky")]
        public bool DamagesPlayerWhileHeld;
        [Min(0.1f)] public float HeldDamageInterval = 0.75f;

        [Header("Visuals")]
        public bool OverrideBodyColor;
        public Color BodyColor = new(0.42f, 0.2f, 0.72f, 1f);
        [Min(0.1f)] public float VisualScale = 1f;
        public bool ShowSpikes = true;
    }
}
