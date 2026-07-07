using Data;
using Game.Pickups.Interfaces;
using UnityEngine;
using Zenject;

namespace Game.Pickups
{
    public class PickupFactory : IPickupFactory
    {
        private readonly DiContainer _container;
        private readonly GameSettings _gameSettings;

        public PickupFactory(DiContainer container, GameSettings gameSettings)
        {
            _container = container;
            _gameSettings = gameSettings;
        }

        public HealthOrbPickup CreateHealthOrb(Vector3 at, Quaternion rotation, Transform parent)
        {
            GameObject prefab = _gameSettings.Prefabs.HealthOrbPrefab;

            if (prefab == null)
            {
                Debug.LogError("HealthOrbPrefab is not assigned in Game Settings → Prefabs.");
                return null;
            }

            HealthOrbPickup pickup = _container.InstantiatePrefabForComponent<HealthOrbPickup>(
                prefab, at, rotation, parent);
            pickup.Initialize();

            return pickup;
        }
    }
}
