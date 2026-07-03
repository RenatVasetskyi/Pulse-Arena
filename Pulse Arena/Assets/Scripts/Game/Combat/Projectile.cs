using Architecture.Services.Interfaces;
using Data;
using Game.Enemy;
using UnityEngine;
using Zenject;

namespace Game.Combat
{
    public class Projectile : MonoBehaviour
    {
        private WeaponData _weaponData;
        private EnemyData _enemyData;
        private IScoreService _scoreService;
        private Vector3 _direction;
        private float _lifetime;
        private float _travelledDistance;
        private bool _isInitialized;

        [Inject]
        public void Construct(GameSettings gameSettings, IScoreService scoreService)
        {
            _weaponData = gameSettings.WeaponData;
            _enemyData = gameSettings.EnemyData;
            _scoreService = scoreService;
        }

        public void Initialize(Vector3 direction)
        {
            _direction = direction.normalized;
            _isInitialized = true;
        }

        private void Update()
        {
            if (!_isInitialized)
                return;

            TickLifetime();
            Move();
        }

        private void Move()
        {
            Vector3 currentPosition = transform.position;
            Vector3 nextPosition = currentPosition + _direction * (_weaponData.ProjectileSpeed * Time.deltaTime);
            float distance = Vector3.Distance(currentPosition, nextPosition);

            if (TryHitEnemy(currentPosition, distance, out EnemyController enemy, out Vector3 hitPoint))
            {
                Hit(enemy, hitPoint);
                return;
            }

            transform.position = nextPosition;
            _travelledDistance += distance;

            if (_travelledDistance >= _weaponData.Range)
                Destroy(gameObject);
        }

        private bool TryHitEnemy(Vector3 origin, float distance, out EnemyController enemy, out Vector3 hitPoint)
        {
            enemy = null;
            hitPoint = origin + _direction * distance;

            int layerMask = _weaponData.EnemyLayer.value == 0
                ? Physics.DefaultRaycastLayers
                : _weaponData.EnemyLayer.value;

            if (!Physics.SphereCast(
                    origin,
                    _weaponData.Radius,
                    _direction,
                    out RaycastHit hit,
                    distance,
                    layerMask,
                    QueryTriggerInteraction.Collide))
            {
                return false;
            }

            enemy = hit.collider.GetComponentInParent<EnemyController>();

            if (enemy == null)
                return false;

            hitPoint = hit.point;
            return true;
        }

        private void Hit(EnemyController enemy, Vector3 hitPoint)
        {
            enemy.Knockback(_direction * _weaponData.KnockbackForce);

            if (enemy.TakeDamage(_weaponData.Damage))
                _scoreService.Add(_enemyData.ScoreReward);

            SpawnImpact(hitPoint);
            Destroy(gameObject);
        }

        private void SpawnImpact(Vector3 at)
        {
            GameObject impact = new GameObject("Projectile Impact");
            impact.transform.position = at;
            impact.transform.localScale = Vector3.one * _weaponData.ImpactVisualScale;

            ProjectileVfxUtility.CreateImpactVisual(impact.transform);
            Destroy(impact, _weaponData.ImpactLifetime);
        }

        private void TickLifetime()
        {
            _lifetime += Time.deltaTime;

            if (_lifetime >= _weaponData.ProjectileLifetime)
                Destroy(gameObject);
        }
    }
}
