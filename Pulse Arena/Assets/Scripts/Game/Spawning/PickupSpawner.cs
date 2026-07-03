using System.Collections;
using Data;
using Game.Pickups;
using Game.Pickups.Interfaces;
using UnityEngine;
using Zenject;

namespace Game.Spawning
{
    public class PickupSpawner : MonoBehaviour
    {
        [SerializeField] private Transform _spawnParent;
        [SerializeField] private Transform[] _spawnPoints;

        private IPickupFactory _pickupFactory;
        private GameSettings _gameSettings;
        private Coroutine _spawnRoutine;
        private int _alivePickups;

        [Inject]
        public void Construct(IPickupFactory pickupFactory, GameSettings gameSettings)
        {
            _pickupFactory = pickupFactory;
            _gameSettings = gameSettings;
        }

        public void StartSpawn()
        {
            if (_spawnRoutine != null)
                return;

            _spawnRoutine = StartCoroutine(SpawnLoop());
        }

        public void StopSpawn()
        {
            if (_spawnRoutine == null)
                return;

            StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
        }

        private IEnumerator SpawnLoop()
        {
            while (true)
            {
                if (_alivePickups < _gameSettings.SpawnData.MaxPickups)
                    Spawn();

                yield return new WaitForSeconds(_gameSettings.SpawnData.PickupSpawnDelay);
            }
        }

        private void Spawn()
        {
            if (_spawnPoints.Length == 0)
                return;

            Transform point = _spawnPoints[Random.Range(0, _spawnPoints.Length)];

            EnergyPickup pickup = _pickupFactory.Create(point.position, point.rotation, _spawnParent);
            pickup.Collected += OnPickupCollected;

            _alivePickups++;
        }

        private void OnPickupCollected(EnergyPickup pickup)
        {
            pickup.Collected -= OnPickupCollected;
            _alivePickups = Mathf.Max(0, _alivePickups - 1);
        }
    }
}
