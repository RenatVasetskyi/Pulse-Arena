using System;
using System.Collections.Generic;
using Data;
using Game.Common;
using Game.Enemy.Interfaces;
using Game.Pooling;
using UnityEngine;
using Zenject;

namespace Game.Enemy
{
    /// <summary>
    ///     DI-instantiates enemies and pools them PER PREFAB: each distinct enemy prefab (skeleton, wolf, karate…)
    ///     gets its own <see cref="ComponentPool{T}" />, keyed by the prefab, so a spawned creature always returns
    ///     to the pool it came from. The prefab for a spawn is chosen from the requested <see cref="EnemyTypeData" />
    ///     (<see cref="EnemyTypeData.Prefab" />), falling back to the shared <c>GameSettings.Prefabs.EnemyPrefab</c>
    ///     for a type that has no prefab of its own. One shared pool root parents every pooled body so teardown
    ///     destroys them all at once.
    /// </summary>
    public class EnemyFactory : IEnemyFactory
    {
        private readonly DiContainer _container;
        private readonly GameSettings _gameSettings;
        private readonly Dictionary<GameObject, ComponentPool<EnemyController>> _pools = new();

        private Transform _poolRoot;

        public EnemyFactory(DiContainer container, GameSettings gameSettings)
        {
            _container = container;
            _gameSettings = gameSettings;
        }

        public void Clear()
        {
            // Clear runs during scene unload (GameWorldBuilder.Teardown). The scene itself destroys the pooled
            // enemies (children of the pool root), so we only explicitly destroy the root if it is still alive.
            // We must NOT reparent/create GameObjects here: doing so would spawn a fresh "Enemy Pool" mid-OnDestroy
            // and trip Unity's "objects not cleaned up when closing the scene" error.
            if (_poolRoot != null)
                UnityEngine.Object.Destroy(_poolRoot.gameObject);

            _pools.Clear();
            _poolRoot = null;
        }

        public EnemyController Create(Vector3 at, Quaternion rotation, Transform parent, Transform target,
            EnemyTypeData typeData = null)
        {
            GameObject prefab = ResolvePrefab(typeData);
            ComponentPool<EnemyController> pool = GetPool(prefab, 0);

            EnemyController enemy = pool.Get();
            enemy.transform.SetParent(parent, false);
            enemy.transform.SetPositionAndRotation(at, rotation);

            ActorGroundingUtility.SnapToGround(enemy.transform, _gameSettings.Grounding);
            enemy.Initialize(target, typeData);

            return enemy;
        }

        public void Preload()
        {
            HashSet<GameObject> prefabs = CollectPrefabs();
            int perPrefab = Mathf.Max(2, Mathf.CeilToInt((float)GetPreloadBudget() / prefabs.Count));

            foreach (GameObject prefab in prefabs)
                GetPool(prefab, perPrefab);
        }

        // Default prefab + every type that declares its own — the distinct set we pool.
        private HashSet<GameObject> CollectPrefabs()
        {
            HashSet<GameObject> prefabs = new() { DefaultPrefab() };

            if (_gameSettings.EnemyTypes != null)
                foreach (EnemyTypeData type in _gameSettings.EnemyTypes)
                    if (type != null && type.Prefab != null)
                        prefabs.Add(type.Prefab);

            return prefabs;
        }

        private GameObject ResolvePrefab(EnemyTypeData typeData)
        {
            if (typeData != null && typeData.Prefab != null)
                return typeData.Prefab;

            return DefaultPrefab();
        }

        private GameObject DefaultPrefab()
        {
            if (_gameSettings.Prefabs.EnemyPrefab == null)
                throw new InvalidOperationException("Enemy prefab is not assigned in GameSettings.");

            return _gameSettings.Prefabs.EnemyPrefab;
        }

        private ComponentPool<EnemyController> GetPool(GameObject prefab, int preloadCount)
        {
            if (_pools.TryGetValue(prefab, out ComponentPool<EnemyController> existing))
                return existing;

            ComponentPool<EnemyController> pool = new(
                () => CreateEnemyInstance(prefab),
                enemy => enemy.gameObject.SetActive(true),
                ReleaseEnemyInstance,
                preloadCount);

            _pools[prefab] = pool;
            return pool;
        }

        // The spawned enemy returns itself to the pool it came from: the closure captures its prefab and looks the
        // pool up lazily (it exists by the time any body is released), so each creature never lands in a foreign pool.
        private EnemyController CreateEnemyInstance(GameObject prefab)
        {
            EnemyController enemy = _container.InstantiatePrefabForComponent<EnemyController>
                (prefab, Vector3.zero, Quaternion.identity, GetPoolRoot());

            enemy.SetPoolReturnAction(released => ReleaseTo(prefab, released));
            return enemy;
        }

        private void ReleaseTo(GameObject prefab, EnemyController enemy)
        {
            if (_pools.TryGetValue(prefab, out ComponentPool<EnemyController> pool))
                pool.Release(enemy);
        }

        private void ReleaseEnemyInstance(EnemyController enemy)
        {
            enemy.PrepareForPool();
            enemy.transform.SetParent(GetPoolRoot(), false);
            enemy.transform.localPosition = Vector3.zero;
            enemy.transform.localRotation = Quaternion.identity;
            enemy.gameObject.SetActive(false);
        }

        private Transform GetPoolRoot()
        {
            if (_poolRoot != null)
                return _poolRoot;

            _poolRoot = new GameObject("Enemy Pool").transform;
            return _poolRoot;
        }

        private int GetPreloadBudget()
        {
            int configuredCount = _gameSettings.PoolData != null
                ? _gameSettings.PoolData.EnemyPreloadCount
                : _gameSettings.SpawnData.MaxEnemies;

            return Mathf.Max(_gameSettings.SpawnData.MaxEnemies, configuredCount);
        }
    }
}
