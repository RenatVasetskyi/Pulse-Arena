using System;
using Architecture.Services.Interfaces;
using Data;
using Game.Enemy;
using Game.Player.Interfaces;
using UnityEngine;
using Zenject;

namespace Game.Player
{
    /// <summary>
    ///     The player's ultimate: when the super meter is full and the key is pressed, a shockwave flings every
    ///     enemy in range outward + up (most ring out), emits a ground shockwave VFX, and fires
    ///     <see cref="Activated" /> so the scene can layer camera shake / slow-mo / sound on top.
    /// </summary>
    public class PlayerUltimate : MonoBehaviour, IPausable
    {
        private readonly IShockwaveEffect _shockwave = new ShockwaveEffect();
        private IInputService _input;
        private bool _paused;
        private IPauseService _pauseService;
        private GameSettings _settings;

        private ISuperMeterService _superMeter;
        public event Action Activated;

        [Inject]
        public void Construct(ISuperMeterService superMeter, IInputService input, GameSettings settings,
            IPauseService pauseService)
        {
            _superMeter = superMeter;
            _input = input;
            _settings = settings;
            _pauseService = pauseService;
            _shockwave.Initialize(transform, settings.Prefabs.ShockwavePrefab);
            _pauseService.Register(this);
        }

        /// <summary>Mechanical pause: stop polling the ultimate input so it can't fire (and consume the meter /
        /// launch frozen enemies) while the game is mechanically paused.</summary>
        public void Pause()
        {
            _paused = true;
        }

        public void Resume()
        {
            _paused = false;
        }

        private void Update()
        {
            if (_paused)
                return;

            if (!_input.IsUltimatePressedThisFrame || !_superMeter.IsFull)
                return;

            if (_superMeter.TryConsume())
                Unleash();
        }

        private void OnDestroy()
        {
            _pauseService?.Unregister(this);
        }

        private void Unleash()
        {
            SuperData data = _settings.SuperData;
            _shockwave.Play(transform.position);

            foreach (Collider hit in FindEnemiesInRadius(data.Radius))
            {
                EnemyController enemy = ResolveEnemy(hit);

                if (enemy != null)
                    LaunchEnemy(enemy, data);
            }

            Activated?.Invoke();
        }

        private Collider[] FindEnemiesInRadius(float radius)
        {
            return Physics.OverlapSphere(transform.position, radius, _settings.SlingshotData.EnemyLayer);
        }

        private static EnemyController ResolveEnemy(Collider hit)
        {
            if (hit.attachedRigidbody != null)
                return hit.attachedRigidbody.TryGetComponent(out EnemyController enemy) ? enemy : null;

            return hit.TryGetComponent(out EnemyController fallback) ? fallback : null;
        }

        private void LaunchEnemy(EnemyController enemy, SuperData data)
        {
            Vector3 direction = enemy.transform.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.01f)
                direction = transform.forward;

            direction.Normalize();
            Vector3 velocity = direction * data.LaunchSpeed + Vector3.up * (data.LaunchSpeed * data.UpwardRatio);
            enemy.Launch(velocity, data.LaunchDuration);
        }
    }
}