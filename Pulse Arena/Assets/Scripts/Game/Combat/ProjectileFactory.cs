using Data;
using Game.Combat.Interfaces;
using UnityEngine;
using Zenject;

namespace Game.Combat
{
    public class ProjectileFactory : IProjectileFactory
    {
        private readonly GameSettings _gameSettings;
        private readonly DiContainer _container;

        public ProjectileFactory(DiContainer container, GameSettings gameSettings)
        {
            _container = container;
            _gameSettings = gameSettings;
        }

        public Projectile Create(Vector3 at, Quaternion rotation, Vector3 direction)
        {
            GameObject projectileRoot = new("Pulse Projectile");
            projectileRoot.transform.SetPositionAndRotation(at, rotation);
            projectileRoot.transform.localScale = Vector3.one * _gameSettings.WeaponData.ProjectileVisualScale;

            ProjectileVfxUtility.CreateProjectileVisual(projectileRoot.transform);

            Projectile projectile = projectileRoot.AddComponent<Projectile>();
            _container.Inject(projectile);
            projectile.Initialize(direction);

            return projectile;
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
