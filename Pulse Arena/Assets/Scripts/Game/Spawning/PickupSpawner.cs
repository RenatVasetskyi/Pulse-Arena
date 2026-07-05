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
    public class PickupSpawner : IPickupSpawner
    {
        private readonly ICoroutineRunner _coroutineRunner;
        private readonly IPickupFactory _pickupFactory;
        private readonly GameSettings _gameSettings;

        private Coroutine _spawnRoutine;
        private Transform _spawnParent;
        private Transform[] _spawnPoints;
        private float _spawnHeightOffset;
        private int _alivePickups;

        public event Action<string, float> RarePickupSpawned;

        public PickupSpawner(ICoroutineRunner coroutineRunner, IPickupFactory pickupFactory, GameSettings gameSettings)
        {
            _coroutineRunner = coroutineRunner;
            _pickupFactory = pickupFactory;
            _gameSettings = gameSettings;
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
        }

        public void StopSpawn()
        {
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
                yield return new WaitForSeconds(_gameSettings.SpawnData.PickupSpawnDelay);

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
