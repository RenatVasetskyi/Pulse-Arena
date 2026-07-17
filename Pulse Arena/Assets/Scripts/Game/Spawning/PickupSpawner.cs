using System;
using System.Collections;
using Architecture.Services.Interfaces;
using Data;
using Game.Pickups;
using Game.Pickups.Interfaces;
using UnityEngine;
using Random = System.Random;

namespace Game.Spawning
{
    public class PickupSpawner : IPickupSpawner, IPausable
    {
        private readonly ICoroutineRunner _coroutineRunner;
        private readonly GameSettings _gameSettings;
        private readonly IPauseService _pauseService;
        private readonly IPickupFactory _pickupFactory;
        private readonly ISafeSpawnFinder _placementFinder = new SafeSpawnFinder();
        private int _alivePickups;
        private bool _paused;
        private float _spawnHeightOffset;
        private Transform _spawnParent;

        private Coroutine _spawnRoutine;

        public event Action<string, float> PickupSpawned;

        public PickupSpawner(ICoroutineRunner coroutineRunner, IPickupFactory pickupFactory, GameSettings gameSettings,
            IPauseService pauseService)
        {
            _coroutineRunner = coroutineRunner;
            _pickupFactory = pickupFactory;
            _gameSettings = gameSettings;
            _pauseService = pauseService;
        }

        public void Pause()
        {
            _paused = true;
        }

        public void Resume()
        {
            _paused = false;
        }

        // WaitForSeconds that holds its remaining time while paused instead of restarting the interval.
        private IEnumerator PausableWait(float seconds)
        {
            float remaining = seconds;

            while (remaining > 0f)
            {
                if (!_paused)
                    remaining -= Time.deltaTime;

                yield return null;
            }
        }

        public void Initialize(Vector3 center, Transform player, Transform spawnParent, float spawnHeightOffset)
        {
            _spawnParent = spawnParent;
            _spawnHeightOffset = spawnHeightOffset;
            _alivePickups = 0;
            _placementFinder.Initialize(center, player, _gameSettings.SpawnAreaData, BlockerMask());
        }

        public void StartSpawn()
        {
            if (_spawnRoutine != null)
                return;

            _spawnRoutine = _coroutineRunner.StartCoroutine(SpawnLoop());
            _pauseService.Register(this);
        }

        public void StopSpawn()
        {
            _pauseService?.Unregister(this);

            if (_spawnRoutine == null)
                return;

            try
            {
                _coroutineRunner.StopCoroutine(_spawnRoutine);
            }
            catch (MissingReferenceException)
            {
                // Unity can destroy the runner before Zenject disposes local scene services.
            }
            finally
            {
                _spawnRoutine = null;
            }
        }
        
        // Walls/boxes and the Default-layer pit & pickup triggers (ObstacleLayer) plus live enemies (EnemyLayer),
        // so one clearance test keeps a fresh orb out of walls, off pits, and off other spawns.
        private LayerMask BlockerMask()
        {
            return _gameSettings.SlingshotData.ObstacleLayer.value | _gameSettings.SlingshotData.EnemyLayer.value;
        }

        private IEnumerator SpawnLoop()
        {
            while (true)
            {
                yield return PausableWait(_gameSettings.SpawnData.PickupSpawnDelay);

                if (_alivePickups < _gameSettings.SpawnData.MaxPickups)
                    Spawn();
            }
        }

        private void Spawn()
        {
            if (!_placementFinder.TryFind(out Vector3 position))
                return;

            Vector3 spawnPosition = position + Vector3.up * _spawnHeightOffset;
            Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.value * 360f, 0f);

            HealthOrbPickup pickup = _pickupFactory.CreateHealthOrb(spawnPosition, rotation, _spawnParent);

            if (pickup == null)
                return;

            pickup.Collected += OnPickupCollected;

            _alivePickups++;
            PickupSpawned?.Invoke(_gameSettings.PickupData.RareSpawnMessage,
                _gameSettings.PickupData.SpawnToastDuration);
        }

        private void OnPickupCollected(HealthOrbPickup pickup)
        {
            pickup.Collected -= OnPickupCollected;
            _alivePickups = Mathf.Max(0, _alivePickups - 1);
        }
    }
}