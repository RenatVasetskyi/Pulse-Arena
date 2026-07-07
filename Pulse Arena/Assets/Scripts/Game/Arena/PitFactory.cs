using Data;
using Game.Arena.Interfaces;
using UnityEngine;
using Zenject;

namespace Game.Arena
{
    public class PitFactory : IPitFactory
    {
        private readonly DiContainer _container;
        private readonly GameSettings _gameSettings;

        public PitFactory(DiContainer container, GameSettings gameSettings)
        {
            _container = container;
            _gameSettings = gameSettings;
        }

        public Pit Create(Vector3 at, float scale, float lifetime, Transform parent)
        {
            GameObject prefab = _gameSettings.Prefabs.PitPrefab;

            if (prefab == null)
            {
                Debug.LogError("PitPrefab is not assigned in Game Settings → Prefabs.");
                return null;
            }

            Pit pit = _container.InstantiatePrefabForComponent<Pit>(prefab, at, Quaternion.identity, parent);
            PitData data = _gameSettings.PitData;
            pit.Initialize(scale, lifetime, data.SuckSpeed, data.SuckDown);

            return pit;
        }
    }
}
