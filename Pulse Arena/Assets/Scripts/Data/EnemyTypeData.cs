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

    /// <summary>
    ///     Which combat "brain" an enemy type runs — its pursue/attack archetype, mapped to an
    ///     <c>IEnemyBehavior</c> by <c>EnemyBehaviorFactory</c>. The universal reactions (grab, knockback, ringout,
    ///     death) are shared by every enemy and are NOT selected here; only the offense archetype varies. Add a value
    ///     + a factory case + an <c>IEnemyBehavior</c> impl to give a new enemy its own behaviour.
    /// </summary>
    public enum EnemyBehaviorId
    {
        MeleeChaser = 0
    }

    [Serializable]
    public class EnemyTypeData
    {
        private static EnemyTypeData _default;

        public EnemyTypeId Id = EnemyTypeId.Standard;

        [Tooltip("The self-contained enemy prefab for this type — its own model, Animator, controller and scale. " +
                 "Null falls back to the shared GameSettings.Prefabs.EnemyPrefab.")]
        public GameObject Prefab;

        [Header("Behavior")]
        [Tooltip("Which pursue/attack brain this enemy runs (EnemyBehaviorFactory maps it to an IEnemyBehavior). " +
                 "MeleeChaser = the default run-in-and-swing archetype.")]
        public EnemyBehaviorId Behavior = EnemyBehaviorId.MeleeChaser;

        [Header("Spawning")] [Min(0f)] public float SpawnWeight = 1f;

        [Header("Stats")] [Min(0.1f)] public float HealthMultiplier = 1f;

        [Min(0.1f)] public float MoveSpeedMultiplier = 1f;
        [Min(0.1f)] public float ScoreMultiplier = 1f;

        [Header("Slingshot Feel")] [Min(0.1f)] public float Weight = 1f;

        [Min(0.1f)] public float TensionRateMultiplier = 1f;
        [Min(0.1f)] public float LaunchVelocityMultiplier = 1f;
        [Min(0f)] public float ImpactDamageMultiplier = 1f;
        [Min(0f)] public float ImpactKnockbackMultiplier = 1f;

        [Header("Spiky")] public bool DamagesPlayerWhileHeld;

        [Min(0.1f)] public float HeldDamageInterval = 0.75f;

        [Header("Visuals")] public bool OverrideBodyColor;

        public Color BodyColor = new(0.42f, 0.2f, 0.72f, 1f);
        [Min(0.1f)] public float VisualScale = 1f;
        public bool ShowSpikes = true;

        public static EnemyTypeData Default => _default ??= new EnemyTypeData();
    }
}