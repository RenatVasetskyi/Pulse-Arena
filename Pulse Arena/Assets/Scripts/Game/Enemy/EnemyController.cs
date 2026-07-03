using System.Collections;
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
        [SerializeField] private Renderer[] _renderers;

        private EnemyData _data;
        private Transform _target;
        private Material[][] _originalMaterials;
        private Material _hitFlashMaterial;
        private Coroutine _flashRoutine;
        private float _knockbackTimer;
        private int _health;
        private bool _isDead;

        [Inject]
        public void Construct(GameSettings gameSettings)
        {
            _data = gameSettings.EnemyData;
            _health = Mathf.Max(1, _data.MaxHealth);
        }

        public void Initialize(Transform target)
        {
            _target = target;
        }

        public void Knockback(Vector3 force)
        {
            _knockbackTimer = _data.KnockbackDuration;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.AddForce(force, ForceMode.VelocityChange);
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
            ApplyExtraGravity();

            if (_target == null)
                return;

            if (_knockbackTimer > 0f)
            {
                _knockbackTimer -= Time.fixedDeltaTime;
                return;
            }

            MoveToTarget();
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
    }
}
