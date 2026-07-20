using Architecture.Services.Interfaces;
using Data;
using Game.Pooling;
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

        private ComponentPool<TurretBullet> _bulletPool;
        private Transform _poolRoot;

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

            _bulletPool ??= CreateBulletPool(prefab);

            TurretBullet bullet = _bulletPool.Get();
            bullet.transform.SetPositionAndRotation(position, Quaternion.identity);

            TurretData data = _gameSettings.TurretData;
            bullet.Initialize(direction, data.BulletSpeed, data.BulletDamage, data.BulletLifetime,
                _gameSettings.SlingshotData.ObstacleLayer, _pauseService);

            return bullet;
        }

        // Bullets fire once per turret per FireInterval, so they pool instead of Instantiate/Destroy per shot (mirrors
        // EnemyFactory). The pool root is a scene object destroyed on scene unload and the factory is per-match, so a
        // fresh pool is built each match — no explicit Clear() needed.
        private ComponentPool<TurretBullet> CreateBulletPool(GameObject prefab)
        {
            return new ComponentPool<TurretBullet>(
                () => CreateBulletInstance(prefab),
                bullet => bullet.gameObject.SetActive(true),
                ReleaseBulletInstance,
                0);
        }

        private TurretBullet CreateBulletInstance(GameObject prefab)
        {
            TurretBullet bullet = Object.Instantiate(prefab, GetPoolRoot()).GetComponent<TurretBullet>();
            bullet.SetPoolReturnAction(released => _bulletPool.Release(released));

            return bullet;
        }

        private void ReleaseBulletInstance(TurretBullet bullet)
        {
            bullet.PrepareForPool();
            bullet.transform.SetParent(GetPoolRoot(), false);
            bullet.gameObject.SetActive(false);
        }

        private Transform GetPoolRoot()
        {
            if (_poolRoot != null)
                return _poolRoot;

            _poolRoot = new GameObject("Turret Bullet Pool").transform;

            return _poolRoot;
        }
    }
}
