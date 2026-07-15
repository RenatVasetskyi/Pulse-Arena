using System;
using System.Collections.Generic;
using UnityEngine;

namespace Data
{
    /// <summary>
    ///     The single config facade injected into virtually every gameplay system. Holds five swappable
    ///     sub-config assets (player / enemy / combat / level / presentation) and re-exposes their contents
    ///     through read-only pass-through properties, so consumers keep one dependency and never know the
    ///     data is split across assets. Values live in the .asset files — changing a C# field default only
    ///     affects newly created assets.
    /// </summary>
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
        public SlingshotData SlingshotData => _combat.Slingshot;
        public ComboData ComboData => _combat.Combo;
        public SlowMoData SlowMoData => _combat.SlowMo;
        public PitData PitData => _combat.Pit;
        public SuperData SuperData => _combat.Super;
        public FeelData Feel => _combat.Feel;
        public SpawnData SpawnData => _level.Spawn;
        public SpawnAreaData SpawnAreaData => _level.SpawnArea;
        public TurretData TurretData => _level.Turret;
        public LevelDefinition[] Levels => _level.Levels;
        public SurvivalData SurvivalData => _level.Survival;
        public WaveData[] Waves => _level.Waves;
        public PickupData PickupData => _level.Pickup;
        public PoolData PoolData => _level.Pool;
        public VfxData Vfx => _presentation.Vfx;
        public CameraData CameraData => _presentation.Camera;
        public UiData Ui => _presentation.Ui;
        public AudioData AudioData => _presentation.Audio;
        public HapticData Haptics => _presentation.Haptics;
        public OnboardingData Onboarding => _presentation.Onboarding;

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
        [Header("Movement")] public float MoveSpeed = 6f;
        public float RotationSpeed = 720f;
        public float ExtraGravity = 35f;

        [Header("Health / Hit")] public int MaxHealth = 3;
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
        [Header("Movement")] public float MoveSpeed = 3.5f;
        public float RotationSpeed = 8f;
        public float KnockbackDuration = 0.45f;
        public float ExtraGravity = 45f;

        [Header("NavMesh Agent")] public float AgentRadius = 0.35f;
        public float AgentHeight = 1.8f;
        public float AgentAcceleration = 24f;
        public float AgentAngularSpeed = 720f;
        public float AgentStoppingDistance = 1.25f;
        public float NavMeshSampleDistance = 2f;
        public float DestinationUpdateInterval = 0.15f;

        [Header("Health / Score")] public int MaxHealth = 3;
        public float HealthBarHeight = 2.15f;
        public int ScoreReward = 1;

        [Header("Attack")] public float AttackRange = 1.35f;
        public float AttackCooldown = 0.9f;

        [Tooltip("Delay from the attack telegraph starting to the damage landing — set to the mid-point of the lunge.")]
        public float AttackHitDelay = 0.15f;

        [Tooltip("Stationary recovery window after the strike lands before the enemy resumes chasing (the tail of the attack state).")]
        public float AttackRecovery = 0.4f;

        [Header("Contact / Impact")] public int ContactDamage = 1;
        public float ImpactDamageMinSpeed = 3.5f;
        public float ImpactDamageRadius = 1.6f;
        public float ImpactDamageForwardOffset = 0.75f;
        public float ImpactKnockbackForce = 8.5f;
        public float ImpactKnockbackUpwardRatio = 0.12f;
        public int ImpactDamage = 1;
        public int WallImpactDamage = 1;
        public float ImpactDamageCooldown = 0.08f;
        [Header("Ground Bounce")] public int GroundBounceCount = 1;
        public float GroundBounceUpwardVelocity = 5.8f;
        public float GroundBounceHorizontalDamping = 0.58f;

        [Tooltip("Min upward-facing contact-normal Y for a collision to count as a ground bounce (1 = flat floor, lower = accepts slopes).")]
        public float GroundBounceNormalThreshold = 0.65f;
        public float GroundBounceCooldown = 0.18f;
        public float GroundBounceMinVerticalSpeed = 1.8f;
        public float GroundBounceKeepAliveDuration = 0.45f;

