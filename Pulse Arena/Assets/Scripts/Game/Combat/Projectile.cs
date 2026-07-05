using Data;
using Game.Enemy;
using System;
using UnityEngine;
using Zenject;

namespace Game.Combat
{
    public class Projectile : MonoBehaviour
    {
        private WeaponData _weaponData;
        private Vector3 _direction;
        private Action<Projectile> _releaseAction;
        private Action<Vector3> _impactAction;
        private float _lifetime;
        private float _travelledDistance;
        private bool _isInitialized;

        [Inject]
        public void Construct(GameSettings gameSettings)
        {
            _weaponData = gameSettings.WeaponData;
        }

        public void Initialize(
            Vector3 direction,
            Action<Projectile> releaseAction,
            Action<Vector3> impactAction)
        {
            _direction = direction.normalized;
            _releaseAction = releaseAction;
            _impactAction = impactAction;
            _lifetime = 0f;
            _travelledDistance = 0f;
            _isInitialized = true;
            ClearTrails();
        }

        public void ResetForPool()
        {
            _direction = Vector3.zero;
            _releaseAction = null;
            _impactAction = null;
            _lifetime = 0f;
            _travelledDistance = 0f;
            _isInitialized = false;
            ClearTrails();
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
                Release();
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

            enemy.TakeDamage(_weaponData.Damage);

            _impactAction?.Invoke(hitPoint);
            Release();
        }

        private void TickLifetime()
        {
            _lifetime += Time.deltaTime;

            if (_lifetime >= _weaponData.ProjectileLifetime)
                Release();
        }

        private void Release()
        {
            if (!_isInitialized)
                return;

            _isInitialized = false;
            _releaseAction?.Invoke(this);
        }

        private void ClearTrails()
        {
            TrailRenderer[] trails = GetComponentsInChildren<TrailRenderer>(true);

            foreach (TrailRenderer trail in trails)
                trail.Clear();
        }
    }
}
