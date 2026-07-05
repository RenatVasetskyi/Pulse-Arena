using Game.Pickups;
using UnityEngine;

namespace Game.Pickups.Interfaces
{
    public interface IPickupFactory
    {
        HealthOrbPickup CreateHealthOrb(Vector3 at, Quaternion rotation, Transform parent);
    }
}
