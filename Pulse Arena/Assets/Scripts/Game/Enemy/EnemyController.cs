using Data;
using System;
using UnityEngine;
using Zenject;

namespace Game.Enemy
{
    public class EnemyController : MonoBehaviour
    {
        public event Action<EnemyController> Destroyed;

        [SerializeField] private Rigidbody _rigidbody;

        private EnemyData _data;
        private Transform _target;
        private float _knockbackTimer;

        [Inject]
        public void Construct(GameSettings gameSettings)
        {
            _data = gameSettings.EnemyData;
        }

        public void Initialize(Transform target)
        {
            _target = target;
        }

        public void Knockback(Vector3 force)
        {
            _knockbackTimer = _data.KnockbackDuration;
            _rigidbody.AddForce(force, ForceMode.Impulse);
        }

        private void Awake()
        {
            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody>();
        }

        private void OnDestroy()
        {
            Destroyed?.Invoke(this);
        }

        private void FixedUpdate()
        {
            if (_target == null)
                return;

            if (_knockbackTimer > 0f)
            {
                _knockbackTimer -= Time.fixedDeltaTime;
                return;
            }

            MoveToTarget();
        }

        private void MoveToTarget()
        {
            Vector3 direction = (_target.position - transform.position).normalized;
            direction.y = 0f;

            _rigidbody.linearVelocity = direction * _data.MoveSpeed;

            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Lerp(transform.rotation,
                    targetRotation, _data.RotationSpeed * Time.fixedDeltaTime);
            }
        }
    }
}
