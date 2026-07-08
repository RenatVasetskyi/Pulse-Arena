using Architecture.Services.Interfaces;
using Data;
using Game.Cameras;
using Game.Combat;
using Game.Enemy;
using Game.Player;
using Game.Spawning;
using UI.Hud;
using UnityEngine;

namespace Game.Scene
{
    /// <summary>
    /// Owns the in-game HUD: spawns it, binds it to the player / score / camera, and turns model events
    /// (combo, super meter, waves, rare pickups) into HUD updates. One place for "service state → HUD", so
    /// the scene orchestrator no longer carries any of that wiring. The caller still hooks the HUD's pause
    /// button — that's game-flow, extracted in the next step.
    /// </summary>
    public class HudPresenter
    {
        private readonly IComboService _comboService;
        private readonly ISuperMeterService _superMeterService;
        private readonly IScoreService _scoreService;
        private readonly IEnemySpawner _enemySpawner;
        private readonly IPickupSpawner _pickupSpawner;
        private readonly IInputService _inputService;
        private readonly GameSettings _gameSettings;

        private GameHud _hud;

        public HudPresenter(IComboService comboService, ISuperMeterService superMeterService,
            IScoreService scoreService, IEnemySpawner enemySpawner, IPickupSpawner pickupSpawner,
            IInputService inputService, GameSettings gameSettings)
        {
            _comboService = comboService;
            _superMeterService = superMeterService;
            _scoreService = scoreService;
            _enemySpawner = enemySpawner;
            _pickupSpawner = pickupSpawner;
            _inputService = inputService;
            _gameSettings = gameSettings;
        }

        /// <summary>Spawns + binds the HUD and wires model → HUD. Returns the HUD (caller hooks its pause
        /// button); null if the prefab is missing.</summary>
        public GameHud Bind(PlayerController player, IBattleCamera camera)
        {
            GameObject prefab = _gameSettings.Prefabs.GameHudPrefab;

            if (prefab == null)
            {
                Debug.LogError("GameHudPrefab is not assigned in Game Settings → Prefabs.");
                return null;
            }

            _hud = Object.Instantiate(prefab).GetComponent<GameHud>();
            _hud.Bind(player, _scoreService, camera);
            _hud.BindTension(player.GetComponent<EnemySlingshot>());
            _hud.SetSuperCharge(_superMeterService.Charge01);
            _inputService.SetTouchInput(_hud);

            _comboService.ComboChanged += OnComboChanged;
            _superMeterService.ChargeChanged += OnSuperChargeChanged;
            _superMeterService.Ready += OnSuperReady;
            _enemySpawner.WaveChanged += OnWaveChanged;
            _pickupSpawner.RarePickupSpawned += OnRarePickupSpawned;

            return _hud;
        }

        public void Unbind()
        {
            _comboService.ComboChanged -= OnComboChanged;
            _superMeterService.ChargeChanged -= OnSuperChargeChanged;
            _superMeterService.Ready -= OnSuperReady;
            _enemySpawner.WaveChanged -= OnWaveChanged;
            _pickupSpawner.RarePickupSpawned -= OnRarePickupSpawned;
            _inputService.SetTouchInput(null);

            if (_hud != null)
                Object.Destroy(_hud.gameObject);
        }

        private void OnComboChanged(int combo) => _hud.SetCombo(combo);

        private void OnSuperChargeChanged(float charge01)
        {
            _hud.SetSuperCharge(charge01);

            if (charge01 < 1f)
                _hud.SetSuperReady(false);
        }

        private void OnSuperReady() => _hud.SetSuperReady(true);

        private void OnWaveChanged(int current, int total) => _hud.SetWave(current, total);

        private void OnRarePickupSpawned(string message, float duration) => _hud.ShowToast(message, duration);
    }
}
