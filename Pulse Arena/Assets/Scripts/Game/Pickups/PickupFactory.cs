using System;
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

        public EnergyPickup Create(Vector3 at, Quaternion rotation, Transform parent)
        {
            if (_gameSettings.Prefabs.EnergyPickupPrefab == null)
                throw new InvalidOperationException("Energy pickup prefab is not assigned in GameSettings.");

            return _container.InstantiatePrefabForComponent<EnergyPickup>
                (_gameSettings.Prefabs.EnergyPickupPrefab, at, rotation, parent);
        }
    }
}
