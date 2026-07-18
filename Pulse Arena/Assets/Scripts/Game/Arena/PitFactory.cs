using Data;
using Game.Arena.Interfaces;
using Game.Pooling;
using UnityEngine;
using Zenject;

namespace Game.Arena
{
    public class PitFactory : IPitFactory
    {
        private readonly DiContainer _container;
        private readonly GameSettings _gameSettings;

        private ComponentPool<Pit> _pool;
        private Transform _poolRoot;

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

            _pool ??= CreatePitPool(prefab);

            Pit pit = _pool.Get();
            pit.transform.SetParent(parent, false);
            pit.transform.SetPositionAndRotation(at, Quaternion.identity);

            PitData data = _gameSettings.PitData;
            pit.Initialize(scale, lifetime, data.SuckDown);

            return pit;
        }

        // Pooled rather than Instantiate/Destroy per cycle since pits spawn on an interval. The pool root is a
        // scene object and the factory is per-match, so a fresh pool builds each match — no explicit Clear() needed.
        private ComponentPool<Pit> CreatePitPool(GameObject prefab)
        {
            return new ComponentPool<Pit>(
                () => CreatePitInstance(prefab),
                pit => pit.gameObject.SetActive(true),
                ReleasePitInstance,
                0);
        }

        private Pit CreatePitInstance(GameObject prefab)
        {
            Pit pit = _container.InstantiatePrefabForComponent<Pit>(
                prefab, Vector3.zero, Quaternion.identity, GetPoolRoot());
            pit.SetPoolReturnAction(released => _pool.Release(released));

            return pit;
        }

        private void ReleasePitInstance(Pit pit)
        {
            pit.PrepareForPool();
            pit.transform.SetParent(GetPoolRoot(), false);
            pit.gameObject.SetActive(false);
        }

        private Transform GetPoolRoot()
        {
            if (_poolRoot != null)
                return _poolRoot;

            _poolRoot = new GameObject("Pit Pool").transform;

            return _poolRoot;
        }
    }
}