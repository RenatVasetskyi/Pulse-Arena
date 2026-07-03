using System;
using Data;
using Game.Player.Interfaces;
using UnityEngine;
using Zenject;

namespace Game.Player
{
    public class PlayerFactory : IPlayerFactory
    {
        private readonly DiContainer _container;
        private readonly GameSettings _gameSettings;

        public PlayerFactory(DiContainer container, GameSettings gameSettings)
        {
            _container = container;
            _gameSettings = gameSettings;
        }

        public PlayerController Create(Vector3 at, Quaternion rotation, Transform parent)
        {
            if (_gameSettings.Prefabs.PlayerPrefab == null)
                throw new InvalidOperationException("Player prefab is not assigned in GameSettings.");

            return _container.InstantiatePrefabForComponent<PlayerController>
                (_gameSettings.Prefabs.PlayerPrefab, at, rotation, parent);
        }
    }
}
