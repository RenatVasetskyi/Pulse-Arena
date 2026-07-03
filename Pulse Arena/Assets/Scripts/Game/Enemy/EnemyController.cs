using System.Collections;
using Data;
using System;
using Game.Player;
using UnityEngine;
using Zenject;

namespace Game.Enemy
{
    public class EnemyController : MonoBehaviour
    {
        public event Action<EnemyController> Destroyed;

        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private Renderer[] _renderers;

        private EnemyData _data;
        private Transform _target;
        private PlayerController _playerTarget;
        private Material[][] _originalMaterials;
        private Material _hitFlashMaterial;
        private Coroutine _flashRoutine;
        private float _knockbackTimer;
        private float _stasisTimer;
        private float _attackCooldownTimer;
        private float _impactDamageCooldownTimer;
        private int _health;
        private bool _isDead;
        private bool _isGrabbed;

        public bool IsGrabbed
        {
            get { return _isGrabbed; }
        }

        [Inject]
        public void Construct(GameSettings gameSettings)
        {
            _data = gameSettings.EnemyData;
            _health = Mathf.Max(1, _data.MaxHealth);
        }

        public void Initialize(Transform target)
        {
            _target = target;
            _playerTarget = target.GetComponentInParent<PlayerController>();
        }

        public void Knockback(Vector3 force)
        {
            _isGrabbed = false;
            _stasisTimer = 0f;
            _knockbackTimer = _data.KnockbackDuration;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.AddForce(force, ForceMode.VelocityChange);
        }

        public void Grab()
        {
            if (_isDead)
                return;

            _isGrabbed = true;
            _stasisTimer = 0f;
            _knockbackTimer = 0f;
            _rigidbody.linearVelocity = Vector3.zero;
        }

        public void MoveGrabbed(Vector3 targetPosition, float followSpeed)
        {
            if (_isDead || !_isGrabbed)
                return;

            Vector3 velocity = (targetPosition - transform.position) * followSpeed;
            _rigidbody.linearVelocity = velocity;
        }

        public void Launch(Vector3 velocity, float duration)
        {
            if (_isDead)
                return;

            _isGrabbed = false;
            _stasisTimer = 0f;
            _knockbackTimer = duration;
            _rigidbody.linearVelocity = velocity;
        }

        public void PullTo(Vector3 targetPosition, float force, float upwardForceRatio, float stasisDuration)
        {
            _stasisTimer = Mathf.Max(_stasisTimer, stasisDuration);
            _knockbackTimer = 0f;

            Vector3 direction = targetPosition - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.001f)
                return;

            Vector3 pullVelocity = direction.normalized * force;
            pullVelocity += Vector3.up * (force * upwardForceRatio);

            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.AddForce(pullVelocity, ForceMode.VelocityChange);
        }

        public bool TakeDamage(int damage)
        {
            if (_isDead)
                return false;

            _health -= Mathf.Max(0, damage);

            if (_health > 0)
            {
                FlashHit();
                return false;
            }

            _isDead = true;
            Destroy(gameObject);

            return true;
        }

        private void Awake()
        {
            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody>();

            if (_renderers == null || _renderers.Length == 0)
                _renderers = GetComponentsInChildren<Renderer>();

            CacheMaterials();
            CreateHitFlashMaterial();
        }

        private void FlashHit()
        {
            if (_flashRoutine != null)
                StopCoroutine(_flashRoutine);

            _flashRoutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            ApplyFlashMaterials();

            yield return new WaitForSeconds(_data.HitFlashDuration);

            RestoreMaterials();
            _flashRoutine = null;
        }

        private void CacheMaterials()
        {
            _originalMaterials = new Material[_renderers.Length][];

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null)
                    continue;

