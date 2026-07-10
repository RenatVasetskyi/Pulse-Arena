using UnityEngine;

namespace Game.Turrets
{
    public interface ITurretFactory
    {
        Turret CreateTurret(Vector3 position, Transform parent, Transform target);
        TurretBullet CreateBullet(Vector3 position, Vector3 direction);
    }
}
