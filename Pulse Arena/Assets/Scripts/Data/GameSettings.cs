using System;
using System.Collections.Generic;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "Game Settings", menuName = "Pulse Arena/Game Settings")]
    public class GameSettings : ScriptableObject
    {
        [Header("Bootstrap")]
        public int TargetFrameRate = 120;
        public string MainMenuSceneName = SceneName.MainMenu;
        public string GameSceneName = SceneName.Game;
        public float MinLoadingScreenTime = 0.35f;

        [Header("Core")]
        public GroundingData Grounding = new();
        public PrefabData Prefabs;

        [Header("Sub-Configs")]
        [SerializeField] private PlayerConfig _player;
        [SerializeField] private EnemyConfig _enemy;
        [SerializeField] private CombatConfig _combat;
        [SerializeField] private LevelConfig _level;
        [SerializeField] private PresentationConfig _presentation;

        // Facade: consumers keep calling gameSettings.PlayerData etc.
        // The data now lives in separate, swappable config assets.
        public PlayerData PlayerData => _player.Data;
        public PlayerVisualData PlayerVisuals => _player.Visuals;
        public EnemyData EnemyData => _enemy.Data;
        public EnemyTypeData[] EnemyTypes => _enemy.Types;
        public EnemyVisualData EnemyVisuals => _enemy.Visuals;
        public SlingshotData SlingshotData => _combat.Slingshot;
        public SpawnData SpawnData => _level.Spawn;
        public WaveData[] Waves => _level.Waves;
        public PickupData PickupData => _level.Pickup;
        public PoolData PoolData => _level.Pool;
        public VfxData Vfx => _presentation.Vfx;
        public CameraData CameraData => _presentation.Camera;
        public UiData Ui => _presentation.Ui;

        [NonSerialized] private Dictionary<EnemyTypeId, EnemyTypeData> _enemyTypeCache;

        public EnemyTypeData GetEnemyType(EnemyTypeId id)
        {
            if (_enemyTypeCache == null)
            {
                _enemyTypeCache = new Dictionary<EnemyTypeId, EnemyTypeData>();

                if (EnemyTypes != null)
                {
                    foreach (EnemyTypeData type in EnemyTypes)
                    {
                        if (type != null)
                            _enemyTypeCache.TryAdd(type.Id, type);
                    }
                }
            }

            return _enemyTypeCache.TryGetValue(id, out EnemyTypeData found) ? found : EnemyTypeData.Default;
        }
    }

    [Serializable]
    public class PlayerData
    {
        public float MoveSpeed = 6f;
        public float RotationSpeed = 720f;
        public float ExtraGravity = 35f;
        public int MaxHealth = 3;
        public float HitInvulnerability = 0.65f;
        public float HitKnockbackForce = 5f;
        public float HitKnockbackDuration = 0.18f;
        public float HitFlashDuration = 0.12f;
        public Color HitFlashColor = new(1f, 0.08f, 0.03f, 1f);
        public float RingoutHeight = -2.5f;
    }

    [Serializable]
    public class EnemyData
    {
        public float MoveSpeed = 3.5f;
        public float RotationSpeed = 8f;
        public float KnockbackDuration = 0.45f;
        public float ExtraGravity = 45f;
        public float AgentRadius = 0.35f;
        public float AgentHeight = 1.8f;
        public float AgentAcceleration = 24f;
        public float AgentAngularSpeed = 720f;
        public float AgentStoppingDistance = 1.25f;
        public float NavMeshSampleDistance = 2f;
        public float DestinationUpdateInterval = 0.15f;
        public int MaxHealth = 3;
        public float HealthBarHeight = 2.15f;
        public int ScoreReward = 1;
        public float HitFlashDuration = 0.12f;
        public Color HitFlashColor = new(1f, 0.08f, 0.03f, 1f);
        public float AttackRange = 1.35f;
        public float AttackCooldown = 0.9f;
        public int ContactDamage = 1;
        public float ImpactDamageMinSpeed = 3.5f;
        public float ImpactDamageRadius = 1.6f;
        public float ImpactDamageForwardOffset = 0.75f;
        public float ImpactKnockbackForce = 8.5f;
        public float ImpactKnockbackUpwardRatio = 0.12f;
        public int ImpactDamage = 1;
        public int WallImpactDamage = 1;
        public float ImpactDamageCooldown = 0.08f;
        public int GroundBounceCount = 1;
        public float GroundBounceUpwardVelocity = 5.8f;
        public float GroundBounceHorizontalDamping = 0.58f;
        public float GroundBounceCooldown = 0.18f;
        public float GroundBounceMinVerticalSpeed = 1.8f;
        public float GroundBounceKeepAliveDuration = 0.45f;
        public float GroundRecoveryProbeDistance = 4f;
        public float GroundRecoveryMaxVerticalOffset = 1.35f;
        public float GroundRecoveryMaxUpwardSpeed = 0.1f;
        public float GroundRecoveryForceAfter = 1.1f;
        public float RingoutHeight = -2.5f;
        public float RingoutDuration = 1.1f;
        public float RingoutTextHeight = 1.2f;
        public float RingoutShrinkScale = 0.15f;
        public float GroundContactMemory = 0.12f;
        public float HeldDamageGrace = 0.35f;
    }

    [Serializable]
    public class GroundingData
    {
        public float GroundClearance = 0.02f;
        public float DefaultProbeDistance = 8f;
        public float GroundNormalThreshold = 0.55f;
    }

    [Serializable]
    public class PlayerVisualData
    {
        public Vector3 RootOffset = new(0f, -0.78f, 0f);
        public Color BodyColor = new(0.2f, 0.75f, 0.95f, 1f);
        public Color HeadColor = new(0.78f, 0.9f, 0.95f, 1f);
        public Color DarkColor = new(0.08f, 0.11f, 0.16f, 1f);
        public Color AccentColor = new(1f, 0.78f, 0.24f, 1f);
        public float MoveThreshold = 0.25f;
        public float BobFrequencyIdle = 2.4f;
        public float BobFrequencyRun = 9f;
        public float BobAmplitudeIdle = 0.025f;
        public float BobAmplitudeRun = 0.09f;
        public float ArmSwingFrequency = 10f;
        public float ArmSwingAngle = 28f;
        public float ThrowSwingDuration = 0.28f;
        public float HitSquashDuration = 0.16f;
        public float DeathRollAngle = 72f;
    }

    [Serializable]
    public class EnemyVisualData
    {
        public Vector3 RootOffset = new(0f, -1.15f, 0f);
        public Color BodyColor = new(0.42f, 0.2f, 0.72f, 1f);
        public Color BellyColor = new(0.58f, 0.42f, 0.9f, 1f);
        public Color EyeColor = new(1f, 0.92f, 0.72f, 1f);
        public Color PupilColor = new(0.04f, 0.02f, 0.08f, 1f);
        public Color SpikeColor = new(0.12f, 0.08f, 0.22f, 1f);
        public float MoveThreshold = 0.2f;
        public float WobbleFrequencyIdle = 2.6f;
        public float WobbleFrequencyRun = 8.5f;
        public float SquashAmountIdle = 0.025f;
        public float SquashAmountRun = 0.08f;
        public float HitSquashDuration = 0.16f;
        public float BounceSquashDuration = 0.22f;
        public float DeathPopDuration = 0.38f;
        public float ThrownSpinSpeed = 540f;
    }

    [Serializable]
    public class VfxData
    {
        [Header("Rope Snap Burst")]
        public int SnapBurstCount = 26;
        public float SnapBurstLifetimeMin = 0.18f;
        public float SnapBurstLifetimeMax = 0.42f;
        public float SnapBurstSpeedMin = 2.5f;
        public float SnapBurstSpeedMax = 6.5f;
        public float SnapBurstSizeMin = 0.05f;
        public float SnapBurstSizeMax = 0.14f;
        public float SnapBurstGravity = 1.2f;

        [Header("Ringout Burst")]
        public int RingoutBurstCount = 20;
        public float RingoutBurstLifetimeMin = 0.22f;
        public float RingoutBurstLifetimeMax = 0.5f;
        public float RingoutBurstSpeedMin = 2f;
        public float RingoutBurstSpeedMax = 5.5f;
        public float RingoutBurstSizeMin = 0.06f;
        public float RingoutBurstSizeMax = 0.16f;
        public float RingoutBurstGravity = 0.6f;
        public Color RingoutColorA = new(1f, 0.92f, 0.4f, 1f);
        public Color RingoutColorB = new(1f, 0.55f, 0.2f, 1f);

        [Header("Floating Score Text")]
        public float FloatingTextLifetime = 0.9f;
        public float FloatingTextRiseSpeed = 1.6f;
        public Color FloatingTextColor = new(1f, 0.92f, 0.4f, 1f);
    }

    [Serializable]
    public class CameraData
    {
        [Header("Zoom")]
        public float DefaultZoom = 1f;
        public float MinZoom = 0.72f;
        public float MaxZoom = 1.38f;
        public float ZoomStep = 0.08f;
        public float ZoomSmoothTime = 0.22f;

        [Header("Shake")]
        public float DefaultShakeDuration = 0.18f;
        public float DefaultShakeStrength = 0.35f;
        public float ShakeFrequency = 35f;
        public float RopeBreakShakeDuration = 0.12f;
        public float RopeBreakShakeStrength = 0.32f;

        [Header("Lasso Launch FX")]
        public Vector3 LaunchKickOffset = new(0f, 0.32f, -0.65f);
        public float LaunchKickDuration = 0.2f;
        public float LaunchShakeDuration = 0.12f;
        public float LaunchShakeStrength = 0.18f;
    }

    [Serializable]
    public class UiData
    {
        [Header("HUD")]
        public Color HudPanelColor = new(0.06f, 0.07f, 0.1f, 0.62f);
        public Color HealthAliveColor = new(1f, 0.16f, 0.12f, 1f);
        public Color HealthEmptyColor = new(0.18f, 0.2f, 0.24f, 0.82f);
        public Color ScoreTextColor = new(1f, 0.92f, 0.4f, 1f);
        public Color WaveTextColor = new(0.92f, 0.96f, 1f, 1f);
        public Color ToastBackgroundColor = new(0.05f, 0.12f, 0.08f, 0.82f);
        public Color ToastTextColor = new(0.78f, 1f, 0.68f, 1f);
        public Color TensionBackgroundColor = new(0.06f, 0.07f, 0.1f, 0.78f);
        public Color TensionSafeColor = new(1f, 0.82f, 0.3f, 0.95f);
        public Color TensionDangerColor = new(1f, 0.22f, 0.12f, 1f);

        [Header("World Health Bar")]
        public Color WorldHealthAliveColor = new(1f, 0.2f, 0.12f, 1f);
        public Color WorldHealthEmptyColor = new(0.12f, 0.12f, 0.15f, 0.76f);
        public Color WorldHealthBackgroundColor = new(0.02f, 0.025f, 0.035f, 0.72f);
    }

    [Serializable]
    public class SlingshotData
    {
        public float GrabRadius = 7.5f;
        public float HoldRadius = 1.85f;
        public float HoldHeight = 0.75f;
        public float HoldAngularSpeed = 170f;
        public float MaxHoldAngularSpeed = 720f;
        public float SpinAcceleration = 190f;
        public float HoldFollowSpeed = 26f;
        public float LaunchForce = 18f;
        public float LaunchUpwardRatio = 0.12f;
        public float LaunchDownwardVelocity = 8f;
        public float LaunchDuration = 0.9f;
        public float ChargeDuration = 4f;
        public float MaxChargeLaunchMultiplier = 1.6f;
        public float MaxChargeLineWidthMultiplier = 1.65f;
        public float ThrowDuration = 0.18f;
        public float WrapDuration = 0.32f;
        public float PullToHoldDuration = 0.28f;
        public float PullToHoldArcHeight = 0.45f;
        public float Cooldown = 0.45f;
        public float LineWidth = 0.08f;
        public float ThrowWaveAmplitude = 0.08f;
        public float WrapWaveAmplitude = 0.06f;
        public float WrapRadius = 0.7f;
        public float WrapRadiusScale = 0.86f;
        public float WrapRadiusPadding = 0.015f;
        public float MinWrapRadius = 0.3f;
        public float WrapVerticalScale = 0.34f;
        public float WrapTurns = 2.4f;
        public float WrapSpinSpeed = 12f;
        public float RopeWaveCount = 3f;
        public float RopeWaveSpeed = 12f;
        public float RopeTextureRepeat = 1.35f;
        public Color LineColor = new(0.82f, 0.55f, 0.28f, 1f);
        public Color ChargedLineColor = new(1f, 0.88f, 0.32f, 1f);
        public Color RopeBaseColor = new(0.78f, 0.48f, 0.22f, 1f);
        public Color RopeStripeColor = new(0.35f, 0.21f, 0.1f, 1f);

        [Header("Weight Feel")]
        public float WeightFactorMin = 0.35f;
        public float WeightFactorMax = 1.5f;

        [Header("Target Marker")]
        public float MarkerSearchRangeMultiplier = 1.5f;
        public float MarkerRadius = 0.85f;
        public float MarkerHeight = 0.05f;
        public float MarkerWidth = 0.07f;
        public float MarkerPulseSpeed = 7f;
        public float MarkerPulseAmplitude = 0.06f;
        public Color MarkerActiveColor = new(0.35f, 1f, 0.5f, 0.85f);
        public Color MarkerInactiveColor = new(0.7f, 0.7f, 0.7f, 0.35f);

        [Header("Rope Tension")]
        public float TensionBreakTime = 5f;
        public float TensionChargeInfluence = 0.5f;
        public float TensionWarningThreshold = 0.55f;
        public float TensionShakeAmplitude = 1.6f;
        public float TensionPulseSpeed = 26f;
        public float TensionPulseAmplitude = 0.18f;
        public float BreakDropForce = 6f;
        public float BreakCooldownMultiplier = 1.6f;
        public Color TensionColor = new(1f, 0.25f, 0.15f, 1f);
        public LayerMask EnemyLayer;
    }

    [Serializable]
    public class PickupData
    {
        public float RotateSpeed = 120f;
        public float BobHeight = 0.25f;
        public float BobSpeed = 3f;
        public int HealthAmount = 1;
        public string RareSpawnMessage = "Health Orb spawned!";
        public float SpawnToastDuration = 1.5f;
    }

    [Serializable]
    public class SpawnData
    {
        public float EnemySpawnDelay = 2.2f;
        public float PickupSpawnDelay = 15f;
        public int MaxEnemies = 8;
        public int MaxPickups = 1;
        public float WavePollInterval = 0.25f;
    }

    [Serializable]
    public class WaveEnemyData
    {
        public EnemyTypeId Type = EnemyTypeId.Standard;
        [Min(1)] public int Count = 3;
    }

    [Serializable]
    public class WaveData
    {
        [Min(0f)] public float DelayBeforeWave = 2.5f;
        [Min(0.05f)] public float SpawnInterval = 1f;
        public WaveEnemyData[] Enemies;
    }

    [Serializable]
    public class PoolData
    {
        public int EnemyPreloadCount = 8;
    }

    [Serializable]
    public class PrefabData
    {
        public GameObject PlayerPrefab;
        public GameObject EnemyPrefab;
        public GameObject MainMenuPrefab;
        public GameObject LoadingScreenPrefab;
        public GameObject GameHudPrefab;
        public GameObject GameOverPrefab;
    }
}
