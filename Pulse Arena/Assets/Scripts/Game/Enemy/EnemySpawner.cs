using System.Collections;
using Architecture.Services.Interfaces;
using Data;
using Game.Enemy.Interfaces;
using UnityEngine;

namespace Game.Enemy
{
    public class EnemySpawner : IEnemySpawner
    {
        private readonly ICoroutineRunner _coroutineRunner;
        private readonly IEnemyFactory _enemyFactory;
        private readonly GameSettings _gameSettings;

        private Coroutine _spawnRoutine;
        private Transform _target;
        private Transform _spawnParent;
        private Transform[] _spawnPoints;
        private float _spawnHeightOffset;
        private int _aliveEnemies;

        public EnemySpawner(ICoroutineRunner coroutineRunner, IEnemyFactory enemyFactory, GameSettings gameSettings)
        {
            _coroutineRunner = coroutineRunner;
            _enemyFactory = enemyFactory;
            _gameSettings = gameSettings;
        }

        public void Initialize(Transform target, Transform[] spawnPoints, Transform spawnParent, float spawnHeightOffset)
        {
            _target = target;
            _spawnPoints = spawnPoints;
            _spawnParent = spawnParent;
            _spawnHeightOffset = spawnHeightOffset;
            _aliveEnemies = 0;
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

            _coroutineRunner.StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
        }

        private IEnumerator SpawnLoop()
        {
            while (true)
            {
                if (_aliveEnemies < _gameSettings.SpawnData.MaxEnemies)
                    Spawn();

                yield return new WaitForSeconds(_gameSettings.SpawnData.EnemySpawnDelay);
            }
        }

        private void Spawn()
        {
            if (_spawnPoints == null || _spawnPoints.Length == 0)
                return;

            Transform point = _spawnPoints[Random.Range(0, _spawnPoints.Length)];
            Vector3 spawnPosition = point.position + Vector3.up * _spawnHeightOffset;

            EnemyController enemy = _enemyFactory.Create(spawnPosition, point.rotation, _spawnParent, _target);
            enemy.Destroyed += OnEnemyDestroyed;
            _aliveEnemies++;
        }

        private void OnEnemyDestroyed(EnemyController enemy)
        {
            enemy.Destroyed -= OnEnemyDestroyed;
            _aliveEnemies = Mathf.Max(0, _aliveEnemies - 1);
        }
    }
}
