using System;
using Architecture.Services.Interfaces;
using Architecture.States;
using Architecture.States.Interfaces;
using Data;
using Game.Arena.Interfaces;
using Game.Cameras;
using Game.Combat;
using Game.Enemy;
using Game.Enemy.Interfaces;
using Game.Player;
using Game.Player.Interfaces;
using Game.Spawning;
using UI;
using UI.Hud;
using UI.Pause;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Game.Scene
{
    public class GameSceneStarter : IInitializable, IDisposable
    {
        private readonly IArenaFactory _arenaFactory;
        private readonly IPlayerFactory _playerFactory;
        private readonly IEnemyFactory _enemyFactory;
        private readonly IEnemySpawner _enemySpawner;
        private readonly IPickupSpawner _pickupSpawner;
        private readonly IInputService _inputService;
        private readonly IScoreService _scoreService;
        private readonly IAudioService _audioService;
        private readonly ISettingsService _settingsService;
        private readonly ISettingsController _settingsController;
        private readonly IStateMachine _stateMachine;
        private readonly GameSettings _gameSettings;
        private GameSceneReferences _sceneReferences;
        private IBattleCamera _battleCamera;
        private GameObject _arena;
        private PlayerController _player;
        private EnemySlingshot _enemySlingshot;
        private GameHud _gameHud;
        private GameOverView _gameOverView;
        private PausePanelView _pausePanel;
        private PauseController _pauseController;
        private bool _isGameOver;
        private int _lastPlayerHealth = -1;

        public GameSceneStarter(
            IArenaFactory arenaFactory,
            IPlayerFactory playerFactory,
            IEnemyFactory enemyFactory,
            IEnemySpawner enemySpawner,
            IPickupSpawner pickupSpawner,
            IInputService inputService,
            IScoreService scoreService,
            IAudioService audioService,
            ISettingsService settingsService,
            ISettingsController settingsController,
            IStateMachine stateMachine,
            GameSettings gameSettings)
        {
            _arenaFactory = arenaFactory;
            _playerFactory = playerFactory;
            _enemyFactory = enemyFactory;
            _enemySpawner = enemySpawner;
            _pickupSpawner = pickupSpawner;
            _inputService = inputService;
            _scoreService = scoreService;
            _audioService = audioService;
            _settingsService = settingsService;
            _settingsController = settingsController;
            _stateMachine = stateMachine;
            _gameSettings = gameSettings;
        }

        public void Initialize()
        {
            Time.timeScale = 1f;
            _isGameOver = false;
            _inputService.Enable();
            _scoreService.Reset();
            _audioService.PlayMusic(_gameSettings.AudioData.BattleMusic);

            if (!SpawnArena())
                return;

            _sceneReferences.Validate();
            PreloadPools();

            _gameOverView = InstantiateHud<GameOverView>(_gameSettings.Prefabs.GameOverPrefab, "GameOverPrefab");
            if (_gameOverView != null)
            {
                _gameOverView.RestartClicked += RestartScene;
                _gameOverView.MenuClicked += QuitToMenu;
            }

            _player = SpawnPlayer();
            _player.Died += OnPlayerDied;
            _lastPlayerHealth = _player.Health;
            _player.HealthChanged += OnPlayerHealthChanged;

            _gameHud = InstantiateHud<GameHud>(_gameSettings.Prefabs.GameHudPrefab, "GameHudPrefab");
            _gameHud?.Bind(_player, _scoreService, _battleCamera);
            _inputService.SetTouchInput(_gameHud);

            SetupPause();

            _battleCamera.Follow(_player.transform);
            SubscribeToCombat(_player);

            _enemySpawner.Initialize(_player.transform, _sceneReferences.EnemySpawnPoints,
                _sceneReferences.EnemySpawnParent, _sceneReferences.EnemySpawnHeightOffset);
            _pickupSpawner.Initialize(_sceneReferences.PickupSpawnPoints, _sceneReferences.PickupSpawnParent,
                _sceneReferences.PickupSpawnHeightOffset);
            _pickupSpawner.RarePickupSpawned += OnRarePickupSpawned;

            _enemySpawner.WaveChanged += OnWaveChanged;
            _enemySpawner.AllWavesCleared += OnAllWavesCleared;

            _enemySpawner.StartSpawn();
            _pickupSpawner.StartSpawn();
        }

        private static T InstantiateHud<T>(GameObject prefab, string prefabName) where T : Component
        {
            if (prefab == null)
            {
                Debug.LogError($"{prefabName} is not assigned in Game Settings → Prefabs.");
                return null;
            }

            return UnityEngine.Object.Instantiate(prefab).GetComponent<T>();
        }

        public void Dispose()
        {
            Time.timeScale = 1f;

            if (_player != null)
            {
                _player.Died -= OnPlayerDied;
                _player.HealthChanged -= OnPlayerHealthChanged;
            }

            if (_enemySlingshot != null)
            {
                _enemySlingshot.LassoThrown -= OnLassoThrown;
                _enemySlingshot.EnemyGrabbed -= OnEnemyGrabbed;
                _enemySlingshot.EnemyLaunched -= OnEnemyLaunched;
                _enemySlingshot.RopeBroke -= OnRopeBroke;
            }

            _enemySpawner.StopSpawn();
            _pickupSpawner.StopSpawn();
            _pickupSpawner.RarePickupSpawned -= OnRarePickupSpawned;
            _enemySpawner.WaveChanged -= OnWaveChanged;
            _enemySpawner.AllWavesCleared -= OnAllWavesCleared;

            if (_gameOverView != null)
            {
                _gameOverView.RestartClicked -= RestartScene;
                _gameOverView.MenuClicked -= QuitToMenu;
                UnityEngine.Object.Destroy(_gameOverView.gameObject);
            }

            _inputService.SetTouchInput(null);

            if (_gameHud != null)
            {
                _gameHud.PauseRequested -= OnPauseRequested;
                UnityEngine.Object.Destroy(_gameHud.gameObject);
            }

            _pauseController?.Dispose();

            if (_pausePanel != null)
                UnityEngine.Object.Destroy(_pausePanel.gameObject);

            _enemyFactory.Clear();

            if (_arena != null)
                UnityEngine.Object.Destroy(_arena);
        }

        private bool SpawnArena()
        {
            _arena = _arenaFactory.Create();
            _sceneReferences = _arena.GetComponentInChildren<GameSceneReferences>();
            _battleCamera = _arena.GetComponentInChildren<BattleCamera>();

            if (_sceneReferences != null && _battleCamera != null)
                return true;

            Debug.LogError("Arena prefab must contain GameSceneReferences and a BattleCamera.", _arena);
            return false;
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
        }

        private void SubscribeToCombat(PlayerController player)
        {
            _enemySlingshot = player.GetComponent<EnemySlingshot>();

            if (_enemySlingshot != null)
            {
                _enemySlingshot.LassoThrown += OnLassoThrown;
                _enemySlingshot.EnemyGrabbed += OnEnemyGrabbed;
                _enemySlingshot.EnemyLaunched += OnEnemyLaunched;
                _enemySlingshot.RopeBroke += OnRopeBroke;
                _gameHud?.BindTension(_enemySlingshot);
            }
        }

        private void OnLassoThrown()
        {
            _audioService.PlaySfx(GameSfx.LassoThrow);
        }

        private void OnEnemyGrabbed()
        {
            _audioService.PlaySfx(GameSfx.EnemyGrab);
        }

        private void OnEnemyLaunched(float chargeProgress)
        {
            _battleCamera.PlayLassoLaunch(chargeProgress);
            _audioService.PlaySfx(GameSfx.EnemyLaunch);
        }

        private void OnRopeBroke()
        {
            _battleCamera.Shake(_gameSettings.CameraData.RopeBreakShakeDuration,
                _gameSettings.CameraData.RopeBreakShakeStrength);
            _audioService.PlaySfx(GameSfx.RopeBreak);
        }

        private void OnPlayerHealthChanged(int health, int maxHealth)
        {
            if (_lastPlayerHealth >= 0 && health < _lastPlayerHealth)
            {
                _audioService.PlaySfx(GameSfx.PlayerHit);
                TryVibrate();
            }

            _lastPlayerHealth = health;
        }

        private void TryVibrate()
        {
            if (_settingsService == null || !_settingsService.VibrationEnabled)
                return;

#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }

        private void OnRarePickupSpawned(string message, float duration)
        {
            _gameHud?.ShowToast(message, duration);
        }

        private void OnPlayerDied()
        {
            _audioService.PlaySfx(GameSfx.Defeat);
            EndGame("GAME OVER");
        }

        private void OnAllWavesCleared()
        {
            _audioService.PlaySfx(GameSfx.Victory);
            EndGame("YOU WIN!");
        }

        private void OnWaveChanged(int current, int total)
        {
            _gameHud?.SetWave(current, total);
            _audioService.PlaySfx(GameSfx.WaveStart);
        }

        private void EndGame(string title)
        {
            if (_isGameOver)
                return;

            _isGameOver = true;
            _inputService.Disable();
            _enemySpawner.StopSpawn();
            _pickupSpawner.StopSpawn();
            _gameOverView?.Show(_scoreService.Score, title);
            Time.timeScale = 0f;
        }

        private void SetupPause()
        {
            _pausePanel = InstantiateHud<PausePanelView>(_gameSettings.Prefabs.PausePanelPrefab, "PausePanelPrefab");

            if (_pausePanel != null)
                _pauseController = new PauseController(_pausePanel, _inputService, _settingsController,
                    RestartScene, QuitToMenu);

            if (_gameHud != null)
                _gameHud.PauseRequested += OnPauseRequested;
        }

        private void OnPauseRequested()
        {
            if (_isGameOver)
                return;

            _pauseController?.Toggle();
        }

        private void QuitToMenu()
        {
            Time.timeScale = 1f;
            _stateMachine.Enter<LoadMainMenuState>();
        }

        private void RestartScene()
        {
            _audioService.PlaySfx(GameSfx.UiClick);
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.name);
        }
    }
}
