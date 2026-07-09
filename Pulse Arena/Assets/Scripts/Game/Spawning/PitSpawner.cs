using System.Collections;
using Architecture.Services.Interfaces;
using Data;
using Game.Arena;
using Game.Arena.Interfaces;
using UnityEngine;

namespace Game.Spawning
{
    /// <summary>
    ///     Spawns transient <see cref="Pit" /> hazards inside the play ring at random size and lifetime, up to a
    ///     cap. Owns the spawn cadence + active-pit bookkeeping; the placement geometry lives in
    ///     <see cref="IPitPlacementFinder" />. Each pit frees its slot when it despawns (eaten or timed out).
    /// </summary>
    public class PitSpawner : IPitSpawner
    {
        private readonly ICoroutineRunner _coroutineRunner;
        private readonly GameSettings _gameSettings;
        private readonly IPitFactory _pitFactory;
        private readonly IPitPlacementFinder _placementFinder = new PitPlacementFinder();
        private int _activePits;
        private Transform _parent;

        private Coroutine _spawnRoutine;

        public PitSpawner(ICoroutineRunner coroutineRunner, IPitFactory pitFactory, GameSettings gameSettings)
        {
            _coroutineRunner = coroutineRunner;
            _pitFactory = pitFactory;
            _gameSettings = gameSettings;
        }

        public void Initialize(Vector3 center, Transform player, Transform parent)
        {
            _parent = parent;
            _activePits = 0;
            _placementFinder.Initialize(center, player, _gameSettings.PitData,
                _gameSettings.SlingshotData.EnemyLayer);
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
                yield return new WaitForSeconds(_gameSettings.PitData.SpawnInterval);

                if (_activePits < _gameSettings.PitData.MaxActive)
                    TrySpawn();
            }
        }

        private void TrySpawn()
        {
            PitData data = _gameSettings.PitData;
            float scale = Random.Range(data.MinScale, data.MaxScale);
            float lifetime = Random.Range(data.MinLifetime, data.MaxLifetime);

            if (!_placementFinder.TryFind(scale, out Vector3 position))
                return;

            Pit pit = _pitFactory.Create(position, scale, lifetime, _parent);

            if (pit == null)
                return;

            pit.Despawned += OnPitDespawned;
            _activePits++;
        }

        private void OnPitDespawned(Pit pit)
        {
            pit.Despawned -= OnPitDespawned;
            _activePits = Mathf.Max(0, _activePits - 1);
        }
    }
}