                _originalMaterials[i] = _renderers[i].sharedMaterials;
            }
        }

        private void CreateHitFlashMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            shader ??= Shader.Find("Sprites/Default");

            _hitFlashMaterial = new Material(shader)
            {
                name = "Enemy Hit Flash"
            };

            if (_hitFlashMaterial.HasProperty("_BaseColor"))
                _hitFlashMaterial.SetColor("_BaseColor", _data.HitFlashColor);

            if (_hitFlashMaterial.HasProperty("_Color"))
                _hitFlashMaterial.SetColor("_Color", _data.HitFlashColor);
        }

        private void ApplyFlashMaterials()
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer enemyRenderer = _renderers[i];

                if (enemyRenderer == null)
                    continue;

                Material[] flashMaterials = new Material[enemyRenderer.sharedMaterials.Length];

                for (int j = 0; j < flashMaterials.Length; j++)
                    flashMaterials[j] = _hitFlashMaterial;

                enemyRenderer.sharedMaterials = flashMaterials;
            }
        }

        private void RestoreMaterials()
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null || _originalMaterials[i] == null)
                    continue;

                _renderers[i].sharedMaterials = _originalMaterials[i];
            }
        }

        private void OnDestroy()
        {
            Destroyed?.Invoke(this);
        }

        private void FixedUpdate()
        {
            TickTimers();

            if (_isGrabbed)
                return;

            ApplyExtraGravity();

            if (_target == null)
                return;

            if (_stasisTimer > 0f)
                return;

            if (_knockbackTimer > 0f)
                return;

            MoveToTarget();
            TryAttackTarget();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_isDead || _impactDamageCooldownTimer > 0f || _knockbackTimer <= 0f)
                return;

            if (collision.collider.GetComponentInParent<PlayerController>() != null)
                return;

            if (IsGroundCollision(collision))
                return;

            if (_rigidbody.linearVelocity.magnitude < _data.ImpactDamageMinSpeed)
                return;

            TryDamageOtherEnemy(collision);

            _impactDamageCooldownTimer = _data.ImpactDamageCooldown;
            TakeDamage(_data.ImpactDamage);
        }

        private void TryDamageOtherEnemy(Collision collision)
        {
            EnemyController otherEnemy = collision.collider.GetComponentInParent<EnemyController>();

            if (otherEnemy == null || otherEnemy == this)
                return;

            Vector3 direction = otherEnemy.transform.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.001f)
                direction = _rigidbody.linearVelocity;

            direction.y = 0f;
            otherEnemy.Knockback((direction.normalized + Vector3.up * 0.25f).normalized *
                _data.ImpactDamageMinSpeed);
            otherEnemy.TakeDamage(_data.ImpactDamage);
        }

        private bool IsGroundCollision(Collision collision)
        {
            foreach (ContactPoint contact in collision.contacts)
            {
                if (contact.normal.y > 0.65f)
                    return true;
            }

            return false;
        }

        private void TickTimers()
        {
            if (_stasisTimer > 0f)
                _stasisTimer -= Time.fixedDeltaTime;

            if (_knockbackTimer > 0f)
                _knockbackTimer -= Time.fixedDeltaTime;

            if (_attackCooldownTimer > 0f)
                _attackCooldownTimer -= Time.fixedDeltaTime;

            if (_impactDamageCooldownTimer > 0f)
                _impactDamageCooldownTimer -= Time.fixedDeltaTime;
        }

        private void ApplyExtraGravity()
        {
            _rigidbody.AddForce(Vector3.down * _data.ExtraGravity, ForceMode.Acceleration);
        }

        private void MoveToTarget()
        {
            Vector3 direction = (_target.position - transform.position).normalized;
            direction.y = 0f;
            Vector3 horizontalVelocity = direction * _data.MoveSpeed;

            _rigidbody.linearVelocity = new Vector3(
                horizontalVelocity.x,
                _rigidbody.linearVelocity.y,
                horizontalVelocity.z);

            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Lerp(transform.rotation,
                    targetRotation, _data.RotationSpeed * Time.fixedDeltaTime);
            }
        }

        private void TryAttackTarget()
        {
            if (_playerTarget == null || _attackCooldownTimer > 0f)
                return;

            Vector3 offset = _playerTarget.transform.position - transform.position;
            offset.y = 0f;

            if (offset.sqrMagnitude > _data.AttackRange * _data.AttackRange)
                return;

            if (_playerTarget.TakeDamage(_data.ContactDamage, transform.position))
                _attackCooldownTimer = _data.AttackCooldown;
        }
    }
}
