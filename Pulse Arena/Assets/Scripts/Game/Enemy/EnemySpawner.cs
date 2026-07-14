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
    /// <summary>
    ///     Spawns the enemies for the currently-SELECTED level (<see cref="ILevelService" />): a fixed
    ///     <see cref="WaveRoutine" /> for a campaign level (fires <see cref="AllWavesCleared" /> on completion → win),
    ///     or an endless, escalating <see cref="SurvivalRoutine" /> for an <c>IsEndless</c> level (never wins — the
    ///     player plays until they die). The level also drives the on-screen enemy cap (<see cref="MaxEnemies" />).
    /// </summary>
    public class EnemySpawner : IEnemySpawner, IPausable
    {
        private readonly ICoroutineRunner _coroutineRunner;
        private readonly IEnemyFactory _enemyFactory;
        private readonly GameSettings _gameSettings;
        private readonly ILevelService _levelService;
        private readonly IPauseService _pauseService;
        private readonly ISafeSpawnFinder _placementFinder = new SafeSpawnFinder();
        private int _aliveEnemies;
        private int _livingEnemies;
        private bool _paused;
        private bool _waveClearPending;
        private float _spawnHeightOffset;
        private Transform _spawnParent;

        private Coroutine _spawnRoutine;
        private Transform _target;
        public event Action AllWavesCleared;
        public event Action<int, int> WaveChanged;
        public event Action WaveCleared;

        public EnemySpawner(ICoroutineRunner coroutineRunner, IEnemyFactory enemyFactory, GameSettings gameSettings,
            ILevelService levelService, IPauseService pauseService)
        {
            _coroutineRunner = coroutineRunner;
            _enemyFactory = enemyFactory;
            _gameSettings = gameSettings;
            _levelService = levelService;
            _pauseService = pauseService;
        }

        private LevelDefinition CurrentLevel => _levelService.Selected;

        // On-screen enemy cap for this level (falls back to the global default when no level is selected).
        private int MaxEnemies =>
            CurrentLevel != null ? CurrentLevel.MaxEnemiesOnScreen : _gameSettings.SpawnData.MaxEnemies;

        private WaveData[] CurrentWaves =>
            CurrentLevel != null ? CurrentLevel.Waves : _gameSettings.Waves;

        /// <summary>Mechanical pause: stop the spawn timer accumulating (PausableWait holds), so no spawns fire while paused.</summary>
        public void Pause()
        {
            _paused = true;
        }

        public void Resume()
        {
            _paused = false;
        }

        public void Initialize(Transform target, Vector3 center, Transform spawnParent, float spawnHeightOffset)
        {
            _target = target;
            _spawnParent = spawnParent;
            _spawnHeightOffset = spawnHeightOffset;
            _aliveEnemies = 0;
            _livingEnemies = 0;
            _placementFinder.Initialize(center, target, _gameSettings.SpawnAreaData, BlockerMask());
        }

        public void StartSpawn()
        {
            if (_spawnRoutine != null)
                return;

            _spawnRoutine = _coroutineRunner.StartCoroutine(PickRoutine());
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
        // so one clearance test keeps a fresh spawn out of all of them.
        private LayerMask BlockerMask()
        {
            return _gameSettings.SlingshotData.ObstacleLayer.value | _gameSettings.SlingshotData.EnemyLayer.value;
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

        // Endless Survival wins nothing → runs SurvivalRoutine; a campaign level runs its fixed waves; a level with
        // no waves authored falls back to the plain weighted loop. Plain method (not an iterator) so it can branch.
        private IEnumerator PickRoutine()
        {
            if (CurrentLevel != null && CurrentLevel.IsEndless)
                return SurvivalRoutine();

            return HasWaves() ? WaveRoutine() : SpawnLoop();
        }

        private bool HasWaves()
        {
            return CurrentWaves != null && CurrentWaves.Length > 0;
        }

        private IEnumerator SpawnLoop()
        {
            while (true)
            {
                if (_aliveEnemies < MaxEnemies)
                    Spawn(PickEnemyType());

                yield return PausableWait(_gameSettings.SpawnData.EnemySpawnDelay);
            }
        }

        private IEnumerator WaveRoutine()
        {
            WaveData[] waves = CurrentWaves;
            float pollInterval = Mathf.Max(0.05f, _gameSettings.SpawnData.WavePollInterval);

            for (int waveIndex = 0; waveIndex < waves.Length; waveIndex++)
            {
                WaveData wave = waves[waveIndex];

                if (wave == null || wave.Enemies == null || wave.Enemies.Length == 0)
                    continue;

                _waveClearPending = false;
                WaveChanged?.Invoke(waveIndex + 1, waves.Length);
                yield return PausableWait(Mathf.Max(0f, wave.DelayBeforeWave));

                List<EnemyTypeData> spawnQueue = BuildWaveQueue(wave);

                foreach (EnemyTypeData type in spawnQueue)
                {
                    while (_aliveEnemies >= MaxEnemies)
                        yield return PausableWait(pollInterval);

                    Spawn(type);
                    yield return PausableWait(Mathf.Max(0.05f, wave.SpawnInterval));
                }

                // Wave fully spawned — from now the moment _livingEnemies hits 0 is this wave's LAST KILL (the lethal
                // hit / ring-out, via OnEnemyDied — NOT the ~1s-later pool return), which is where the slow-mo must
                // land. Skip the final wave: its clear rolls straight into the victory sequence (AllWavesCleared),
                // and a breather slow-mo there would fight the game-over freeze.
                _waveClearPending = waveIndex < waves.Length - 1;

                if (_waveClearPending && _livingEnemies == 0)
                    RaiseWaveCleared();

                while (_aliveEnemies > 0)
                    yield return PausableWait(pollInterval);
            }

            _spawnRoutine = null;
            AllWavesCleared?.Invoke();
        }

        // Endless: wave w spawns more enemies, faster, from a pool that unlocks tougher types as w climbs. The
        // on-screen cap throttles the queue, so a slow player gets pinned at MaxEnemies rather than buried instantly.
        // Never fires AllWavesCleared — Survival ends only when the player dies (the normal lose flow).
        private IEnumerator SurvivalRoutine()
        {
            SurvivalData survival = _gameSettings.SurvivalData;
            float pollInterval = Mathf.Max(0.05f, _gameSettings.SpawnData.WavePollInterval);
            int wave = 1;

            while (true)
            {
                WaveChanged?.Invoke(wave, 0); // total 0 = endless; the HUD shows a bare "Wave N"
                yield return PausableWait(survival.WaveGap);

                int count = Mathf.Min(survival.MaxEnemiesPerWave,
                    survival.BaseEnemiesPerWave + (wave - 1) * survival.CountGrowthPerWave);
                float interval = Mathf.Max(survival.MinSpawnInterval,
                    survival.BaseSpawnInterval - (wave - 1) * survival.SpawnIntervalDecayPerWave);

                List<EnemyTypeData> spawnQueue = BuildSurvivalQueue(wave, count, survival);

                foreach (EnemyTypeData type in spawnQueue)
                {
                    while (_aliveEnemies >= MaxEnemies)
                        yield return PausableWait(pollInterval);

                    Spawn(type);
                    yield return PausableWait(interval);
                }

                // Wait for the whole wave to be cleared before escalating — otherwise waves pile on top of each other
                // (the on-screen cap alone just keeps the arena full without ever "finishing" a wave).
                while (_aliveEnemies > 0)
                    yield return PausableWait(pollInterval);

                wave++;
            }
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

            Shuffle(queue);
            return queue;
        }

        // Build a random spawn queue of `count` enemies from the types unlocked by this wave (Standard is always in).
        private List<EnemyTypeData> BuildSurvivalQueue(int wave, int count, SurvivalData survival)
        {
            List<EnemyTypeId> pool = new() { EnemyTypeId.Standard };

            if (wave >= survival.LightUnlockWave)
                pool.Add(EnemyTypeId.Light);

            if (wave >= survival.HeavyUnlockWave)
                pool.Add(EnemyTypeId.Heavy);

            if (wave >= survival.FastUnlockWave)
                pool.Add(EnemyTypeId.Fast);

            if (wave >= survival.SpikyUnlockWave)
                pool.Add(EnemyTypeId.Spiky);

            List<EnemyTypeData> queue = new();

            for (int i = 0; i < count; i++)
                queue.Add(_gameSettings.GetEnemyType(pool[Random.Range(0, pool.Count)]));

            return queue;
        }

        private static void Shuffle(List<EnemyTypeData> queue)
        {
            for (int i = queue.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (queue[i], queue[j]) = (queue[j], queue[i]);
            }
        }

        private void Spawn(EnemyTypeData type)
        {
            if (!_placementFinder.TryFind(out Vector3 position))
                return;

            Vector3 spawnPosition = position + Vector3.up * _spawnHeightOffset;
            Quaternion rotation = Quaternion.Euler(0f, Random.value * 360f, 0f);

            EnemyController enemy = _enemyFactory.Create(spawnPosition, rotation, _spawnParent, _target, type);
            enemy.Destroyed += OnEnemyDestroyed;
            enemy.Died += OnEnemyDied;
            _aliveEnemies++;
            _livingEnemies++;
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

        // Pool-return bookkeeping: drives the on-screen cap + the wave-progression gate (both want "corpse gone").
        private void OnEnemyDestroyed(EnemyController enemy)
        {
            enemy.Destroyed -= OnEnemyDestroyed;
            _aliveEnemies = Mathf.Max(0, _aliveEnemies - 1);
        }

        // Moment-of-death bookkeeping: the instant the wave's last enemy takes its lethal hit / rings out, so the
        // slow-mo lands ON the kill instead of ~1s later (when the corpse finally returns to the pool).
        private void OnEnemyDied(EnemyController enemy)
        {
            enemy.Died -= OnEnemyDied;
            _livingEnemies = Mathf.Max(0, _livingEnemies - 1);

            if (_waveClearPending && _livingEnemies == 0)
                RaiseWaveCleared();
        }

        // Fires WaveCleared exactly once per wave — guarded by the pending flag so a mid-spawn dip to zero or a
        // second death on the same frame can't re-raise it.
        private void RaiseWaveCleared()
        {
            if (!_waveClearPending)
                return;

            _waveClearPending = false;
            WaveCleared?.Invoke();
        }
    }
}
