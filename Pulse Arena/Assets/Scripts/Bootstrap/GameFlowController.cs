using Architecture.Services.Interfaces;
using Architecture.States;
using Architecture.States.Interfaces;
using Data;
using Game.Enemy;
using Game.Player;
using Game.Spawning;
using UI;
using UI.Hud;
using UI.Pause;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Scene
{
    /// <summary>
    /// Owns the game's flow: win / lose (EndGame), the game-over screen, pause, restart and quit-to-menu.
    /// GameSceneStarter builds the world then hands the player + HUD here via <see cref="Bind"/>; everything
    /// about "how a run ends and what the buttons do" lives in this one class.
    /// </summary>
    public class GameFlowController
    {
        private readonly IInputService _inputService;
        private readonly IScoreService _scoreService;
        private readonly ISlowMoService _slowMoService;
        private readonly IStateMachine _stateMachine;
        private readonly ISettingsController _settingsController;
        private readonly IAudioService _audioService;
        private readonly IEnemySpawner _enemySpawner;
        private readonly IPickupSpawner _pickupSpawner;
        private readonly IPitSpawner _pitSpawner;
        private readonly GameSettings _gameSettings;

        private PlayerController _player;
        private GameHud _gameHud;
        private GameOverView _gameOverView;
        private PausePanelView _pausePanel;
        private PauseController _pauseController;
        private bool _isGameOver;

        public GameFlowController(IInputService inputService, IScoreService scoreService,
            ISlowMoService slowMoService, IStateMachine stateMachine, ISettingsController settingsController,
            IAudioService audioService, IEnemySpawner enemySpawner, IPickupSpawner pickupSpawner,
            IPitSpawner pitSpawner, GameSettings gameSettings)
        {
            _inputService = inputService;
            _scoreService = scoreService;
            _slowMoService = slowMoService;
            _stateMachine = stateMachine;
            _settingsController = settingsController;
            _audioService = audioService;
            _enemySpawner = enemySpawner;
            _pickupSpawner = pickupSpawner;
            _pitSpawner = pitSpawner;
            _gameSettings = gameSettings;
        }

        /// <summary>Builds the game-over screen + pause and starts listening for win/lose. Call once the world is built.</summary>
        public void Bind(PlayerController player, GameHud gameHud)
        {
            _player = player;
            _gameHud = gameHud;
            _isGameOver = false;

            _gameOverView = InstantiateHud<GameOverView>(_gameSettings.Prefabs.GameOverPrefab, "GameOverPrefab");
            if (_gameOverView != null)
            {
                _gameOverView.RestartClicked += RestartScene;
                _gameOverView.MenuClicked += QuitToMenu;
            }

            SetupPause();

            if (_player != null)
                _player.Died += OnPlayerDied;

            _enemySpawner.AllWavesCleared += OnAllWavesCleared;
        }

        public void Unbind()
        {
            if (_player != null)
                _player.Died -= OnPlayerDied;

            _enemySpawner.AllWavesCleared -= OnAllWavesCleared;

            if (_gameOverView != null)
            {
                _gameOverView.RestartClicked -= RestartScene;
                _gameOverView.MenuClicked -= QuitToMenu;
                Object.Destroy(_gameOverView.gameObject);
            }

            if (_gameHud != null)
                _gameHud.PauseRequested -= OnPauseRequested;

            _pauseController?.Dispose();

            if (_pausePanel != null)
                Object.Destroy(_pausePanel.gameObject);
        }

        private void OnPlayerDied() => EndGame("GAME OVER");

        private void OnAllWavesCleared() => EndGame("YOU WIN!");

        private void EndGame(string title)
        {
            if (_isGameOver)
                return;

            _isGameOver = true;
            _slowMoService.Stop();
            _inputService.Disable();
            _enemySpawner.StopSpawn();
            _pickupSpawner.StopSpawn();
            _pitSpawner.StopSpawn();
            _gameOverView?.Show(_scoreService.Score, title);
            Time.timeScale = 0f;
        }

        private void SetupPause()
        {
            _pausePanel = InstantiateHud<PausePanelView>(_gameSettings.Prefabs.PausePanelPrefab, "PausePanelPrefab");

            if (_pausePanel != null)
                _pauseController = new PauseController(_pausePanel, _inputService, _settingsController,
                    _slowMoService, RestartScene, QuitToMenu);

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
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private static T InstantiateHud<T>(GameObject prefab, string prefabName) where T : Component
        {
            if (prefab == null)
            {
                Debug.LogError($"{prefabName} is not assigned in Game Settings → Prefabs.");
                return null;
            }

            return Object.Instantiate(prefab).GetComponent<T>();
        }
    }
}
