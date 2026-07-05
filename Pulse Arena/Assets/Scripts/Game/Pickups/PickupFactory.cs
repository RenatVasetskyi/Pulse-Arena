using Game.Pickups.Interfaces;
using UnityEngine;
using Zenject;

namespace Game.Pickups
{
    public class PickupFactory : IPickupFactory
    {
        private readonly DiContainer _container;

        public PickupFactory(DiContainer container)
        {
            _container = container;
        }

        public HealthOrbPickup CreateHealthOrb(Vector3 at, Quaternion rotation, Transform parent)
        {
            GameObject instance = new("Health Orb");
            instance.transform.SetParent(parent, false);
            instance.transform.SetPositionAndRotation(at, rotation);

            HealthOrbPickup pickup = instance.AddComponent<HealthOrbPickup>();
            _container.Inject(pickup);
            pickup.Initialize();

            return pickup;
        }
    }
}
