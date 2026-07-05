using System;
using System.Collections;
using Architecture.Services.Interfaces;
using Data;
using Game.Combat;
using Game.Common.StateMachine;
using Game.Player.States;
using Game.Visuals;
using UnityEngine;
using Zenject;

namespace Game.Player
{
    public class PlayerController : MonoBehaviour
    {
        public event Action Died;
        public event Action<int, int> HealthChanged;

        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private Renderer[] _renderers;

        private PlayerPrimitiveVisual _visual;
        private IInputService _inputService;
        private PlayerData _data;
        private Material[][] _originalMaterials;
        private Material _hitFlashMaterial;
        private Coroutine _flashRoutine;
        private ActorStateMachine _stateMachine;
        private PlayerMoveState _moveState;
        private PlayerHitState _hitState;
        private PlayerDeadState _deadState;
        private int _maxHealth;
        private int _health;
        private float _hitInvulnerabilityTimer;
        private bool _isDead;

        public int Health => _health;
        public int MaxHealth => _maxHealth;
        internal PlayerData Data => _data;

        [Inject]
        public void Construct(IInputService inputService, GameSettings gameSettings)
        {
            _inputService = inputService;
            _data = gameSettings.PlayerData;
            _maxHealth = Mathf.Max(1, _data.MaxHealth);
            _health = _maxHealth;
            CreateHitFlashMaterial();
        }

        private void Awake()
        {
            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody>();

            NormalizeCapsuleRoot();
            DisablePlaceholderRenderers();
            EnsurePrimitiveVisual();
            _renderers = GetComponentsInChildren<Renderer>();

            CacheMaterials();
        }

        private void NormalizeCapsuleRoot()
        {
            CapsuleCollider capsule = GetComponent<CapsuleCollider>();

            if (capsule == null)
                return;

            Vector3 center = capsule.center;
            center.y = capsule.height * 0.5f;
            capsule.center = center;
        }

        private void Update()
        {
            EnsureStateMachine();
            TickHitInvulnerability();
            TickRingout();
            _stateMachine.Tick();
        }

        private void TickRingout()
        {
            if (!_isDead && transform.position.y < _data.RingoutHeight)
                Die();
        }

        public bool TakeDamage(int damage, Vector3 sourcePosition)
        {
            if (_isDead)
                return false;

            if (_hitInvulnerabilityTimer > 0f)
                return false;

            _health -= Mathf.Max(0, damage);
            _hitInvulnerabilityTimer = _data.HitInvulnerability;

            Vector3 knockbackDirection = transform.position - sourcePosition;
            knockbackDirection.y = 0f;

            if (knockbackDirection.sqrMagnitude <= 0.001f)
                knockbackDirection = -transform.forward;

            _rigidbody.AddForce(knockbackDirection.normalized * _data.HitKnockbackForce,
                ForceMode.VelocityChange);

            Debug.Log($"Player hit. Health: {Mathf.Max(0, _health)}");
            HealthChanged?.Invoke(Mathf.Max(0, _health), _maxHealth);
            FlashHit();
            _visual?.PlayHit();

            if (_health <= 0)
                Die();
            else
                ChangeToHitState();

            return true;
        }

        public bool TryHeal(int amount)
        {
            if (_isDead || _health >= _maxHealth)
                return false;

            _health = Mathf.Min(_maxHealth, _health + Mathf.Max(0, amount));
            HealthChanged?.Invoke(_health, _maxHealth);
            return true;
        }

        public void Kill()
        {
            Die();
        }

        private void FixedUpdate()
        {
            EnsureStateMachine();
            _stateMachine.FixedTick();
        }

        internal void MoveByInput()
        {
            if (_isDead)
                return;

            Vector2 input = _inputService.MoveDirection;
            Vector3 direction = new Vector3(input.x, 0f, input.y);
            Vector3 horizontalVelocity = direction * _data.MoveSpeed;

            _rigidbody.linearVelocity = new Vector3(
                horizontalVelocity.x,
                _rigidbody.linearVelocity.y,
                horizontalVelocity.z);
        }

        internal void ApplyExtraGravity()
        {
            _rigidbody.AddForce(Vector3.down * _data.ExtraGravity, ForceMode.Acceleration);
        }

        private void TickHitInvulnerability()
        {
            if (_hitInvulnerabilityTimer > 0f)
                _hitInvulnerabilityTimer -= Time.deltaTime;
        }

        internal void RotateToInput()
        {
            Vector2 input = _inputService.MoveDirection;

            if (input.sqrMagnitude <= 0.01f)
                return;

            Vector3 direction = new Vector3(input.x, 0f, input.y);
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                targetRotation, _data.RotationSpeed * Time.deltaTime);
        }

        internal void ChangeToMoveState()
        {
            EnsureStateMachine();

            if (!_isDead)
                _stateMachine.ChangeState(_moveState);
        }

        private void ChangeToHitState()
        {
            EnsureStateMachine();

            if (!_isDead)
                _stateMachine.ChangeState(_hitState);
        }

        private void Die()
        {
            if (_isDead)
                return;

            _health = 0;
            _isDead = true;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            Debug.Log("Player died.");
            HealthChanged?.Invoke(_health, _maxHealth);
            _visual?.PlayDeath();
            EnsureStateMachine();
            _stateMachine.ChangeState(_deadState);
            Died?.Invoke();
        }

        private void EnsureStateMachine()
        {
            if (_stateMachine != null)
                return;

            _stateMachine = new ActorStateMachine();
            _moveState = new PlayerMoveState(this);
            _hitState = new PlayerHitState(this);
            _deadState = new PlayerDeadState();
            _stateMachine.ChangeState(_moveState);
        }

        private void EnsurePrimitiveVisual()
        {
            _visual = GetComponentInChildren<PlayerPrimitiveVisual>();

            EnemySlingshot slingshot = GetComponent<EnemySlingshot>();

            if (_visual == null)
                _visual = PlayerPrimitiveVisual.Create(transform, _rigidbody, slingshot);
            else
                _visual.Initialize(_rigidbody, slingshot);

            if (slingshot != null && _visual.LassoOrigin != null)
                slingshot.SetLassoOrigin(_visual.LassoOrigin);
        }

        private void DisablePlaceholderRenderers()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();

            foreach (Renderer placeholderRenderer in renderers)
            {
                if (placeholderRenderer == null ||
                    placeholderRenderer.GetComponentInParent<PlayerPrimitiveVisual>() != null)
                    continue;

                placeholderRenderer.enabled = false;
            }
        }

        private void FlashHit()
        {
            if (_hitFlashMaterial == null || _renderers == null || _renderers.Length == 0)
                return;

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
                name = "Player Hit Flash"
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
                Renderer playerRenderer = _renderers[i];

                if (playerRenderer == null)
                    continue;

                Material[] flashMaterials = new Material[playerRenderer.sharedMaterials.Length];

                for (int j = 0; j < flashMaterials.Length; j++)
                    flashMaterials[j] = _hitFlashMaterial;

                playerRenderer.sharedMaterials = flashMaterials;
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
    }
}
