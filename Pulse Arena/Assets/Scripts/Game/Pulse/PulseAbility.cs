using System;
using Architecture.Services.Interfaces;
using Data;
using Game.Enemy;
using UnityEngine;
using Zenject;

namespace Game.Pulse
{
    public class PulseAbility : MonoBehaviour
    {
        public event Action<float> ChargeChanged;

        private IInputService _inputService;
        private PulseData _data;
        private float _charge;
        private float _cooldownTimer;

        [Inject]
        public void Construct(IInputService inputService, GameSettings gameSettings)
        {
            _inputService = inputService;
            _data = gameSettings.PulseData;
        }

        private void Update()
        {
            TickCooldown();

            if (_inputService.IsPulsePressedThisFrame)
                TryUse();
        }

        public void AddEnergy(float value)
        {
            _charge = Mathf.Clamp(_charge + value, 0f, _data.MaxCharge);
            ChargeChanged?.Invoke(_charge / _data.MaxCharge);
        }

        private void TryUse()
        {
            if (_cooldownTimer > 0f || _charge < _data.MaxCharge)
                return;

            PushEnemies();

            _charge = 0f;
            _cooldownTimer = _data.Cooldown;
            ChargeChanged?.Invoke(0f);
        }

        private void TickCooldown()
        {
            if (_cooldownTimer > 0f)
                _cooldownTimer -= Time.deltaTime;
        }

        private void PushEnemies()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, _data.Radius, _data.EnemyLayer);

            foreach (Collider hit in hits)
            {
                EnemyController enemy = hit.GetComponentInParent<EnemyController>();

                if (enemy == null)
                    continue;

                Vector3 direction = (enemy.transform.position - transform.position).normalized;
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                float forceMultiplier = 1f - Mathf.Clamp01(distance / _data.Radius);

                enemy.Knockback(direction * (_data.Force * forceMultiplier));
            }
        }
    }
}
