using System;
using System.Collections;
using System.Collections.Generic;
using Architecture.Services.Interfaces;
using Data;
using Game.Enemy.Interfaces;
using Game.Spawning;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Enemy
{
    public class EnemySpawner : IEnemySpawner, IPausable
    {
        private readonly ICoroutineRunner _coroutineRunner;
        private readonly IEnemyFactory _enemyFactory;
        private readonly GameSettings _gameSettings;
        private readonly IPauseService _pauseService;
        private readonly ISafeSpawnFinder _placementFinder = new SafeSpawnFinder();
        private int _aliveEnemies;
        private bool _paused;
        private float _spawnHeightOffset;
        private Transform _spawnParent;

        private Coroutine _spawnRoutine;
        private Transform _target;
        public event Action AllWavesCleared;
        public event Action<int, int> WaveChanged;

        public EnemySpawner(ICoroutineRunner coroutineRunner, IEnemyFactory enemyFactory, GameSettings gameSettings,
            IPauseService pauseService)
        {
            _coroutineRunner = coroutineRunner;
            _enemyFactory = enemyFactory;
            _gameSettings = gameSettings;
            _pauseService = pauseService;
        }

        /// <summary>Mechanical pause: stop the spawn timer accumulating (PausableWait holds), so no spawns fire while paused.</summary>
        public void Pause()
        {
            _paused = true;
        }

        public void Resume()
        {
            _paused = false;
        }

        // A WaitForSeconds that freezes while paused — it holds its remaining time instead of restarting the
        // interval on resume (StopCoroutine would lose the elapsed time WaitForSeconds hides).
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

        public void Initialize(Transform target, Vector3 center, Transform spawnParent, float spawnHeightOffset)
        {
            _target = target;
            _spawnParent = spawnParent;
            _spawnHeightOffset = spawnHeightOffset;
            _aliveEnemies = 0;
            _placementFinder.Initialize(center, target, _gameSettings.SpawnAreaData, BlockerMask());
        }

        // Walls/boxes and the Default-layer pit & pickup triggers (ObstacleLayer) plus live enemies (EnemyLayer),
        // so one clearance test keeps a fresh spawn out of all of them.
        private LayerMask BlockerMask()
        {
            return _gameSettings.SlingshotData.ObstacleLayer.value | _gameSettings.SlingshotData.EnemyLayer.value;
        }

        public void StartSpawn()
        {
            if (_spawnRoutine != null)
                return;

            _spawnRoutine = _coroutineRunner.StartCoroutine(HasWaves() ? WaveRoutine() : SpawnLoop());
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

        private bool HasWaves()
        {
            return _gameSettings.Waves != null && _gameSettings.Waves.Length > 0;
        }

        private IEnumerator SpawnLoop()
        {
            while (true)
            {
                if (_aliveEnemies < _gameSettings.SpawnData.MaxEnemies)
                    Spawn(PickEnemyType());

                yield return PausableWait(_gameSettings.SpawnData.EnemySpawnDelay);
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
                yield return PausableWait(Mathf.Max(0f, wave.DelayBeforeWave));

                List<EnemyTypeData> spawnQueue = BuildWaveQueue(wave);

                float pollInterval = Mathf.Max(0.05f, _gameSettings.SpawnData.WavePollInterval);

                foreach (EnemyTypeData type in spawnQueue)
                {
                    while (_aliveEnemies >= _gameSettings.SpawnData.MaxEnemies)
                        yield return PausableWait(pollInterval);

                    Spawn(type);
                    yield return PausableWait(Mathf.Max(0.05f, wave.SpawnInterval));
                }

                while (_aliveEnemies > 0)
                    yield return PausableWait(pollInterval);
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
            if (!_placementFinder.TryFind(out Vector3 position))
                return;

            Vector3 spawnPosition = position + Vector3.up * _spawnHeightOffset;
            Quaternion rotation = Quaternion.Euler(0f, Random.value * 360f, 0f);

            EnemyController enemy = _enemyFactory.Create(spawnPosition, rotation, _spawnParent, _target, type);
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