        [Header("Ground Recovery")] public float GroundRecoveryProbeDistance = 4f;
        public float GroundRecoveryMaxVerticalOffset = 1.35f;
        public float GroundRecoveryMaxUpwardSpeed = 0.1f;
        public float GroundRecoveryForceAfter = 1.1f;

        [Header("Ringout")] public float RingoutDuration = 1.1f;
        public float RingoutTextHeight = 1.2f;
        public float RingoutShrinkScale = 0.15f;

        [Tooltip("Ragdoll tumble spin (deg/s, ±) applied to X/Z when an enemy is flung off the edge.")]
        public float RingoutTumbleSpin = 10f;

        [Tooltip("Ragdoll tumble yaw (deg/s, ±) applied to Y when an enemy is flung off the edge.")]
        public float RingoutTumbleYaw = 4f;
        [Header("Misc Timing")] public float GroundContactMemory = 0.12f;
        public float HeldDamageGrace = 0.35f;

        [Header("Run Flourish")]
        [Tooltip("Min seconds between run-flourish attempts (e.g. the karate somersault). Re-rolled on each chase entry.")]
        public float FlourishMinInterval = 3f;

        [Tooltip("Max seconds between run-flourish attempts.")]
        public float FlourishMaxInterval = 6f;

        [Tooltip("The enemy only flourishes (somersaults) while farther than this from the target — never shows off inside striking range.")]
        public float FlourishMinPlayerDistance = 4.5f;

        [Header("Per-Spawn Variation")]
        [Tooltip("Per-spawn speed spread: each enemy rolls a random move speed in [1-this, 1+this] for variety. 0 = every enemy the same speed.")]
        [Range(0f, 0.6f)] public float SpeedVariation = 0.25f;

