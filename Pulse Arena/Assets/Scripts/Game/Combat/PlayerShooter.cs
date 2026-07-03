using Architecture.Services.Interfaces;
using Data;
using Game.Combat.Interfaces;
using UnityEngine;
using Zenject;

namespace Game.Combat
{
    public class PlayerShooter : MonoBehaviour
    {
        private IInputService _inputService;
        private IProjectileFactory _projectileFactory;
        private WeaponData _weaponData;
        private GameObject _orbitVisual;
        private float _cooldownTimer;
        private float _orbitAngle;

        [Inject]
        public void Construct(
            IInputService inputService,
            IProjectileFactory projectileFactory,
            GameSettings gameSettings)
        {
            _inputService = inputService;
            _projectileFactory = projectileFactory;
            _weaponData = gameSettings.WeaponData;
        }

        private void Start()
        {
            CreateOrbitVisual();
            UpdateOrbitVisual();
        }

        private void Update()
        {
            TickCooldown();
            UpdateOrbitVisual();

            if (_inputService.IsShootPressedThisFrame)
                TryShoot();
        }

        private void TryShoot()
        {
            if (_cooldownTimer > 0f)
                return;

            _cooldownTimer = _weaponData.Cooldown;

            Vector3 origin = GetOrbitPosition();
            Vector3 direction = GetShootDirection();
            Quaternion rotation = Quaternion.LookRotation(direction);

            _projectileFactory.Create(origin, rotation, direction);
        }

        private void CreateOrbitVisual()
        {
            if (_orbitVisual != null)
                return;

            _orbitVisual = _projectileFactory.CreateVisual(transform);
        }

        private void UpdateOrbitVisual()
        {
            _orbitAngle += _weaponData.OrbitAngularSpeed * Time.deltaTime;

            if (_orbitVisual == null)
                return;

            Vector3 direction = GetShootDirection();
            _orbitVisual.transform.position = GetOrbitPosition();
            _orbitVisual.transform.rotation = Quaternion.LookRotation(direction);
            _orbitVisual.transform.localScale = Vector3.one * _weaponData.OrbitVisualScale;
        }

        private Vector3 GetOrbitPosition()
        {
            Vector3 offset = GetOrbitOffset();
            return transform.position + Vector3.up * _weaponData.OrbitHeight + offset;
        }

        private Vector3 GetShootDirection()
        {
            Vector3 offset = GetOrbitOffset();

            if (offset.sqrMagnitude <= 0.001f)
                return transform.forward;

            return offset.normalized;
        }

        private Vector3 GetOrbitOffset()
        {
            return Quaternion.Euler(0f, _orbitAngle, 0f) * Vector3.forward * _weaponData.OrbitRadius;
        }

        private void TickCooldown()
        {
            if (_cooldownTimer > 0f)
                _cooldownTimer -= Time.deltaTime;
        }
    }
}
