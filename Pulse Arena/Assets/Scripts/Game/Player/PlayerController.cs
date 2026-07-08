using System;
using Architecture.Services.Interfaces;
using Data;
using Game.Combat;
using Game.Common;
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
        public event Action Dashed;

        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private Renderer[] _renderers;
        [SerializeField] private TrailRenderer _dashTrail;

        private PlayerPrimitiveVisual _visual;
        private IInputService _inputService;
        private GameSettings _settings;
        private PlayerData _data;
        private readonly HitFlash _hitFlash = new();
        private ActorStateMachine _stateMachine;
        private PlayerMoveState _moveState;
        private PlayerHitState _hitState;
        private PlayerDashState _dashState;
        private PlayerDeadState _deadState;
        private int _maxHealth;
        private int _health;
        private float _hitInvulnerabilityTimer;
        private float _dashCooldownTimer;
        private Vector3 _dashDirection;
        private bool _isDead;

        public int Health => _health;
        public int MaxHealth => _maxHealth;
        internal PlayerData Data => _data;

        /// <summary>Dash readiness for the HUD: 0 just after a dash, filling to 1 when the cooldown is up.</summary>
        public float DashCharge01 => _data != null && _data.DashCooldown > 0f
            ? 1f - Mathf.Clamp01(_dashCooldownTimer / _data.DashCooldown)
            : 1f;

        [Inject]
        public void Construct(IInputService inputService, GameSettings gameSettings)
        {
            _inputService = inputService;
            _settings = gameSettings;
            _data = gameSettings.PlayerData;
            _maxHealth = Mathf.Max(1, _data.MaxHealth);
            _health = _maxHealth;
        }

        private void Awake()
        {
            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody>();

            ActorPhysicsUtility.NormalizeCapsuleRoot(transform);
            EnsurePrimitiveVisual();
            _renderers = GetComponentsInChildren<Renderer>();

            _hitFlash.Initialize(_renderers, _settings.Feel.HitFlashColor, _settings.Feel.HitFlashDuration);
            SetDashTrail(false);
        }

        private void Update()
        {
            EnsureStateMachine();
            TickHitInvulnerability();
            TickDashCooldown();
            TryDash();
            TickRingout();
            _hitFlash.Tick(Time.deltaTime);
            _stateMachine.Tick();
        }

        private void TickRingout()
        {
            if (!_isDead && transform.position.y < _settings.Feel.RingoutHeight)
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
            _hitFlash.Play();
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

            // The Rigidbody leaves Y rotation free (so RotateToInput can turn the player via the
            // transform), so a collision can impart spin. Facing is fully code-driven, so kill any
            // physics-induced angular velocity every step to stop the player pinwheeling when idle.
            if (!_isDead && _rigidbody != null)
                _rigidbody.angularVelocity = Vector3.zero;
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
            ActorPhysicsUtility.ApplyExtraGravity(_rigidbody, _data.ExtraGravity);
        }

        private void TickHitInvulnerability()
        {
            if (_hitInvulnerabilityTimer > 0f)
                _hitInvulnerabilityTimer -= Time.deltaTime;
        }

        private void TickDashCooldown()
        {
            if (_dashCooldownTimer > 0f)
                _dashCooldownTimer -= Time.deltaTime;
        }

        private void TryDash()
        {
            if (_isDead || _dashCooldownTimer > 0f)
                return;

            if (_stateMachine.ActiveState == _dashState)
                return;

            if (!_inputService.IsDashPressedThisFrame)
                return;

            StartDash();
        }

        private void StartDash()
        {
            Vector2 input = _inputService.MoveDirection;
            Vector3 direction = new Vector3(input.x, 0f, input.y);

            if (direction.sqrMagnitude < 0.01f)
                direction = transform.forward;

            _dashDirection = direction.normalized;
            _dashCooldownTimer = _data.DashCooldown;
            _hitInvulnerabilityTimer = Mathf.Max(_hitInvulnerabilityTimer, _data.DashInvulnerability);

            ChangeToDashState();
            Dashed?.Invoke();
        }

        internal void ApplyDashVelocity()
        {
            _rigidbody.linearVelocity = new Vector3(
                _dashDirection.x * _data.DashSpeed,
                _rigidbody.linearVelocity.y,
                _dashDirection.z * _data.DashSpeed);
        }

        internal void FaceDashDirection()
        {
            if (_dashDirection.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(_dashDirection);
        }

        internal void SetDashTrail(bool active)
        {
            if (_dashTrail == null)
                return;

            if (active)
                _dashTrail.Clear();

            _dashTrail.emitting = active;
        }

        private void ChangeToDashState()
        {
            EnsureStateMachine();

            if (!_isDead)
                _stateMachine.ChangeState(_dashState);
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
            _hitFlash.Restore();
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
            _dashState = new PlayerDashState(this);
            _deadState = new PlayerDeadState();
            _stateMachine.ChangeState(_moveState);
        }

        private void EnsurePrimitiveVisual()
        {
            _visual = GetComponentInChildren<PlayerPrimitiveVisual>();

            if (_visual == null)
            {
                Debug.LogError("PlayerPrimitiveVisual is missing on the player prefab.", this);
                return;
            }

            EnemySlingshot slingshot = GetComponent<EnemySlingshot>();
            _visual.Initialize(_rigidbody, slingshot, _settings.PlayerVisuals);

            if (slingshot != null && _visual.LassoOrigin != null)
                slingshot.SetLassoOrigin(_visual.LassoOrigin);
        }
    }
}