        [Tooltip("Per-spawn size spread, INVERSELY tied to the speed roll — the fast ones come out smaller, the slow ones bigger. 0 = uniform size.")]
        [Range(0f, 0.5f)] public float SizeVariation = 0.2f;
    }

    [Serializable]
    public class GroundingData
    {
        public float GroundClearance = 0.02f;
        public float DefaultProbeDistance = 8f;
        public float GroundNormalThreshold = 0.55f;
    }

    /// <summary>
    ///     The player model's locomotion-blend knobs, read by <c>CowboyVisual</c> to drive its Animator Speed
    ///     parameter. (The enemy model — <c>SkeletonEnemyVisual</c> — samples raw movement instead and needs no
    ///     equivalent data, so there is deliberately no <c>EnemyVisualData</c>.)
    /// </summary>
    [Serializable]
    public class PlayerVisualData
    {
        [Tooltip("Planar speed below which the model stays in Idle (dead-zone before the run blend starts).")]
        public float MoveThreshold = 0.25f;

        [Tooltip("Planar speed that maps to the full run-blend — the model's Speed anim param saturates here.")]
        public float MoveAnimationMaxSpeed = 4.5f;
    }

    [Serializable]
    public class VfxData
    {
        [Header("Floating Score Text")] public float FloatingTextLifetime = 0.9f;

        public float FloatingTextRiseSpeed = 1.6f;
        public Color FloatingTextColor = new(1f, 0.92f, 0.4f, 1f);
    }

    [Serializable]
    public class CameraData
    {
        [Header("Zoom")] public float DefaultZoom = 1f;

        public float MinZoom = 0.36f; // closest zoom-in (offset multiplier) — halved so the camera can get 50% closer
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

    /// <summary>Device haptics tuning. Handheld.Vibrate() can only do a fixed ~500ms buzz, so damage feedback uses a native short/weak one-shot on Android instead.</summary>
    [Serializable]
    public class HapticData
    {
        [Tooltip("Length of the damage vibration in milliseconds — short (~20-40ms) reads as a tap, not a buzz.")]
        [Range(5f, 200f)] public int PlayerHitDurationMs = 30;

        [Tooltip("Strength of the damage vibration, 1-255 (Android amplitude). Low (~40-80) = a soft nudge.")]
        [Range(1, 255)] public int PlayerHitAmplitude = 60;
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

        [Tooltip("Slow-mo scale when a wave is cleared — its last enemy dies in slow motion (lower = slower).")]
        [Range(0.05f, 1f)] public float WaveClearScale = 0.3f;

        [Tooltip("How long the wave-clear slow-mo lasts (unscaled seconds).")]
        public float WaveClearDuration = 0.55f;
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

        [Tooltip("The pit prefab's base trigger-collider radius (scaled by spawn scale) — the clearance placement keeps around it. Match the pit prefab's CapsuleCollider.")]
        public float TriggerRadius = 2.5f;

        [Tooltip("Height (Y) pits sit at, just above the floor.")]
        public float SpawnHeight = 0.05f;

        [Header("Size / Lifetime")] [Tooltip("Random uniform scale each pit spawns at (bigger = wider catch zone).")]
        public float MinScale = 0.7f;

        public float MaxScale = 1.8f;

        [Tooltip("Seconds an unused pit stays open before it closes on its own.")]
        public float MinLifetime = 6f;

        public float MaxLifetime = 11f;

        [Header("Suck-In")]
        [Tooltip("Speed the swallowed enemy is drawn to the pit center and sinks straight down into the maw.")]
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

    /// <summary>
    ///     First-run onboarding copy + timing: the contextual hint strings shown ONCE on the player's first game
    ///     (grab → fling → goal, plus one-shot dash + ultimate prompts) and how long each stays on screen. Lives on
    ///     <see cref="PresentationConfig" />; the onboarding controller reads it and shows each via the HUD toast.
    /// </summary>
    [Serializable]
    public class OnboardingData
    {
        [Min(0.5f)] public float HintDuration = 5f;

        [TextArea] public string GrabHint = "Hold the lasso button next to an enemy to grab it";
        [TextArea] public string FlingHint = "Keep holding to wind up - release to hurl the enemy!";

        [TextArea] public string KillHint =
            "Smash enemies into walls or into each other to take them down.\nClear every wave to win!";

        [TextArea] public string BoosterHint = "Grab the glowing orbs to restore health";
        [TextArea] public string DashHint = "Tap the dash button to dodge enemy attacks";

        [TextArea] public string UltimateHint =
            "Ultimate charged! Tap the ultimate button to blast enemies away";
    }

    /// <summary>
    ///     All lasso/slingshot feel and balance: grab range and layers, spin/pull/launch physics, rope
    ///     tension accrual (break time, charge influence, warning threshold) and break feedback. Lives on
    ///     <see cref="CombatConfig" />; consumed by the slingshot and its collaborators via GameSettings.
    /// </summary>
    [Serializable]
    public class SlingshotData
    {
        public float GrabRadius = 7.5f;

        [Tooltip("Orbit radius at FULL charge (the widest the spin gets).")]
        public float HoldRadius = 1.85f;

        [Tooltip("Orbit radius at ZERO charge (a fresh grab). The spin stretches from here out to HoldRadius as it " +
                 "charges, like a rubber band.")]
        public float HoldRadiusStart = 1f;
        public float HoldHeight = 0.75f;
        public float HoldAngularSpeed = 170f;
        public float MaxHoldAngularSpeed = 720f;
        public float SpinAcceleration = 190f;
        public float HoldFollowSpeed = 26f;

        [Tooltip("While spinning, if the held enemy travels LESS than this fraction of the distance the spin should " +
                 "carry it, it counts as BLOCKED (snagged on a wall corner).")]
        [Range(0f, 1f)] public float SpinBlockedMoveFraction = 0.35f;

        [Tooltip("How long the spin can stay blocked before the rope snaps and drops the enemy.")]
        public float SpinBlockedBreakTime = 0.5f;
        public float LaunchForce = 18f;
        public float LaunchUpwardRatio = 0.12f;
        public float LaunchDownwardVelocity = 8f;
        public float LaunchDownwardMaxChargeMultiplier = 1.25f; // extra downward boost at full charge
        public float LaunchDuration = 0.9f;
        public float ChargeDuration = 4f;

        [Tooltip("Launch-force multiplier at ZERO charge (grab + instant release) — keep it small so a quick fling " +
                 "barely travels; distance then scales with spin up to MaxChargeLaunchMultiplier.")]
        [Range(0f, 1f)] public float MinChargeLaunchMultiplier = 0.2f;
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

        [Tooltip("Wrap-ring radius as a multiple of the enemy's torso (capsule) half-width — MUST be >1 so the ring " +
                 "encircles the body from OUTSIDE at every spawn size (large enemies otherwise get a ring inside them).")]
        public float WrapRadiusScale = 1.4f;
        public float WrapRadiusPadding = 0.015f;
        public float MinWrapRadius = 0.3f;
        public float WrapVerticalScale = 0.34f;
        public float WrapTurns = 2.4f;
        public float WrapSpinSpeed = 12f;
        public float RopeWaveCount = 3f;
        public float RopeWaveSpeed = 12f;
        public Color LineColor = new(0.82f, 0.55f, 0.28f, 1f);
        public Color ChargedLineColor = new(1f, 0.88f, 0.32f, 1f);

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
        public float BreakUpwardRatio = 0.25f; // fraction of BreakDropForce applied upward on a rope snap
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

        [Tooltip("Height above the target's feet the turret aims at (0.6 = torso, not feet).")]
        public float AimHeightOffset = 0.6f;

        [Header("Bullet")] public float BulletSpeed = 11f;
        public int BulletDamage = 1;

        [Tooltip("Seconds a bullet lives before it despawns if it hits nothing.")]
        public float BulletLifetime = 4f;

        [Header("Destruct")] [Tooltip("Angle the barrel droops just before the turret collapses (degrees).")]
        public float DestructDroopAngle = -45f;

        [Tooltip("Seconds the barrel takes to droop.")]
        public float DestructDroopDuration = 0.18f;

        [Tooltip("Seconds the turret takes to crumble down + sink.")]
        public float DestructCollapseDuration = 0.4f;

        [Tooltip("How far the turret sinks into the ground as it collapses.")]
        public float DestructSinkDepth = 0.35f;
    }

    [Serializable]
    public class WaveEnemyData
    {
        public EnemyTypeId Type = EnemyTypeId.Standard;
        [Min(1)] public int Count = 3;
    }

    /// <summary>
    ///     Difficulty-scaling knobs for the endless Survival mode (a <c>LevelDefinition</c> with <c>IsEndless</c>).
    ///     The spawner generates wave <c>w = 1,2,3…</c> where the count grows, the spawn interval tightens, and each
    ///     enemy type joins the random pool once <c>w</c> reaches its unlock wave — so Survival ramps forever.
    /// </summary>
    [Serializable]
    public class SurvivalData
    {
        [Header("Count per wave")]
        [Min(1)] public int BaseEnemiesPerWave = 4;
        [Min(0)] public int CountGrowthPerWave = 1;
        [Min(1)] public int MaxEnemiesPerWave = 16;

        [Header("Pace")]
        [Tooltip("Breather (s) between waves.")] [Min(0f)] public float WaveGap = 2f;
        [Min(0.05f)] public float BaseSpawnInterval = 1.1f;
        [Min(0f)] public float SpawnIntervalDecayPerWave = 0.04f;
        [Min(0.05f)] public float MinSpawnInterval = 0.4f;

        [Header("Enemy unlock (wave # a type joins the random pool)")]
        [Min(1)] public int LightUnlockWave = 1;
        [Min(1)] public int HeavyUnlockWave = 3;
        [Min(1)] public int FastUnlockWave = 5;
        [Min(1)] public int SpikyUnlockWave = 7;
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
        public GameObject LevelSelectPrefab;

        [Header("Audio")] public GameObject AudioHostPrefab;

        [Header("VFX")] [Tooltip("Authored particle prefab for the ring-out burst (look + material live on the asset).")]
        public GameObject RingoutBurstPrefab;

        [Tooltip("Authored lasso prefab: the rope line + wrap ring, with their material and line style baked in.")]
        public GameObject LassoRopePrefab;

        [Tooltip("Authored particle prefab for the rope-snap burst (look + material live on the asset).")]
        public GameObject RopeSnapBurstPrefab;

        [Tooltip("Authored particle prefab for the ultimate's ground shockwave ring (look + material live on the asset).")]
        public GameObject ShockwavePrefab;
    }
}