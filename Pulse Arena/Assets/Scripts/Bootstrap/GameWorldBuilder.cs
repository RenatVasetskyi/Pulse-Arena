using Architecture.Services.Interfaces;
using Data;
using Game.Arena.Interfaces;
using Game.Cameras;
using Game.Enemy;
using Game.Enemy.Interfaces;
using Game.Player;
using Game.Player.Interfaces;
using Game.Spawning;
using UI.Hud;
using UnityEngine;
using Zenject;

namespace Game.Scene
{
    /// <summary>
    /// Builds and tears down the whole game world. It lives in the game scene's SceneContext and runs off that
    /// context's kernel: <see cref="Initialize"/> (→ Build) fires when the scene loads, <see cref="Dispose"/>
    /// (→ Teardown) fires automatically when the scene unloads — so no state has to manage the match lifecycle,
    /// and cleanup can never be forgotten. It coordinates: creates the arena + player via factories, then hands
    /// the world to focused collaborators (<see cref="HudPresenter"/>, <see cref="GameplayFeedbackDirector"/>,
    /// <see cref="GameFlowController"/>) and starts spawning.
    /// </summary>
    public class GameWorldBuilder : IInitializable, System.IDisposable
    {
        private readonly IArenaFactory _arenaFactory;
        private readonly IPlayerFactory _playerFactory;
        private readonly IEnemyFactory _enemyFactory;
        private readonly IEnemySpawner _enemySpawner;
        private readonly IPickupSpawner _pickupSpawner;
        private readonly IPitSpawner _pitSpawner;
        private readonly IInputService _inputService;
        private readonly IScoreService _scoreService;
        private readonly IComboService _comboService;
        private readonly ISuperMeterService _superMeterService;
        private readonly IAudioService _audioService;
        private readonly HudPresenter _hudPresenter;
        private readonly GameplayFeedbackDirector _feedback;
        private readonly GameFlowController _gameFlow;
        private readonly GameSettings _gameSettings;
        private GameSceneReferences _sceneReferences;
        private IBattleCamera _battleCamera;
        private GameObject _arena;
        private PlayerController _player;

        public GameWorldBuilder(
            IArenaFactory arenaFactory,
            IPlayerFactory playerFactory,
            IEnemyFactory enemyFactory,
            IEnemySpawner enemySpawner,
            IPickupSpawner pickupSpawner,
            IPitSpawner pitSpawner,
            IInputService inputService,
            IScoreService scoreService,
            IComboService comboService,
            ISuperMeterService superMeterService,
            IAudioService audioService,
            HudPresenter hudPresenter,
            GameplayFeedbackDirector feedback,
            GameFlowController gameFlow,
            GameSettings gameSettings)
        {
            _arenaFactory = arenaFactory;
            _playerFactory = playerFactory;
            _enemyFactory = enemyFactory;
            _enemySpawner = enemySpawner;
            _pickupSpawner = pickupSpawner;
            _pitSpawner = pitSpawner;
            _inputService = inputService;
            _scoreService = scoreService;
            _comboService = comboService;
            _superMeterService = superMeterService;
            _audioService = audioService;
            _hudPresenter = hudPresenter;
            _feedback = feedback;
            _gameFlow = gameFlow;
            _gameSettings = gameSettings;
        }

        public void Build()
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
            _gameFlow.Bind(_player, gameHud);

            StartSpawners();
        }

        public void Teardown()
        {
            Time.timeScale = 1f;

            _hudPresenter.Unbind();
            _feedback.Unbind();
            _gameFlow.Unbind();

            _enemySpawner.StopSpawn();
            _pickupSpawner.StopSpawn();
            _pitSpawner.StopSpawn();

            _enemyFactory.Clear();

            if (_arena != null)
                Object.Destroy(_arena);

            _arena = null;
            _player = null;
        }
        
        public void Initialize()
        {
            Build();
        }

        public void Dispose()
        {
            Teardown();
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
            _enemySpawner.Initialize(_player.transform, _sceneReferences.EnemySpawnPoints,
                _sceneReferences.EnemySpawnParent, _sceneReferences.EnemySpawnHeightOffset);
            _pickupSpawner.Initialize(_sceneReferences.PickupSpawnPoints, _sceneReferences.PickupSpawnParent,
                _sceneReferences.PickupSpawnHeightOffset);
            _pitSpawner.Initialize(_arena.transform.position + Vector3.up * _gameSettings.PitData.SpawnHeight,
                _player.transform, _arena.transform);

            _enemySpawner.StartSpawn();
            _pickupSpawner.StartSpawn();
            _pitSpawner.StartSpawn();
        }
    }
}
