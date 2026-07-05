using System;
using Architecture.Services.Interfaces;
using Game.Cameras;
using Game.Combat;
using Game.Combat.Interfaces;
using Game.Enemy;
using Game.Enemy.Interfaces;
using Game.Player;
using Game.Player.Interfaces;
using Game.Spawning;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Game.Scene
{
    public class GameSceneStarter : IInitializable, IDisposable
    {
        private readonly GameSceneReferences _sceneReferences;
        private readonly IPlayerFactory _playerFactory;
        private readonly IEnemyFactory _enemyFactory;
        private readonly IProjectileFactory _projectileFactory;
        private readonly IBattleCamera _battleCamera;
        private readonly IEnemySpawner _enemySpawner;
        private readonly IPickupSpawner _pickupSpawner;
        private readonly IInputService _inputService;
        private readonly IScoreService _scoreService;
        private PlayerController _player;
        private OrbitCutter _orbitCutter;
        private EnemySlingshot _enemySlingshot;
        private PlayerHealthView _playerHealthView;
        private CameraZoomView _cameraZoomView;
        private GameOverView _gameOverView;
        private RarePickupToastView _rarePickupToastView;
        private RopeTensionView _ropeTensionView;
        private WaveView _waveView;
        private bool _isGameOver;

        public GameSceneStarter(
            GameSceneReferences sceneReferences,
            IPlayerFactory playerFactory,
            IEnemyFactory enemyFactory,
            IProjectileFactory projectileFactory,
            IBattleCamera battleCamera,
            IEnemySpawner enemySpawner,
            IPickupSpawner pickupSpawner,
            IInputService inputService,
            IScoreService scoreService)
        {
            _sceneReferences = sceneReferences;
            _playerFactory = playerFactory;
            _enemyFactory = enemyFactory;
            _projectileFactory = projectileFactory;
            _battleCamera = battleCamera;
            _enemySpawner = enemySpawner;
            _pickupSpawner = pickupSpawner;
            _inputService = inputService;
            _scoreService = scoreService;
        }

        public void Initialize()
        {
            Time.timeScale = 1f;
            _isGameOver = false;
            _inputService.Enable();
            _scoreService.Reset();

            _sceneReferences.Validate();
            PreloadPools();
            _gameOverView = GameOverView.Create(RestartScene);

            _player = SpawnPlayer();
            _player.Died += OnPlayerDied;
            _playerHealthView = PlayerHealthView.Create(_player);
            _cameraZoomView = CameraZoomView.Create(_battleCamera);
            _rarePickupToastView = RarePickupToastView.Create();
            _battleCamera.Follow(_player.transform);
            SubscribeToCombat(_player);

            _enemySpawner.Initialize(_player.transform, _sceneReferences.EnemySpawnPoints,
                _sceneReferences.EnemySpawnParent, _sceneReferences.EnemySpawnHeightOffset);
            _pickupSpawner.Initialize(_sceneReferences.PickupSpawnPoints, _sceneReferences.PickupSpawnParent,
                _sceneReferences.PickupSpawnHeightOffset);
            _pickupSpawner.RarePickupSpawned += OnRarePickupSpawned;

            _waveView = WaveView.Create();
            _enemySpawner.WaveChanged += OnWaveChanged;
            _enemySpawner.AllWavesCleared += OnAllWavesCleared;

            _enemySpawner.StartSpawn();
            _pickupSpawner.StartSpawn();
        }

        public void Dispose()
        {
            Time.timeScale = 1f;

            if (_player != null)
                _player.Died -= OnPlayerDied;

            if (_orbitCutter != null)
                _orbitCutter.BurstUsed -= OnOrbitBurstUsed;

            if (_enemySlingshot != null)
            {
                _enemySlingshot.EnemyLaunched -= OnEnemyLaunched;
                _enemySlingshot.RopeBroke -= OnRopeBroke;
            }

            _enemySpawner.StopSpawn();
            _pickupSpawner.StopSpawn();
            _pickupSpawner.RarePickupSpawned -= OnRarePickupSpawned;
            _enemySpawner.WaveChanged -= OnWaveChanged;
            _enemySpawner.AllWavesCleared -= OnAllWavesCleared;

            if (_gameOverView != null)
                UnityEngine.Object.Destroy(_gameOverView.gameObject);

            if (_playerHealthView != null)
                UnityEngine.Object.Destroy(_playerHealthView.gameObject);

            if (_cameraZoomView != null)
                UnityEngine.Object.Destroy(_cameraZoomView.gameObject);

            if (_rarePickupToastView != null)
                UnityEngine.Object.Destroy(_rarePickupToastView.gameObject);

            if (_ropeTensionView != null)
                UnityEngine.Object.Destroy(_ropeTensionView.gameObject);

            if (_waveView != null)
                UnityEngine.Object.Destroy(_waveView.gameObject);
        }

        private PlayerController SpawnPlayer()
        {
            Transform spawnPoint = _sceneReferences.PlayerSpawnPoint;
            return _playerFactory.Create(_sceneReferences.PlayerSpawnPosition, spawnPoint.rotation,
                _sceneReferences.PlayerParent);
        }

        private void PreloadPools()
        {
            _enemyFactory.Preload();
            _projectileFactory.Preload();
        }

        private void SubscribeToCombat(PlayerController player)
        {
            _orbitCutter = player.GetComponent<OrbitCutter>();
            _enemySlingshot = player.GetComponent<EnemySlingshot>();

            if (_orbitCutter != null)
                _orbitCutter.BurstUsed += OnOrbitBurstUsed;

            if (_enemySlingshot != null)
            {
                _enemySlingshot.EnemyLaunched += OnEnemyLaunched;
                _enemySlingshot.RopeBroke += OnRopeBroke;
                _ropeTensionView = RopeTensionView.Create(_enemySlingshot);
            }
        }

        private void OnOrbitBurstUsed()
        {
            _battleCamera.Shake(0.22f, 0.45f);
        }

        private void OnEnemyLaunched(float chargeProgress)
        {
            _battleCamera.PlayLassoLaunch(chargeProgress);
        }

        private void OnRopeBroke()
        {
            _battleCamera.Shake(0.12f, 0.32f);
        }

        private void OnRarePickupSpawned(string message, float duration)
        {
            _rarePickupToastView?.Show(message, duration);
        }

        private void OnPlayerDied()
        {
            if (_isGameOver)
                return;

            _isGameOver = true;
            _inputService.Disable();
            _enemySpawner.StopSpawn();
            _pickupSpawner.StopSpawn();

            if (_gameOverView != null)
                _gameOverView.Show(_scoreService.Score);

            Time.timeScale = 0f;
        }

        private void OnWaveChanged(int current, int total)
        {
            _waveView?.SetWave(current, total);
        }

        private void OnAllWavesCleared()
        {
            if (_isGameOver)
                return;

            _isGameOver = true;
            _inputService.Disable();
            _enemySpawner.StopSpawn();
            _pickupSpawner.StopSpawn();

            if (_gameOverView != null)
                _gameOverView.Show(_scoreService.Score, "YOU WIN!");

            Time.timeScale = 0f;
        }

        private void RestartScene()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.name);
        }
    }
}
