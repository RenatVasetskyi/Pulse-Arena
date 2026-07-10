using System;
using System.Collections.Generic;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "Game Settings", menuName = "Pulse Arena/Game Settings")]
    public class GameSettings : ScriptableObject
    {
        [Header("Bootstrap")] public int TargetFrameRate = 120;

        public string MainMenuSceneName = SceneName.MainMenu;
        public string GameSceneName = SceneName.Game;
        public float MinLoadingScreenTime = 0.35f;

        [Header("Core")] public GroundingData Grounding = new();

        public PrefabData Prefabs;

        [Header("Sub-Configs")] [SerializeField]
        private PlayerConfig _player;

        [SerializeField] private EnemyConfig _enemy;
        [SerializeField] private CombatConfig _combat;
        [SerializeField] private LevelConfig _level;
        [SerializeField] private PresentationConfig _presentation;

        [NonSerialized] private Dictionary<EnemyTypeId, EnemyTypeData> _enemyTypeCache;

        // Facade: consumers keep calling gameSettings.PlayerData etc.
        // The data now lives in separate, swappable config assets.
        public PlayerData PlayerData => _player.Data;
        public PlayerVisualData PlayerVisuals => _player.Visuals;
        public EnemyData EnemyData => _enemy.Data;
        public EnemyTypeData[] EnemyTypes => _enemy.Types;
        public EnemyVisualData EnemyVisuals => _enemy.Visuals;
        public SlingshotData SlingshotData => _combat.Slingshot;
        public ComboData ComboData => _combat.Combo;
        public SlowMoData SlowMoData => _combat.SlowMo;
        public PitData PitData => _combat.Pit;
        public SuperData SuperData => _combat.Super;
        public FeelData Feel => _combat.Feel;
        public SpawnData SpawnData => _level.Spawn;
        public SpawnAreaData SpawnAreaData => _level.SpawnArea;
        public TurretData TurretData => _level.Turret;
        public WaveData[] Waves => _level.Waves;
        public PickupData PickupData => _level.Pickup;
        public PoolData PoolData => _level.Pool;
        public VfxData Vfx => _presentation.Vfx;
        public CameraData CameraData => _presentation.Camera;
        public UiData Ui => _presentation.Ui;
        public AudioData AudioData => _presentation.Audio;

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

        [Header("Dash / Dodge")] [Tooltip("Burst speed during a dash (vs MoveSpeed for normal movement).")]
        public float DashSpeed = 24f;

        [Tooltip("How long the dash burst lasts.")]
        public float DashDuration = 0.18f;

        [Tooltip("Seconds before the player can dash again.")]
        public float DashCooldown = 0.9f;

        [Tooltip("Invulnerability window granted by a dash (dodge i-frames).")]
        public float DashInvulnerability = 0.3f;
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
        public float AttackRange = 1.35f;
        public float AttackCooldown = 0.9f;

        [Tooltip("Delay from the attack telegraph starting to the damage landing — set to the mid-point of the lunge.")]
        public float AttackHitDelay = 0.15f;

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
        public float MoveAnimationMaxSpeed = 4.5f; // planar speed that maps to full run-blend
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
        public float MoveAnimationMaxSpeed = 3.8f; // planar speed that maps to full run-blend
        public float WobbleFrequencyIdle = 2.6f;
        public float WobbleFrequencyRun = 8.5f;
        public float SquashAmountIdle = 0.025f;
        public float SquashAmountRun = 0.08f;
        public float HitSquashDuration = 0.16f;
        public float BounceSquashDuration = 0.22f;
        public float DeathPopDuration = 0.38f;
    }

    [Serializable]
    public class VfxData
    {
        [Header("Rope Snap Burst")] public int SnapBurstCount = 26;

        public float SnapBurstLifetimeMin = 0.18f;
        public float SnapBurstLifetimeMax = 0.42f;
        public float SnapBurstSpeedMin = 2.5f;
        public float SnapBurstSpeedMax = 6.5f;
        public float SnapBurstSizeMin = 0.05f;
        public float SnapBurstSizeMax = 0.14f;
        public float SnapBurstGravity = 1.2f;

        [Header("Ringout Burst")] public int RingoutBurstCount = 20;

        public float RingoutBurstLifetimeMin = 0.22f;
        public float RingoutBurstLifetimeMax = 0.5f;
        public float RingoutBurstSpeedMin = 2f;
        public float RingoutBurstSpeedMax = 5.5f;
        public float RingoutBurstSizeMin = 0.06f;
        public float RingoutBurstSizeMax = 0.16f;
        public float RingoutBurstGravity = 0.6f;
        public Color RingoutColorA = new(1f, 0.92f, 0.4f, 1f);
        public Color RingoutColorB = new(1f, 0.55f, 0.2f, 1f);

        [Header("Floating Score Text")] public float FloatingTextLifetime = 0.9f;

        public float FloatingTextRiseSpeed = 1.6f;
        public Color FloatingTextColor = new(1f, 0.92f, 0.4f, 1f);
    }

    [Serializable]
    public class CameraData
    {
        [Header("Zoom")] public float DefaultZoom = 1f;

        public float MinZoom = 0.72f;
        public float MaxZoom = 1.38f;
        public float ZoomStep = 0.08f;
        public float ZoomSmoothTime = 0.22f;

        [Header("Shake")] public float DefaultShakeDuration = 0.18f;

        public float DefaultShakeStrength = 0.35f;
        public float ShakeFrequency = 35f;
        public float RopeBreakShakeDuration = 0.12f;
        public float RopeBreakShakeStrength = 0.32f;

        [Header("Player Hit FX")] public float PlayerHitShakeDuration = 0.15f;

        public float PlayerHitShakeStrength = 0.25f;

        [Header("Lasso Launch FX")] public Vector3 LaunchKickOffset = new(0f, 0.32f, -0.65f);

        public float LaunchKickDuration = 0.2f;
        public float LaunchShakeDuration = 0.12f;
        public float LaunchShakeStrength = 0.18f;

        [Tooltip("FOV delta punched on lasso launch (negative = quick zoom-in). Scaled by charge.")]
        public float LaunchFovPunch = -3f;

        public float LaunchFovDuration = 0.26f;
    }

    [Serializable]
    public class SlowMoData
    {
        [Range(0.05f, 1f)] public float Scale = 0.4f;
        public float Duration = 0.22f;

        [Tooltip("Launch charge (0-1) at/above which a big fling triggers slow-mo.")] [Range(0f, 1f)]
        public float LaunchChargeThreshold = 0.85f;

        [Tooltip("Minimum seconds between launch-triggered slow-mos.")]
        public float Cooldown = 1.4f;
    }

    [Serializable]
    public class PitData
    {
        [Header("Spawn")] [Tooltip("Seconds between pit spawn attempts.")]
        public float SpawnInterval = 5f;

        [Tooltip("Max pits open at once.")] public int MaxActive = 3;

        [Tooltip("Inner/outer ring (from arena center) where pits can appear.")]
        public float MinRadius = 4f;

        public float MaxRadius = 18f;

        [Tooltip("Pits never spawn closer than this (horizontal) to the player.")]
        public float MinPlayerDistance = 3.5f;

        [Tooltip(
            "Extra clearance (beyond the pit's own radius) kept from the player and every enemy, so a pit never opens right on top of someone.")]
        public float SpawnClearance = 1.5f;

        [Tooltip("Height (Y) pits sit at, just above the floor.")]
        public float SpawnHeight = 0.05f;

        [Header("Size / Lifetime")] [Tooltip("Random uniform scale each pit spawns at (bigger = wider catch zone).")]
        public float MinScale = 0.7f;

        public float MaxScale = 1.8f;

        [Tooltip("Seconds an unused pit stays open before it closes on its own.")]
        public float MinLifetime = 6f;

        public float MaxLifetime = 11f;

        [Header("Suck-In")] [Tooltip("Horizontal speed the eaten enemy is yanked toward the pit center.")]
        public float SuckSpeed = 12f;

        [Tooltip("Downward speed added as the enemy is swallowed.")]
        public float SuckDown = 4f;
    }

    [Serializable]
    public class ComboData
    {
        [Tooltip("Seconds within which the next kill keeps the combo chain alive.")]
        public float Window = 2.5f;

        [Tooltip("Highest score multiplier the combo can reach.")]
        public int MaxMultiplier = 8;
    }

    [Serializable]
    public class SuperData
    {
        [Header("Charge")] [Tooltip("Kills needed to fill the super meter.")]
        public int KillsToCharge = 10;

        [Header("Ultimate — Shockwave")] [Tooltip("Radius around the player that the ultimate flings enemies within.")]
        public float Radius = 12f;

        [Tooltip("Outward launch speed applied to caught enemies.")]
        public float LaunchSpeed = 22f;

        [Tooltip("Upward launch ratio (arc height) on top of the outward speed.")]
        public float UpwardRatio = 0.4f;

        [Tooltip("How long caught enemies stay airborne / launched.")]
        public float LaunchDuration = 1f;

        [Header("Ultimate — Juice")] public float ShakeDuration = 0.4f;

        public float ShakeStrength = 0.7f;
        [Range(0.05f, 1f)] public float SlowMoScale = 0.35f;
        public float SlowMoDuration = 0.4f;

        [Header("Ultimate — Shockwave VFX")] [Tooltip("Particles in the expanding ground ring.")]
        public int ShockwaveParticleCount = 64;

        [Tooltip("How long the ring particles live (also how long the ring keeps expanding).")]
        public float ShockwaveLifetime = 0.5f;

        [Tooltip("Outward speed of the ring — tuned so the ring reaches roughly the fling Radius.")]
        public float ShockwaveSpeed = 22f;

        public float ShockwaveStartSize = 0.55f;
        public float ShockwaveStartRadius = 0.6f;
        public Color ShockwaveColor = new(1f, 0.82f, 0.35f, 1f);
    }

    [Serializable]
    public class AudioData
    {
        [Range(0f, 1f)] public float MasterVolume = 1f;
        [Range(0f, 1f)] public float SfxVolume = 0.85f;
        public SfxEntry[] Sfx;

        [Header("Music")] [Range(0f, 1f)] public float MusicVolume = 0.45f;

        public AudioClip MenuMusic;
        public AudioClip BattleMusic;
    }

    [Serializable]
    public class SfxEntry
    {
        public GameSfx Id;
        public AudioClip Clip;
        [Range(0f, 1f)] public float Volume = 1f;
        public float PitchMin = 1f;
        public float PitchMax = 1f;
    }

    [Serializable]
    public class UiData
    {
        [Header("HUD")] public Color HudPanelColor = new(0.06f, 0.07f, 0.1f, 0.62f);

        public Color HealthAliveColor = new(1f, 0.16f, 0.12f, 1f);
        public Color HealthEmptyColor = new(0.18f, 0.2f, 0.24f, 0.82f);
        public Color ScoreTextColor = new(1f, 0.92f, 0.4f, 1f);
        public Color WaveTextColor = new(0.92f, 0.96f, 1f, 1f);
        public Color ToastBackgroundColor = new(0.05f, 0.12f, 0.08f, 0.82f);
        public Color ToastTextColor = new(0.78f, 1f, 0.68f, 1f);
        public Color TensionBackgroundColor = new(0.06f, 0.07f, 0.1f, 0.78f);
        public Color TensionSafeColor = new(1f, 0.82f, 0.3f, 0.95f);
        public Color TensionDangerColor = new(1f, 0.22f, 0.12f, 1f);

        [Header("World Health Bar")] public Color WorldHealthAliveColor = new(1f, 0.2f, 0.12f, 1f);

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

        [Header("Weight Feel")] public float WeightFactorMin = 0.35f;

        public float WeightFactorMax = 1.5f;

        [Header("Target Marker")] public float MarkerSearchRangeMultiplier = 1.5f;

        public float MarkerRadius = 0.85f;
        public float MarkerHeight = 0.05f;
        public float MarkerWidth = 0.07f;
        public float MarkerPulseSpeed = 7f;
        public float MarkerPulseAmplitude = 0.06f;
        public Color MarkerActiveColor = new(0.35f, 1f, 0.5f, 0.85f);
        public Color MarkerInactiveColor = new(0.7f, 0.7f, 0.7f, 0.35f);

        [Header("Rope Tension")] public float TensionBreakTime = 5f;

        public float TensionChargeInfluence = 0.5f;
        public float TensionWarningThreshold = 0.55f;
        public float TensionShakeAmplitude = 1.6f;
        public float TensionPulseSpeed = 26f;
        public float TensionPulseAmplitude = 0.18f;
        public float BreakDropForce = 6f;
        public float BreakCooldownMultiplier = 1.6f;
        public Color TensionColor = new(1f, 0.25f, 0.15f, 1f);
        public LayerMask EnemyLayer;
        public LayerMask ObstacleLayer;
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

        [Tooltip(
            "Enemies won't spawn at points closer than this (horizontal) to the player, so they never pop up right on top of you.")]
        public float MinPlayerSpawnDistance = 5f;
    }

    /// <summary>
    ///     Geometry for the random safe-zone spawns (enemies + pickups): a ring around the arena center, kept clear
    ///     of the player and of any wall/box, suck-hole pit, or other spawn. Consumed by
    ///     <see cref="Game.Spawning.ISafeSpawnFinder" />.
    /// </summary>
    [Serializable]
    public class SpawnAreaData
    {
        [Header("Ring (from arena center)")]
        [Tooltip("Inner radius of the ring where enemies & pickups can appear.")]
        public float MinRadius = 5f;

        [Tooltip("Outer radius of the ring where enemies & pickups can appear.")]
        public float MaxRadius = 17f;

        [Tooltip("Nothing spawns closer than this (horizontal) to the player.")]
        public float PlayerClearance = 5f;

        [Tooltip(
            "Clearance-sphere radius: a spot is rejected if this sphere overlaps a wall/box, pit, pickup or enemy.")]
        public float SpawnClearance = 1.1f;

        [Tooltip("Height above the floor the clearance sphere is cast from, so it clears the ground plane.")]
        public float ProbeHeight = 0.6f;

        [Tooltip("How many random spots to try before giving up for this spawn tick.")]
        public int MaxTries = 24;
    }

    /// <summary>
    ///     Config for the stationary turrets that spawn on the map and shoot the player (not enemies).
    ///     Consumed by <see cref="Game.Turrets.TurretSpawner" /> / <see cref="Game.Turrets.Turret" />.
    /// </summary>
    [Serializable]
    public class TurretData
    {
        [Header("Spawn")] [Tooltip("Seconds between turret spawn attempts.")]
        public float SpawnInterval = 10f;

        [Tooltip("Max turrets alive at once.")] public int MaxActive = 1;

        [Tooltip("Seconds a turret lives before it self-destructs (with a collapse animation).")]
        public float Lifetime = 7f;

        [Header("Fire")] [Tooltip("Seconds between shots.")]
        public float FireInterval = 1.5f;

        [Tooltip("How fast the head swivels to track the player (lerp speed).")]
        public float AimSpeed = 4f;

        [Header("Bullet")] public float BulletSpeed = 11f;
        public int BulletDamage = 1;

        [Tooltip("Seconds a bullet lives before it despawns if it hits nothing.")]
        public float BulletLifetime = 4f;
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
        public GameObject ArenaPrefab;

        [Header("World UI")] public GameObject WorldHealthBarPrefab;

        public GameObject HookTargetMarkerPrefab;
        public GameObject FloatingScoreTextPrefab;

        [Header("Pickups")] public GameObject HealthOrbPrefab;

        [Header("Hazards")] public GameObject PitPrefab;
        public GameObject TurretPrefab;
        public GameObject TurretBulletPrefab;

        [Header("UI")] public GameObject SettingsPanelPrefab;

        public GameObject PausePanelPrefab;
    }
}