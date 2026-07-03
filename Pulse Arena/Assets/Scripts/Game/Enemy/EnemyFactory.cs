using System;
using Data;
using Game.Enemy.Interfaces;
using UnityEngine;
using Zenject;

namespace Game.Enemy
{
    public class EnemyFactory : IEnemyFactory
    {
        private readonly DiContainer _container;
        private readonly GameSettings _gameSettings;

        public EnemyFactory(DiContainer container, GameSettings gameSettings)
        {
            _container = container;
            _gameSettings = gameSettings;
        }

        public EnemyController Create(Vector3 at, Quaternion rotation, Transform parent, Transform target)
        {
            if (_gameSettings.Prefabs.EnemyPrefab == null)
                throw new InvalidOperationException("Enemy prefab is not assigned in GameSettings.");

            EnemyController enemy = _container.InstantiatePrefabForComponent<EnemyController>
                (_gameSettings.Prefabs.EnemyPrefab, at, rotation, parent);

            enemy.Initialize(target);

            return enemy;
        }
    }
}
