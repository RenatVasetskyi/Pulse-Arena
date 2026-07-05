using System;
using System.Collections;
using System.Collections.Generic;
using Architecture.Services.Interfaces;
using Data;
using Game.Enemy.Interfaces;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Enemy
{
    public class EnemySpawner : IEnemySpawner
    {
        public event Action<int, int> WaveChanged;
        public event Action AllWavesCleared;

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

            _spawnRoutine = _coroutineRunner.StartCoroutine(HasWaves() ? WaveRoutine() : SpawnLoop());
        }

        private bool HasWaves()
        {
            return _gameSettings.Waves != null && _gameSettings.Waves.Length > 0;
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
                if (_aliveEnemies < _gameSettings.SpawnData.MaxEnemies)
                    Spawn(PickEnemyType());

                yield return new WaitForSeconds(_gameSettings.SpawnData.EnemySpawnDelay);
            }
        }

        private IEnumerator WaveRoutine()
        {
            WaveData[] waves = _gameSettings.Waves;

            for (int waveIndex = 0; waveIndex < waves.Length; waveIndex++)
            {
                WaveData wave = waves[waveIndex];

                if (wave == null || wave.Enemies == null || wave.Enemies.Length == 0)
                    continue;

                WaveChanged?.Invoke(waveIndex + 1, waves.Length);
                yield return new WaitForSeconds(Mathf.Max(0f, wave.DelayBeforeWave));

                List<EnemyTypeData> spawnQueue = BuildWaveQueue(wave);

                foreach (EnemyTypeData type in spawnQueue)
                {
                    while (_aliveEnemies >= _gameSettings.SpawnData.MaxEnemies)
                        yield return new WaitForSeconds(0.25f);

                    Spawn(type);
                    yield return new WaitForSeconds(Mathf.Max(0.05f, wave.SpawnInterval));
                }

                while (_aliveEnemies > 0)
                    yield return new WaitForSeconds(0.25f);
            }

            _spawnRoutine = null;
            AllWavesCleared?.Invoke();
        }

        private List<EnemyTypeData> BuildWaveQueue(WaveData wave)
        {
            List<EnemyTypeData> queue = new();

            foreach (WaveEnemyData entry in wave.Enemies)
            {
                if (entry == null)
                    continue;

                EnemyTypeData type = _gameSettings.GetEnemyType(entry.Type);

                for (int i = 0; i < entry.Count; i++)
                    queue.Add(type);
            }

            for (int i = queue.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (queue[i], queue[j]) = (queue[j], queue[i]);
            }

            return queue;
        }

        private void Spawn(EnemyTypeData type)
        {
            if (_spawnPoints == null || _spawnPoints.Length == 0)
                return;

            Transform point = _spawnPoints[Random.Range(0, _spawnPoints.Length)];
            Vector3 spawnPosition = point.position + Vector3.up * _spawnHeightOffset;

            EnemyController enemy = _enemyFactory.Create(spawnPosition, point.rotation, _spawnParent, _target,
                type);
            enemy.Destroyed += OnEnemyDestroyed;
            _aliveEnemies++;
        }

        private EnemyTypeData PickEnemyType()
        {
            EnemyTypeData[] types = _gameSettings.EnemyTypes;

            if (types == null || types.Length == 0)
                return null;

            float totalWeight = 0f;

            foreach (EnemyTypeData type in types)
            {
                if (type != null)
                    totalWeight += Mathf.Max(0f, type.SpawnWeight);
            }

            if (totalWeight <= 0f)
                return types[0];

            float roll = Random.value * totalWeight;

            foreach (EnemyTypeData type in types)
            {
                if (type == null)
                    continue;

                roll -= Mathf.Max(0f, type.SpawnWeight);

                if (roll <= 0f)
                    return type;
            }

            return types[^1];
        }

        private void OnEnemyDestroyed(EnemyController enemy)
        {
            enemy.Destroyed -= OnEnemyDestroyed;
            _aliveEnemies = Mathf.Max(0, _aliveEnemies - 1);
        }
    }
}
