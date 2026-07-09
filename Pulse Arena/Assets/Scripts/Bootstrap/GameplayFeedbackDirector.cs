using Architecture.Services.Interfaces;
using Data;
using Game.Cameras;
using Game.Combat;
using Game.Enemy;
using Game.Player;
using UnityEngine;

namespace Game.Scene
{
    /// <summary>
    ///     Turns gameplay events into sensory feedback — SFX, camera shake/punch, bullet-time and haptics.
    ///     One responsibility: "what does the player hear / see / feel when X happens". GameSceneStarter builds
    ///     the world and hands the event sources here via <see cref="Bind" />, so none of this wiring clutters
    ///     the scene orchestrator. It owns the audio/slow-mo/vibration services; the camera is created with the
    ///     arena at runtime, so it arrives through Bind rather than the constructor.
    /// </summary>
    public class GameplayFeedbackDirector
    {
        private readonly IAudioService _audioService;
        private readonly IEnemySpawner _enemySpawner;
        private readonly GameSettings _gameSettings;
        private readonly ISettingsService _settingsService;
        private readonly ISlowMoService _slowMoService;

        private IBattleCamera _camera;

        private int _lastPlayerHealth = -1;
        private float _lastSlowMoTime = -99f;
        private PlayerController _player;
        private PlayerUltimate _playerUltimate;
        private EnemySlingshot _slingshot;

        public GameplayFeedbackDirector(IAudioService audioService, ISlowMoService slowMoService,
            ISettingsService settingsService, IEnemySpawner enemySpawner, GameSettings gameSettings)
        {
            _audioService = audioService;
            _slowMoService = slowMoService;
            _settingsService = settingsService;
            _enemySpawner = enemySpawner;
            _gameSettings = gameSettings;
        }

        /// <summary>Subscribe to every source of player-facing feedback. Call once the world is built.</summary>
        public void Bind(PlayerController player, IBattleCamera camera)
        {
            _player = player;
            _camera = camera;
            _slingshot = player != null ? player.GetComponent<EnemySlingshot>() : null;
            _playerUltimate = player != null ? player.GetComponent<PlayerUltimate>() : null;
            _lastPlayerHealth = player != null ? player.Health : -1;

            if (_player != null)
            {
                _player.HealthChanged += OnPlayerHealthChanged;
                _player.Dashed += OnPlayerDashed;
                _player.Died += OnPlayerDied;
            }

            if (_playerUltimate != null)
                _playerUltimate.Activated += OnUltimateActivated;

            if (_slingshot != null)
            {
                _slingshot.LassoThrown += OnLassoThrown;
                _slingshot.EnemyGrabbed += OnEnemyGrabbed;
                _slingshot.EnemyLaunched += OnEnemyLaunched;
                _slingshot.RopeBroke += OnRopeBroke;
            }

            _enemySpawner.WaveChanged += OnWaveStarted;
            _enemySpawner.AllWavesCleared += OnVictory;
        }

        public void Unbind()
        {
            if (_player != null)
            {
                _player.HealthChanged -= OnPlayerHealthChanged;
                _player.Dashed -= OnPlayerDashed;
                _player.Died -= OnPlayerDied;
            }

            if (_playerUltimate != null)
                _playerUltimate.Activated -= OnUltimateActivated;

            if (_slingshot != null)
            {
                _slingshot.LassoThrown -= OnLassoThrown;
                _slingshot.EnemyGrabbed -= OnEnemyGrabbed;
                _slingshot.EnemyLaunched -= OnEnemyLaunched;
                _slingshot.RopeBroke -= OnRopeBroke;
            }

            _enemySpawner.WaveChanged -= OnWaveStarted;
            _enemySpawner.AllWavesCleared -= OnVictory;
        }

        private void OnLassoThrown() => _audioService.PlaySfx(GameSfx.LassoThrow);

        private void OnEnemyGrabbed() => _audioService.PlaySfx(GameSfx.EnemyGrab);

        private void OnEnemyLaunched(float chargeProgress)
        {
            _camera.PlayLassoLaunch(chargeProgress);
            _audioService.PlaySfx(GameSfx.EnemyLaunch);
            TryLaunchSlowMo(chargeProgress);
        }

        // Brief bullet-time on a big (high-charge) fling, rate-limited so it never spams.
        private void TryLaunchSlowMo(float chargeProgress)
        {
            SlowMoData data = _gameSettings.SlowMoData;

            if (data == null || chargeProgress < data.LaunchChargeThreshold)
                return;

            if (Time.unscaledTime - _lastSlowMoTime < data.Cooldown)
                return;

            _lastSlowMoTime = Time.unscaledTime;
            _slowMoService.Trigger(data.Scale, data.Duration);
        }

        private void OnRopeBroke()
        {
            _camera.Shake(_gameSettings.CameraData.RopeBreakShakeDuration,
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

        private void OnPlayerDashed() => _audioService.PlaySfx(GameSfx.Dash);

        private void OnUltimateActivated()
        {
            SuperData data = _gameSettings.SuperData;
            _camera.Shake(data.ShakeDuration, data.ShakeStrength);
            _slowMoService.Trigger(data.SlowMoScale, data.SlowMoDuration);
            _audioService.PlaySfx(GameSfx.Ultimate);
        }

        private void OnPlayerDied() => _audioService.PlaySfx(GameSfx.Defeat);

        private void OnWaveStarted(int current, int total) => _audioService.PlaySfx(GameSfx.WaveStart);

        private void OnVictory() => _audioService.PlaySfx(GameSfx.Victory);

        private void TryVibrate()
        {
            if (_settingsService == null || !_settingsService.VibrationEnabled)
                return;

#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }
}