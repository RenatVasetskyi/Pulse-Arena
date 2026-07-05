using System;
using Data;
using Game.Common;
using Game.Enemy.Interfaces;
using Game.Pooling;
using UnityEngine;
using Zenject;

namespace Game.Enemy
{
    public class EnemyFactory : IEnemyFactory
    {
        private readonly DiContainer _container;
        private readonly GameSettings _gameSettings;

        private ComponentPool<EnemyController> _enemyPool;
        private Transform _poolRoot;

        public EnemyFactory(DiContainer container, GameSettings gameSettings)
        {
            _container = container;
            _gameSettings = gameSettings;
        }

        public void Preload()
        {
            if (_gameSettings.Prefabs.EnemyPrefab == null)
                throw new InvalidOperationException("Enemy prefab is not assigned in GameSettings.");

            EnsurePool();
        }

        public EnemyController Create(Vector3 at, Quaternion rotation, Transform parent, Transform target,
            EnemyTypeData typeData = null)
        {
            if (_gameSettings.Prefabs.EnemyPrefab == null)
                throw new InvalidOperationException("Enemy prefab is not assigned in GameSettings.");

            EnsurePool();

            EnemyController enemy = _enemyPool.Get();
            enemy.transform.SetParent(parent, false);
            enemy.transform.SetPositionAndRotation(at, rotation);

            ActorGroundingUtility.SnapToGround(enemy.transform);
            enemy.Initialize(target, typeData);

            return enemy;
        }

        private void EnsurePool()
        {
            if (_enemyPool != null)
                return;

            _enemyPool = new ComponentPool<EnemyController>(
                CreateEnemyInstance,
                enemy => enemy.gameObject.SetActive(true),
                ReleaseEnemyInstance,
                GetEnemyPreloadCount());
        }

        private EnemyController CreateEnemyInstance()
        {
            EnemyController enemy = _container.InstantiatePrefabForComponent<EnemyController>
                (_gameSettings.Prefabs.EnemyPrefab, Vector3.zero, Quaternion.identity, GetPoolRoot());

            enemy.SetPoolReturnAction(Release);
            return enemy;
        }

        private void Release(EnemyController enemy)
        {
            _enemyPool?.Release(enemy);
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

        private int GetEnemyPreloadCount()
        {
            int configuredCount = _gameSettings.PoolData != null
                ? _gameSettings.PoolData.EnemyPreloadCount
                : _gameSettings.SpawnData.MaxEnemies;

            return Mathf.Max(_gameSettings.SpawnData.MaxEnemies, configuredCount);
        }
    }
}
