using System.Collections;
using Data;
using Game.Enemy.Interfaces;
using UnityEngine;
using Zenject;

namespace Game.Enemy
{
    public class EnemySpawner : MonoBehaviour, IEnemySpawner
    {
        [SerializeField] private Transform _target;
        [SerializeField] private Transform _spawnParent;
        [SerializeField] private Transform[] _spawnPoints;

        private IEnemyFactory _enemyFactory;
        private GameSettings _gameSettings;
        private Coroutine _spawnRoutine;
        private int _aliveEnemies;

        [Inject]
        public void Construct(IEnemyFactory enemyFactory, GameSettings gameSettings)
        {
            _enemyFactory = enemyFactory;
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
                if (_aliveEnemies < _gameSettings.SpawnData.MaxEnemies)
                    Spawn();

                yield return new WaitForSeconds(_gameSettings.SpawnData.EnemySpawnDelay);
            }
        }

        private void Spawn()
        {
            if (_spawnPoints.Length == 0)
                return;

            Transform point = _spawnPoints[Random.Range(0, _spawnPoints.Length)];

            EnemyController enemy = _enemyFactory.Create(point.position, point.rotation, _spawnParent, _target);
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
