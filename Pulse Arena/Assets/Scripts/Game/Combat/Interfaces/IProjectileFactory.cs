using UnityEngine;

namespace Game.Combat.Interfaces
{
    public interface IProjectileFactory
    {
        void Preload();
        Projectile Create(Vector3 at, Quaternion rotation, Vector3 direction);
        GameObject CreateVisual(Transform parent);
    }
}
