using UnityEngine;

namespace Game.Pickups.Interfaces
{
    public interface IPickupFactory
    {
        EnergyPickup Create(Vector3 at, Quaternion rotation, Transform parent);
    }
}
