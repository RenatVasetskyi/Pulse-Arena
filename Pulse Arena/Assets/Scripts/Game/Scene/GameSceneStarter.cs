using System;
using Game.Cameras;
using Game.Combat;
using Game.Enemy;
using Game.Player;
using Game.Player.Interfaces;
using Game.Spawning;
using UnityEngine;
using Zenject;

namespace Game.Scene
{
    public class GameSceneStarter : IInitializable, IDisposable
    {
        private readonly GameSceneReferences _sceneReferences;
        private readonly IPlayerFactory _playerFactory;
        private readonly IBattleCamera _battleCamera;
        private readonly IEnemySpawner _enemySpawner;
        private readonly IPickupSpawner _pickupSpawner;
        private OrbitCutter _orbitCutter;
        private EnemySlingshot _enemySlingshot;

        public GameSceneStarter(
            GameSceneReferences sceneReferences,
            IPlayerFactory playerFactory,
            IBattleCamera battleCamera,
            IEnemySpawner enemySpawner,
            IPickupSpawner pickupSpawner)
        {
            _sceneReferences = sceneReferences;
            _playerFactory = playerFactory;
            _battleCamera = battleCamera;
            _enemySpawner = enemySpawner;
            _pickupSpawner = pickupSpawner;
        }

        public void Initialize()
        {
            _sceneReferences.Validate();

            PlayerController player = SpawnPlayer();
            _battleCamera.Follow(player.transform);
            SubscribeToCombat(player);

            _enemySpawner.Initialize(player.transform, _sceneReferences.EnemySpawnPoints,
                _sceneReferences.EnemySpawnParent, _sceneReferences.EnemySpawnHeightOffset);
            _pickupSpawner.Initialize(_sceneReferences.PickupSpawnPoints, _sceneReferences.PickupSpawnParent,
                _sceneReferences.PickupSpawnHeightOffset);

            _enemySpawner.StartSpawn();
            _pickupSpawner.StartSpawn();
        }

        public void Dispose()
        {
            if (_orbitCutter != null)
                _orbitCutter.BurstUsed -= OnOrbitBurstUsed;

            if (_enemySlingshot != null)
            {
                _enemySlingshot.EnemyGrabbed -= OnEnemyGrabbed;
                _enemySlingshot.EnemyLaunched -= OnEnemyLaunched;
            }

            _enemySpawner.StopSpawn();
            _pickupSpawner.StopSpawn();
        }

        private PlayerController SpawnPlayer()
        {
            Transform spawnPoint = _sceneReferences.PlayerSpawnPoint;
            return _playerFactory.Create(_sceneReferences.PlayerSpawnPosition, spawnPoint.rotation,
                _sceneReferences.PlayerParent);
        }

        private void SubscribeToCombat(PlayerController player)
        {
            _orbitCutter = player.GetComponent<OrbitCutter>();
            _enemySlingshot = player.GetComponent<EnemySlingshot>();

            if (_orbitCutter != null)
                _orbitCutter.BurstUsed += OnOrbitBurstUsed;

            if (_enemySlingshot != null)
            {
                _enemySlingshot.EnemyGrabbed += OnEnemyGrabbed;
                _enemySlingshot.EnemyLaunched += OnEnemyLaunched;
            }
        }

        private void OnOrbitBurstUsed()
        {
            _battleCamera.Shake(0.22f, 0.45f);
        }

        private void OnEnemyGrabbed()
        {
            _battleCamera.Shake(0.08f, 0.18f);
        }

        private void OnEnemyLaunched()
        {
            _battleCamera.Shake(0.24f, 0.55f);
        }
    }
}
