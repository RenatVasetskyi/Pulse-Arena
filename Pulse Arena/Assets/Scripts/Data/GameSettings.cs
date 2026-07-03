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
        public float MaxCharge = 100f;
        public float EnergyPerPickup = 25f;
        public float VisualDuration = 0.32f;
        public float VisualStartRadius = 0.5f;
        public float VisualHeight = 0.08f;
        public float VisualWidth = 0.12f;
        public Color VisualColor = new(0.45f, 0.9f, 1f, 0.85f);
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
