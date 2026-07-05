using Data;
using Game.Combat.Interfaces;
using Game.Pooling;
using UnityEngine;
using Zenject;

namespace Game.Combat
{
    public class ProjectileFactory : IProjectileFactory
    {
        private readonly GameSettings _gameSettings;
        private readonly DiContainer _container;
        private ComponentPool<Projectile> _projectilePool;
        private ComponentPool<PooledProjectileImpact> _impactPool;
        private Transform _poolRoot;

        public ProjectileFactory(DiContainer container, GameSettings gameSettings)
        {
            _container = container;
            _gameSettings = gameSettings;
        }

        public void Preload()
        {
            EnsureProjectilePool();
            EnsureImpactPool();
        }

        public Projectile Create(Vector3 at, Quaternion rotation, Vector3 direction)
        {
            EnsureProjectilePool();

            Projectile projectile = _projectilePool.Get();
            projectile.transform.SetPositionAndRotation(at, rotation);
            projectile.transform.localScale = Vector3.one * _gameSettings.WeaponData.ProjectileVisualScale;
            projectile.Initialize(direction, ReleaseProjectile, SpawnImpact);

            return projectile;
        }

        private void EnsureProjectilePool()
        {
            if (_projectilePool != null)
                return;

            _projectilePool = new ComponentPool<Projectile>(
                CreateProjectileInstance,
                projectile => projectile.gameObject.SetActive(true),
                ReleaseProjectileInstance,
                GetProjectilePreloadCount());
        }

        private void EnsureImpactPool()
        {
            if (_impactPool != null)
                return;

            _impactPool = new ComponentPool<PooledProjectileImpact>(
                CreateImpactInstance,
                impact => impact.gameObject.SetActive(true),
                ReleaseImpactInstance,
                GetImpactPreloadCount());
        }

        private Projectile CreateProjectileInstance()
        {
            GameObject projectileRoot = new("Pulse Projectile");
            projectileRoot.transform.SetParent(GetPoolRoot(), false);
            projectileRoot.transform.localScale = Vector3.one * _gameSettings.WeaponData.ProjectileVisualScale;

            ProjectileVfxUtility.CreateProjectileVisual(projectileRoot.transform);

            Projectile projectile = projectileRoot.AddComponent<Projectile>();
            _container.Inject(projectile);
            return projectile;
        }

        private PooledProjectileImpact CreateImpactInstance()
        {
            GameObject impactRoot = new("Projectile Impact");
            impactRoot.transform.SetParent(GetPoolRoot(), false);
            impactRoot.transform.localScale = Vector3.one * _gameSettings.WeaponData.ImpactVisualScale;

            ProjectileVfxUtility.CreateImpactVisual(impactRoot.transform);

            return impactRoot.AddComponent<PooledProjectileImpact>();
        }

        private void SpawnImpact(Vector3 at)
        {
            EnsureImpactPool();

            PooledProjectileImpact impact = _impactPool.Get();
            impact.transform.position = at;
            impact.transform.rotation = Quaternion.identity;
            impact.transform.localScale = Vector3.one * _gameSettings.WeaponData.ImpactVisualScale;
            impact.Initialize(_gameSettings.WeaponData.ImpactLifetime, ReleaseImpact);
        }

        private void ReleaseProjectile(Projectile projectile)
        {
            _projectilePool?.Release(projectile);
        }

        private void ReleaseProjectileInstance(Projectile projectile)
        {
            projectile.ResetForPool();
            projectile.transform.SetParent(GetPoolRoot(), false);
            projectile.transform.localPosition = Vector3.zero;
            projectile.transform.localRotation = Quaternion.identity;
            projectile.gameObject.SetActive(false);
        }

        private void ReleaseImpact(PooledProjectileImpact impact)
        {
            _impactPool?.Release(impact);
        }

        private void ReleaseImpactInstance(PooledProjectileImpact impact)
        {
            impact.ResetForPool();
            impact.transform.SetParent(GetPoolRoot(), false);
            impact.transform.localPosition = Vector3.zero;
            impact.transform.localRotation = Quaternion.identity;
            impact.gameObject.SetActive(false);
        }

        private Transform GetPoolRoot()
        {
            if (_poolRoot != null)
                return _poolRoot;

            _poolRoot = new GameObject("Projectile Pool").transform;
            return _poolRoot;
        }

        private int GetProjectilePreloadCount()
        {
            return Mathf.Max(0, _gameSettings.PoolData != null
                ? _gameSettings.PoolData.ProjectilePreloadCount
                : 16);
        }

        private int GetImpactPreloadCount()
        {
            return Mathf.Max(0, _gameSettings.PoolData != null
                ? _gameSettings.PoolData.ProjectileImpactPreloadCount
                : 8);
        }

        public GameObject CreateVisual(Transform parent)
        {
            GameObject visualRoot = new("Orbit Pulse Core");
            visualRoot.transform.SetParent(parent, false);

            ProjectileVfxUtility.CreateOrbitVisual(visualRoot.transform);

            return visualRoot;
        }
    }
}
