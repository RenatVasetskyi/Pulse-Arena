using Architecture.Services.Interfaces;
using Data;
using Game.Arena.Interfaces;
using Game.Cameras;
using Game.Enemy;
using Game.Enemy.Interfaces;
using Game.Player;
using Game.Player.Interfaces;
using Game.Spawning;
using Game.Turrets;
using UI.Hud;
using UnityEngine;
using Zenject;

namespace Game.Scene
{
    /// <summary>
    ///     THE composition root of the game scene — read <see cref="Build" /> top-to-bottom to see the ENTIRE match
    ///     setup in one place (this is the game-scene equivalent of <c>LoadMainMenuState.CreateMenu</c>). It lives in
    ///     the game scene's SceneContext and runs off that context's kernel: <see cref="Initialize" /> (→ Build) fires
    ///     when the scene loads, <see cref="Dispose" /> (→ Teardown) fires automatically when the scene unloads — so
    ///     no FSM state has to manage the match lifecycle, and cleanup can never be forgotten. It coordinates: creates
    ///     the arena + player via factories, then hands the world to focused collaborators
    ///     (<see cref="HudPresenter" />, <see cref="GameplayFeedbackDirector" />, <see cref="GameFlowController" />)
    ///     and starts spawning. Build/Teardown are private — the only entry points are the two interface hooks.
    /// </summary>
    public class GameWorldBuilder : IInitializable, System.IDisposable
    {
        private readonly IArenaFactory _arenaFactory;
        private readonly IAudioService _audioService;
        private readonly IComboService _comboService;
        private readonly IEnemyFactory _enemyFactory;
        private readonly IEnemySpawner _enemySpawner;
        private readonly GameplayFeedbackDirector _feedback;
        private readonly GameFlowController _gameFlow;
        private readonly GameSettings _gameSettings;
        private readonly HudPresenter _hudPresenter;
        private readonly IInputService _inputService;
        private readonly ILevelService _levelService;
        private readonly OnboardingController _onboarding;
        private readonly IPickupSpawner _pickupSpawner;
        private readonly IPitSpawner _pitSpawner;
        private readonly IPlayerFactory _playerFactory;
        private readonly IScoreService _scoreService;
        private readonly ISuperMeterService _superMeterService;
        private readonly ITurretSpawner _turretSpawner;
        private GameObject _arena;
        private IBattleCamera _battleCamera;
        private PlayerController _player;
        private GameSceneReferences _sceneReferences;

        public GameWorldBuilder(
            IArenaFactory arenaFactory,
            IPlayerFactory playerFactory,
            IEnemyFactory enemyFactory,
            IEnemySpawner enemySpawner,
            IPickupSpawner pickupSpawner,
            IPitSpawner pitSpawner,
            ITurretSpawner turretSpawner,
            IInputService inputService,
            ILevelService levelService,
            IScoreService scoreService,
            IComboService comboService,
            ISuperMeterService superMeterService,
            IAudioService audioService,
            HudPresenter hudPresenter,
            GameplayFeedbackDirector feedback,
            GameFlowController gameFlow,
            OnboardingController onboarding,
            GameSettings gameSettings)
        {
            _arenaFactory = arenaFactory;
            _playerFactory = playerFactory;
            _enemyFactory = enemyFactory;
            _enemySpawner = enemySpawner;
            _pickupSpawner = pickupSpawner;
            _pitSpawner = pitSpawner;
            _turretSpawner = turretSpawner;
            _inputService = inputService;
            _levelService = levelService;
            _scoreService = scoreService;
            _comboService = comboService;
            _superMeterService = superMeterService;
            _audioService = audioService;
            _hudPresenter = hudPresenter;
            _feedback = feedback;
            _gameFlow = gameFlow;
            _onboarding = onboarding;
            _gameSettings = gameSettings;
        }

        public void Initialize()
        {
            Build();
        }

        private void Build()
        {
            ResetSession();

            if (!SpawnArena())
                return;

            _sceneReferences.Validate();
            PreloadPools();

            _player = SpawnPlayer();
            _battleCamera.Follow(_player.transform);

            GameHud gameHud = _hudPresenter.Bind(_player, _battleCamera);
            _feedback.Bind(_player, _battleCamera);
            _gameFlow.Bind(_player, gameHud, _battleCamera);
            _onboarding.Bind(_player, gameHud);

            StartSpawners();
        }

        public void Dispose()
        {
            Teardown();
        }

        private void Teardown()
        {
            Time.timeScale = 1f;

            _hudPresenter.Unbind();
            _feedback.Unbind();
            _gameFlow.Unbind();
            _onboarding.Unbind();

            _enemySpawner.StopSpawn();
            _pickupSpawner.StopSpawn();
            _pitSpawner.StopSpawn();
            _turretSpawner.StopSpawn();

            _enemyFactory.Clear();

            if (_arena != null)
                Object.Destroy(_arena);

            _arena = null;
            _player = null;
        }

        private void ResetSession()
        {
            Time.timeScale = 1f;
            _inputService.Enable();
            _scoreService.Reset();
            _comboService.Reset();
            _superMeterService.Reset();
            _audioService.PlayMusic(_gameSettings.AudioData.BattleMusic);
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

        private void StartSpawners()
        {
            Vector3 center = _arena.transform.position;
            LevelDefinition level = _levelService.Selected;

            _enemySpawner.Initialize(_player.transform, center, _sceneReferences.EnemySpawnParent,
                _sceneReferences.EnemySpawnHeightOffset);
            _pickupSpawner.Initialize(center, _player.transform, _sceneReferences.PickupSpawnParent,
                _sceneReferences.PickupSpawnHeightOffset);

            _enemySpawner.StartSpawn();
            _pickupSpawner.StartSpawn();

            // Hazards are per-level: the first levels stay clean, later ones layer in pits then turrets.
            if (level == null || level.EnablePits)
            {
                _pitSpawner.Initialize(_arena.transform.position + Vector3.up * _gameSettings.PitData.SpawnHeight,
                    _player.transform, _arena.transform);
                _pitSpawner.StartSpawn();
            }

            if (level == null || level.EnableTurrets)
            {
                _turretSpawner.Initialize(center, _player.transform, _arena.transform);
                _turretSpawner.StartSpawn();
            }
        }
    }
}