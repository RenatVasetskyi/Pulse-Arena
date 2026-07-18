using Architecture.Services.Interfaces;
using Data;
using Game.Cameras;
using Game.Combat;
using Game.Enemy;
using Game.Enemy.Interfaces;
using Game.Player;
using Game.Spawning;
using UI.Hud;
using UnityEngine;

namespace Game.Scene
{
    /// <summary>
    ///     Owns the in-game HUD: spawns it, binds it to the player / score / camera, and turns model events
    ///     (combo, super meter, waves, rare pickups) into HUD updates. One place for "service state → HUD", so
    ///     the scene orchestrator no longer carries any of that wiring. The caller still hooks the HUD's pause
    ///     button — that's game-flow, extracted in the next step.
    /// </summary>
    public class HudPresenter
    {
        private readonly IComboService _comboService;
        private readonly IEnemySpawner _enemySpawner;
        private readonly GameSettings _gameSettings;
        private readonly IInputService _inputService;
        private readonly IPickupSpawner _pickupSpawner;
        private readonly IScoreService _scoreService;
        private readonly ISuperMeterService _superMeterService;
        private readonly IWindowFactory _windowFactory;

        private GameHud _hud;

        public HudPresenter(IComboService comboService, ISuperMeterService superMeterService,
            IScoreService scoreService, IEnemySpawner enemySpawner, IPickupSpawner pickupSpawner,
            IInputService inputService, GameSettings gameSettings, IWindowFactory windowFactory)
        {
            _comboService = comboService;
            _superMeterService = superMeterService;
            _scoreService = scoreService;
            _enemySpawner = enemySpawner;
            _pickupSpawner = pickupSpawner;
            _inputService = inputService;
            _gameSettings = gameSettings;
            _windowFactory = windowFactory;
        }

        /// <summary>
        ///     Spawns + binds the HUD and wires model → HUD. Returns the HUD (caller hooks its pause
        ///     button); null if the prefab is missing.
        /// </summary>
        public GameHud Bind(PlayerController player, IBattleCamera camera)
        {
            _hud = _windowFactory.Create<GameHud>(_gameSettings.Prefabs.GameHudPrefab, "GameHudPrefab");

            if (_hud == null)
                return null;

            _hud.Bind(player, _scoreService, camera, _gameSettings.Ui.ScoreFormat);
            _hud.BindTension(player.Slingshot);
            _hud.SetSuperCharge(_superMeterService.Charge01);
            _inputService.SetTouchInput(_hud);

            _comboService.ComboChanged += OnComboChanged;
            _superMeterService.ChargeChanged += OnSuperChargeChanged;
            _enemySpawner.WaveChanged += OnWaveChanged;
            _pickupSpawner.PickupSpawned += OnPickupSpawned;

            return _hud;
        }

        public void Unbind()
        {
            _comboService.ComboChanged -= OnComboChanged;
            _superMeterService.ChargeChanged -= OnSuperChargeChanged;
            _enemySpawner.WaveChanged -= OnWaveChanged;
            _pickupSpawner.PickupSpawned -= OnPickupSpawned;
            _inputService.SetTouchInput(null);

            if (_hud != null)
                Object.Destroy(_hud.gameObject);
        }

        private void OnComboChanged(int combo)
        {
            _hud.SetCombo(combo);
        }

        private void OnSuperChargeChanged(float charge01)
        {
            _hud.SetSuperCharge(charge01);
        }

        private void OnWaveChanged(int current, int total)
        {
            _hud.SetWave(current, total);
        }

        private void OnPickupSpawned(string message, float duration)
        {
            _hud.ShowToast(message, duration);
        }
    }
}