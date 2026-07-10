using System.Collections;
using Architecture.Services.Interfaces;
using Data;
using Game.Spawning;
using UnityEngine;

namespace Game.Turrets
{
    /// <summary>
    ///     Spawns stationary <see cref="Turret" />s inside the play ring at a fixed cadence, up to a cap. Reuses the
    ///     shared <see cref="SafeSpawnFinder" /> so a turret never lands on a wall, pit, another spawn, or too close
    ///     to the player. Each turret self-destructs after its lifetime and raises <see cref="Turret.Despawned" />,
    ///     which frees a slot so a fresh one can spawn — keeping the live count at the cap over the match.
    ///     Mechanical-pause aware — the spawn timer holds while paused.
    /// </summary>
    public class TurretSpawner : ITurretSpawner, IPausable
    {
        private readonly ICoroutineRunner _coroutineRunner;
        private readonly GameSettings _gameSettings;
        private readonly IPauseService _pauseService;
        private readonly ISafeSpawnFinder _placementFinder = new SafeSpawnFinder();
        private readonly ITurretFactory _turretFactory;
        private int _activeTurrets;
        private Transform _parent;
        private bool _paused;
        private Transform _player;

        private Coroutine _spawnRoutine;

        public TurretSpawner(ICoroutineRunner coroutineRunner, ITurretFactory turretFactory, GameSettings gameSettings,
            IPauseService pauseService)
        {
            _coroutineRunner = coroutineRunner;
            _turretFactory = turretFactory;
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

        public void Initialize(Vector3 center, Transform player, Transform parent)
        {
            _player = player;
            _parent = parent;
            _activeTurrets = 0;
            _placementFinder.Initialize(center, player, _gameSettings.SpawnAreaData, BlockerMask());
        }

        // Walls/boxes + the Default-layer pit & pickup triggers (ObstacleLayer) plus live enemies (EnemyLayer),
        // so one clearance test keeps a fresh turret off all of them.
        private LayerMask BlockerMask()
        {
            return _gameSettings.SlingshotData.ObstacleLayer.value | _gameSettings.SlingshotData.EnemyLayer.value;
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
                yield return PausableWait(_gameSettings.TurretData.SpawnInterval);

                if (_activeTurrets < _gameSettings.TurretData.MaxActive)
                    Spawn();
            }
        }

        private void Spawn()
        {
            if (!_placementFinder.TryFind(out Vector3 position))
                return;

            Turret turret = _turretFactory.CreateTurret(position, _parent, _player);

            if (turret == null)
                return;

            turret.Despawned += OnTurretDespawned;
            _activeTurrets++;
        }

        // A turret self-destructed (lifetime ended) — free its slot so the loop can spawn a replacement.
        private void OnTurretDespawned(Turret turret)
        {
            turret.Despawned -= OnTurretDespawned;
            _activeTurrets = Mathf.Max(0, _activeTurrets - 1);
        }
    }
}
