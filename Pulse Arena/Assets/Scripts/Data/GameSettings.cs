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
        public int MaxHealth = 3;
    }

    [Serializable]
    public class EnemyData
    {
        public float MoveSpeed = 3.5f;
        public float RotationSpeed = 8f;
        public float KnockbackDuration = 0.45f;
        public int ScoreReward = 1;
    }

    [Serializable]
    public class PulseData
    {
        public float Radius = 5f;
        public float Force = 18f;
        public float Cooldown = 0.35f;
        public float MaxCharge = 100f;
        public float EnergyPerPickup = 25f;
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
    }
}
