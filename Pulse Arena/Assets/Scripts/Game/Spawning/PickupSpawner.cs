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
        private int _alivePickups;
        private bool _paused;
        private float _spawnHeightOffset;
        private Transform _spawnParent;
        private Transform[] _spawnPoints;

        private Coroutine _spawnRoutine;

        public event Action<string, float> RarePickupSpawned;

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

        public void Initialize(Transform[] spawnPoints, Transform spawnParent, float spawnHeightOffset)
        {
            _spawnPoints = spawnPoints;
            _spawnParent = spawnParent;
            _spawnHeightOffset = spawnHeightOffset;
            _alivePickups = 0;
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
            if (_spawnPoints == null || _spawnPoints.Length == 0)
                return;

            Transform point = _spawnPoints[UnityEngine.Random.Range(0, _spawnPoints.Length)];
            Vector3 spawnPosition = point.position + Vector3.up * _spawnHeightOffset;

            HealthOrbPickup pickup = _pickupFactory.CreateHealthOrb(spawnPosition, point.rotation, _spawnParent);

            if (pickup == null)
                return;

            pickup.Collected += OnPickupCollected;

            _alivePickups++;
            RarePickupSpawned?.Invoke(_gameSettings.PickupData.RareSpawnMessage,
                _gameSettings.PickupData.SpawnToastDuration);
        }

        private void OnPickupCollected(HealthOrbPickup pickup)
        {
            pickup.Collected -= OnPickupCollected;
            _alivePickups = Mathf.Max(0, _alivePickups - 1);
        }
    }
}