using Architecture.Services.Interfaces;
using Data;
using UnityEngine;
using Zenject;

namespace Game.Player
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private Rigidbody _rigidbody;

        private IInputService _inputService;
        private PlayerData _data;
        private int _health;
        private float _hitInvulnerabilityTimer;
        private float _hitKnockbackTimer;
        private bool _isDead;

        [Inject]
        public void Construct(IInputService inputService, GameSettings gameSettings)
        {
            _inputService = inputService;
            _data = gameSettings.PlayerData;
            _health = Mathf.Max(1, _data.MaxHealth);
        }

        private void Awake()
        {
            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            TickHitInvulnerability();
            RotateToInput();
        }

        public bool TakeDamage(int damage, Vector3 sourcePosition)
        {
            if (_isDead)
                return false;

            if (_hitInvulnerabilityTimer > 0f)
                return false;

            _health -= Mathf.Max(0, damage);
            _hitInvulnerabilityTimer = _data.HitInvulnerability;
            _hitKnockbackTimer = _data.HitKnockbackDuration;

            Vector3 knockbackDirection = transform.position - sourcePosition;
            knockbackDirection.y = 0f;

            if (knockbackDirection.sqrMagnitude <= 0.001f)
                knockbackDirection = -transform.forward;

            _rigidbody.AddForce(knockbackDirection.normalized * _data.HitKnockbackForce,
                ForceMode.VelocityChange);

            Debug.Log($"Player hit. Health: {Mathf.Max(0, _health)}");

            if (_health <= 0)
            {
                _isDead = true;
                Debug.Log("Player died.");
            }

            return true;
        }

        private void FixedUpdate()
        {
            if (_hitKnockbackTimer > 0f)
                _hitKnockbackTimer -= Time.fixedDeltaTime;
            else
                Move();

            ApplyExtraGravity();
        }

        private void Move()
        {
            Vector2 input = _inputService.MoveDirection;
            Vector3 direction = new Vector3(input.x, 0f, input.y);
            Vector3 horizontalVelocity = direction * _data.MoveSpeed;

            _rigidbody.linearVelocity = new Vector3(
                horizontalVelocity.x,
                _rigidbody.linearVelocity.y,
                horizontalVelocity.z);
        }

        private void ApplyExtraGravity()
        {
            _rigidbody.AddForce(Vector3.down * _data.ExtraGravity, ForceMode.Acceleration);
        }

        private void TickHitInvulnerability()
        {
            if (_hitInvulnerabilityTimer > 0f)
                _hitInvulnerabilityTimer -= Time.deltaTime;
        }

        private void RotateToInput()
        {
            Vector2 input = _inputService.MoveDirection;

            if (input.sqrMagnitude <= 0.01f)
                return;

            Vector3 direction = new Vector3(input.x, 0f, input.y);
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                targetRotation, _data.RotationSpeed * Time.deltaTime);
        }
    }
}
