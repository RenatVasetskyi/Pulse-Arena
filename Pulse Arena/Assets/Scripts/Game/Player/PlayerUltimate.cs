using System;
using Architecture.Services.Interfaces;
using Data;
using Game.Enemy;
using UnityEngine;
using Zenject;

namespace Game.Player
{
    /// <summary>
    /// The player's ultimate. When the super meter is full and the ultimate key is pressed, a shockwave
    /// flings every enemy in range violently outward (and up) — most sail off the edge and ring out. Fires
    /// <see cref="Activated"/> so the scene can add camera shake / slow-mo / sound on top.
    /// </summary>
    public class PlayerUltimate : MonoBehaviour
    {
        public event Action Activated;

        private ISuperMeterService _superMeter;
        private IInputService _input;
        private GameSettings _settings;

        [Inject]
        public void Construct(ISuperMeterService superMeter, IInputService input, GameSettings settings)
        {
            _superMeter = superMeter;
            _input = input;
            _settings = settings;
        }

        private void Update()
        {
            if (!_input.IsUltimatePressedThisFrame || !_superMeter.IsFull)
                return;

            if (_superMeter.TryConsume())
                Unleash();
        }

        private void Unleash()
        {
            SuperData data = _settings.SuperData;

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
            return hit.attachedRigidbody != null
                ? hit.attachedRigidbody.GetComponent<EnemyController>()
                : hit.GetComponent<EnemyController>();
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
