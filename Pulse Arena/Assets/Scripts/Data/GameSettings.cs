using System;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "Game Settings", menuName = "Pulse Arena/Game Settings")]
    public class GameSettings : ScriptableObject
    {
        [Header("Bootstrap")]
        public int TargetFrameRate = 120;
        public string GameSceneName = SceneName.Game;

        [Header("Configs")]
        public PlayerData PlayerData;
        public EnemyData EnemyData;
        public PulseData PulseData;
        public OrbitCutterData OrbitCutterData;
        public SlingshotData SlingshotData;
        public WeaponData WeaponData;
        public PickupData PickupData;
        public SpawnData SpawnData;

        [Header("Prefabs")]
        public PrefabData Prefabs;
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
    }

    [Serializable]
    public class EnemyData
    {
        public float MoveSpeed = 3.5f;
        public float RotationSpeed = 8f;
        public float KnockbackDuration = 0.45f;
        public float ExtraGravity = 45f;
        public int MaxHealth = 3;
        public int ScoreReward = 1;
        public float HitFlashDuration = 0.12f;
        public Color HitFlashColor = new(1f, 0.08f, 0.03f, 1f);
        public float AttackRange = 1.05f;
        public float AttackCooldown = 0.9f;
        public int ContactDamage = 1;
        public float ImpactDamageMinSpeed = 7f;
        public int ImpactDamage = 1;
        public float ImpactDamageCooldown = 0.2f;
    }

    [Serializable]
    public class OrbitCutterData
    {
        public float Radius = 1.65f;
        public float Height = 0.7f;
        public float AngularSpeed = 420f;
        public float HitRadius = 0.55f;
        public float HitCooldown = 0.28f;
        public int Damage = 1;
        public float KnockbackForce = 5f;
        public float VisualScale = 0.42f;
        public float BladeLength = 1.2f;
        public float BladeWidth = 0.18f;
        public Color CoreColor = new(0.2f, 0.95f, 1f, 1f);
        public Color TrailColor = new(0.25f, 0.9f, 1f, 0.72f);
        public LayerMask EnemyLayer;
    }

    [Serializable]
    public class SlingshotData
    {
        public float GrabRadius = 6f;
        public float HoldRadius = 1.85f;
        public float HoldHeight = 0.75f;
        public float HoldAngularSpeed = 220f;
        public float HoldFollowSpeed = 18f;
        public float LaunchForce = 20f;
        public float LaunchUpwardRatio = 0.22f;
        public float LaunchDuration = 0.75f;
        public float Cooldown = 0.45f;
        public float LineWidth = 0.08f;
        public Color LineColor = new(1f, 0.4f, 0.95f, 0.9f);
        public LayerMask EnemyLayer;
    }

    [Serializable]
    public class WeaponData
    {
        public float Range = 9f;
        public float Radius = 0.45f;
        public int Damage = 1;
        public float Cooldown = 0.2f;
        public float ProjectileSpeed = 16f;
        public float ProjectileLifetime = 1.2f;
        public float ImpactLifetime = 0.35f;
        public float KnockbackForce = 4f;
        public float OrbitRadius = 1.2f;
        public float OrbitHeight = 0.7f;
        public float OrbitAngularSpeed = 240f;
        public float OrbitVisualScale = 0.7f;
        public float ProjectileVisualScale = 0.45f;
        public float ImpactVisualScale = 0.45f;
        public float OriginHeight = 0.7f;
        public float ForwardOffset = 0.65f;
        public LayerMask EnemyLayer;
    }

    [Serializable]
    public class PulseData
    {
        public float Radius = 5f;
        public float Force = 16f;
        public float Cooldown = 1.5f;
        public float UpwardForceRatio = 0.6f;
        public float MinForceMultiplier = 0.45f;
        public float PullRadius = 6f;
        public float PullStopRadius = 2.6f;
        public float PullForce = 14f;
        public float PullCooldown = 3f;
        public float PullStasisDuration = 0.65f;
        public float PullUpwardForceRatio = 0.18f;
        public float MaxCharge = 100f;
        public float EnergyPerPickup = 25f;
        public float VisualDuration = 0.32f;
        public float VisualStartRadius = 0.5f;
        public float VisualHeight = 0.08f;
        public float VisualWidth = 0.12f;
        public Color VisualColor = new(0.45f, 0.9f, 1f, 0.85f);
        public Color PullVisualColor = new(0.95f, 0.35f, 1f, 0.85f);
        public LayerMask EnemyLayer;
    }

    [Serializable]
    public class PickupData
    {
        public float RotateSpeed = 120f;
        public float BobHeight = 0.25f;
        public float BobSpeed = 3f;
    }

    [Serializable]
    public class SpawnData
    {
        public float EnemySpawnDelay = 1.2f;
        public float PickupSpawnDelay = 2f;
        public int MaxEnemies = 16;
        public int MaxPickups = 6;
    }

    [Serializable]
    public class PrefabData
    {
        public GameObject PlayerPrefab;
        public GameObject EnemyPrefab;
        public GameObject EnergyPickupPrefab;
        public GameObject PulseViewPrefab;
        public GameObject ProjectilePrefab;
        public GameObject ProjectileImpactPrefab;
    }
}
