using System.Collections;
using Architecture.Services.Interfaces;
using Architecture.States;
using Architecture.States.Interfaces;
using Data;
using Game.Cameras;
using Game.Enemy;
using Game.Player;
using Game.Spawning;
using Game.Turrets;
using UI;
using UI.Hud;
using UI.Pause;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Scene
{
    /// <summary>
    ///     Owns the game's flow: win / lose (EndGame), the game-over screen, pause, restart and quit-to-menu.
    ///     GameWorldBuilder builds the world then hands the player + HUD here via <see cref="Bind" />; everything
    ///     about "how a run ends and what the buttons do" lives in this one class. Restart reloads the game scene
    ///     (Unity defers the load to end of frame, so it is safe to call from a button callback); quit hands back to
    ///     the state machine. Either way the scene unload lets the SceneContext tear the world down automatically.
    /// </summary>
    public class GameFlowController
    {
        private const float DeathScreenDelay = 2f; // let the death animation + camera zoom play before the screen

        private readonly IAudioService _audioService;
        private readonly ICoroutineRunner _coroutineRunner;
        private readonly IEnemySpawner _enemySpawner;
        private readonly GameSettings _gameSettings;
        private readonly IInputService _inputService;
        private readonly ILevelProgressService _levelProgress;
        private readonly ILevelService _levelService;
        private readonly IPauseService _pauseService;
        private readonly IPickupSpawner _pickupSpawner;
        private readonly IPitSpawner _pitSpawner;
        private readonly IScoreService _scoreService;
        private readonly ISettingsController _settingsController;
        private readonly ISlowMoService _slowMoService;
        private readonly IStateMachine _stateMachine;
        private readonly ITurretSpawner _turretSpawner;
        private IBattleCamera _battleCamera;
        private Coroutine _gameOverRoutine;
        private GameHud _gameHud;
        private GameOverView _gameOverView;
        private bool _isGameOver;
        private PauseController _pauseController;
        private PausePanelView _pausePanel;
        private PlayerController _player;

        public GameFlowController(IInputService inputService, IScoreService scoreService,
            ISlowMoService slowMoService, IStateMachine stateMachine, ISettingsController settingsController,
            IAudioService audioService, IEnemySpawner enemySpawner, IPickupSpawner pickupSpawner,
            IPitSpawner pitSpawner, ITurretSpawner turretSpawner, IPauseService pauseService,
            ICoroutineRunner coroutineRunner, GameSettings gameSettings, ILevelService levelService,
            ILevelProgressService levelProgress)
        {
            _inputService = inputService;
            _levelService = levelService;
            _levelProgress = levelProgress;
            _scoreService = scoreService;
            _slowMoService = slowMoService;
            _stateMachine = stateMachine;
            _settingsController = settingsController;
            _audioService = audioService;
            _enemySpawner = enemySpawner;
            _pickupSpawner = pickupSpawner;
            _pitSpawner = pitSpawner;
            _turretSpawner = turretSpawner;
            _pauseService = pauseService;
            _coroutineRunner = coroutineRunner;
            _gameSettings = gameSettings;
        }

        /// <summary>Builds the game-over screen + pause and starts listening for win/lose. Call once the world is built.</summary>
        public void Bind(PlayerController player, GameHud gameHud, IBattleCamera battleCamera)
        {
            _player = player;
            _gameHud = gameHud;
            _battleCamera = battleCamera;
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
            // The delay coroutine lives on the persistent (ProjectContext) runner, so an unfinished one would
            // outlive this match and call ShowGameOver on a destroyed view — stop it on teardown.
            if (_gameOverRoutine != null)
            {
                _coroutineRunner.StopCoroutine(_gameOverRoutine);
                _gameOverRoutine = null;
            }

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

        // Player death: stop the run, punch a dramatic camera zoom, and let the death animation play for a beat
        // before the screen drops (so it isn't hidden by the instant freeze).
        private void OnPlayerDied()
        {
            if (_isGameOver)
                return;

            _isGameOver = true;
            RecordSurvivalScore();
            StopRun();
            _battleCamera?.PlayDeathZoom();
            _gameOverRoutine = _coroutineRunner.StartCoroutine(ShowGameOverAfterDelay("GAME OVER", DeathScreenDelay));
        }

        // Survival is endless — the run only ends on death, so bank the final score as the new best here.
        private void RecordSurvivalScore()
        {
            if (_levelService.Selected == null || !_levelService.Selected.IsEndless)
                return;

            _levelProgress.SubmitSurvivalScore(_scoreService.Score);
        }

        // Campaign win: record the clear (unlocks the next level + banks stars). Survival is endless, so this never
        // fires for it — only real campaign levels ever unlock the next one.
        private void OnAllWavesCleared()
        {
            if (_isGameOver)
                return;

            _isGameOver = true;
            _levelProgress.Complete(_levelService.SelectedIndex, ComputeStars());
            StopRun();
            ShowGameOver("YOU WIN!");
        }

        // Stars reward keeping your health: 3 = untouched, 2 = at least half, 1 = survived by a sliver.
        private int ComputeStars()
        {
            if (_player == null || _player.MaxHealth <= 0)
                return 1;

            float ratio = _player.Health / (float)_player.MaxHealth;

            if (ratio >= 0.999f)
                return 3;

            return ratio >= 0.5f ? 2 : 1;
        }

        // Stops the live run but does NOT freeze — so the death/win beat can still animate before the screen appears.
        private void StopRun()
        {
            _pauseService.Unpause(); // if somehow ended while paused, restore state before the game-over freeze
            _slowMoService.Stop();
            _inputService.Disable();
            _enemySpawner.StopSpawn();
            _pickupSpawner.StopSpawn();
            _pitSpawner.StopSpawn();
            _turretSpawner.StopSpawn(); // otherwise turrets keep firing at the corpse during the death-screen delay
        }

        // Let the death animation + camera zoom play (real-time, not frozen), THEN show the screen and freeze.
        private IEnumerator ShowGameOverAfterDelay(string title, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            ShowGameOver(title);
        }

        private void ShowGameOver(string title)
        {
            _gameOverView?.Show(_scoreService.Score, title);
            Time.timeScale = 0f;
        }

        private void SetupPause()
        {
            _pausePanel = InstantiateHud<PausePanelView>(_gameSettings.Prefabs.PausePanelPrefab, "PausePanelPrefab");

            if (_pausePanel != null)
                _pauseController = new PauseController(_pausePanel, _inputService, _settingsController,
                    _slowMoService, _pauseService, RestartScene, QuitToMenu);

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
            _pauseService.Unpause(); // never carry a paused state across a scene change (next scene must be live)
            _slowMoService.Stop(); // kill any in-progress bullet-time on the persistent runner before the scene swap
            Time.timeScale = 1f;
            _stateMachine.Enter<LoadMainMenuState>();
        }

        private void RestartScene()
        {
            _audioService.PlaySfx(GameSfx.UiClick);
            _pauseService.Unpause();
            _slowMoService.Stop();
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