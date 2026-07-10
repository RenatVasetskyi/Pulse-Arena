using Architecture.Services.Interfaces;
using Data;
using UnityEngine;
using Zenject;

namespace Game.Turrets
{
    /// <summary>
    ///     Builds turrets (Zenject-instantiated so the <see cref="Turret" />'s Construct is injected) and their
    ///     bullets (plain Instantiate — the bullet takes everything it needs through <see cref="TurretBullet.Initialize" />).
    /// </summary>
    public class TurretFactory : ITurretFactory
    {
        private readonly DiContainer _container;
        private readonly GameSettings _gameSettings;
        private readonly IPauseService _pauseService;

        public TurretFactory(DiContainer container, GameSettings gameSettings, IPauseService pauseService)
        {
            _container = container;
            _gameSettings = gameSettings;
            _pauseService = pauseService;
        }

        public Turret CreateTurret(Vector3 position, Transform parent, Transform target)
        {
            GameObject prefab = _gameSettings.Prefabs.TurretPrefab;

            if (prefab == null)
            {
                Debug.LogError("TurretPrefab is not assigned in Game Settings → Prefabs.");
                return null;
            }

            Turret turret = _container.InstantiatePrefabForComponent<Turret>(
                prefab, position, Quaternion.identity, parent);
            turret.Initialize(target);

            return turret;
        }

        public TurretBullet CreateBullet(Vector3 position, Vector3 direction)
        {
            GameObject prefab = _gameSettings.Prefabs.TurretBulletPrefab;

            if (prefab == null)
                return null;

            TurretBullet bullet = Object.Instantiate(prefab, position, Quaternion.identity)
                .GetComponent<TurretBullet>();
            TurretData data = _gameSettings.TurretData;
            bullet.Initialize(direction, data.BulletSpeed, data.BulletDamage, data.BulletLifetime,
                _gameSettings.SlingshotData.ObstacleLayer, _pauseService);

            return bullet;
        }
    }
}
