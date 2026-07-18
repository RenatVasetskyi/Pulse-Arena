using System;
using Data;
using Game.Arena.Interfaces;
using UnityEngine;
using Zenject;

namespace Game.Arena
{
    public class ArenaFactory : IArenaFactory
    {
        private readonly DiContainer _container;
        private readonly GameSettings _gameSettings;

        public ArenaFactory(DiContainer container, GameSettings gameSettings)
        {
            _container = container;
            _gameSettings = gameSettings;
        }

        public GameObject Create()
        {
            if (_gameSettings.Prefabs.ArenaPrefab == null)
                throw new InvalidOperationException("Arena prefab is not assigned in GameSettings.");

            // Injects every arena MonoBehaviour (BattleCamera, etc.); the active NavMeshSurface bakes its
            // NavMesh, so the arena is fully ready by the time this returns — before player/enemies spawn.
            return _container.InstantiatePrefab(_gameSettings.Prefabs.ArenaPrefab);
        }
    }
}