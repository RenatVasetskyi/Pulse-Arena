using UnityEngine;

namespace Game.Combat.Interfaces
{
    public interface IProjectileFactory
    {
        Projectile Create(Vector3 at, Quaternion rotation, Vector3 direction);
        GameObject CreateVisual(Transform parent);
    }
}
