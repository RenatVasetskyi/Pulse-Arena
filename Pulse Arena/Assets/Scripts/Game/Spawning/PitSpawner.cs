using System.Collections;
using Architecture.Services.Interfaces;
using Data;
using Game.Arena;
using Game.Arena.Interfaces;
using UnityEngine;

namespace Game.Spawning
{
    /// <summary>
    /// Spawns transient <see cref="Pit"/> hazards at random spots inside the play ring, at random size and
    /// lifetime, up to a cap. Each pit frees its slot when it despawns (eaten or timed out).
    /// </summary>
    public class PitSpawner : IPitSpawner
    {
        private const int MaxPlacementTries = 8;

        // The pit prefab's base trigger radius (Assets/Prefabs/Game/pit.prefab CapsuleCollider). Scaled by
        // the spawn scale to keep a fresh pit clear of the player and any enemy.
        private const float PitTriggerRadius = 2.5f;

        private readonly ICoroutineRunner _coroutineRunner;
        private readonly IPitFactory _pitFactory;
        private readonly GameSettings _gameSettings;

        private Coroutine _spawnRoutine;
        private Vector3 _center;
        private Transform _player;
        private Transform _parent;
        private int _activePits;

        public PitSpawner(ICoroutineRunner coroutineRunner, IPitFactory pitFactory, GameSettings gameSettings)
        {
            _coroutineRunner = coroutineRunner;
            _pitFactory = pitFactory;
            _gameSettings = gameSettings;
        }

        public void Initialize(Vector3 center, Transform player, Transform parent)
        {
            _center = center;
            _player = player;
            _parent = parent;
            _activePits = 0;
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

            if (!TryFindSpawnPoint(data, scale, out Vector3 position))
                return;

            Pit pit = _pitFactory.Create(position, scale, lifetime, _parent);

            if (pit == null)
                return;

            pit.Despawned += OnPitDespawned;
            _activePits++;
        }

        // A few tries to land a spot inside the ring that's clear of the player AND every enemy — a pit
        // must never open right on top of someone (it would swallow an enemy for free or trap the player).
        private bool TryFindSpawnPoint(PitData data, float scale, out Vector3 position)
        {
            float clearance = PitTriggerRadius * scale + data.SpawnClearance;
            float playerClearance = Mathf.Max(data.MinPlayerDistance, clearance);

            for (int i = 0; i < MaxPlacementTries; i++)
            {
                float angle = Random.value * Mathf.PI * 2f;
                float radius = Random.Range(data.MinRadius, data.MaxRadius);
                Vector3 candidate = _center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

                if (_player != null && HorizontalDistance(candidate, _player.position) < playerClearance)
                    continue;

                if (Physics.CheckSphere(candidate + Vector3.up, clearance, _gameSettings.SlingshotData.EnemyLayer))
                    continue;

                position = candidate;
                return true;
            }

            position = default;
            return false;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private void OnPitDespawned(Pit pit)
        {
            pit.Despawned -= OnPitDespawned;
            _activePits = Mathf.Max(0, _activePits - 1);
        }
    }
}
