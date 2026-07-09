using System;
using Architecture.Services.Interfaces;
using Data;
using Game.Combat;
using Game.Common;
using Game.Common.StateMachine;
using Game.Player.Interfaces;
using Game.Player.States;
using Game.Visuals;
using UnityEngine;
using Zenject;

namespace Game.Player
{
    /// <summary>
    ///     The player's thin orchestrator. It owns the state machine + Unity lifecycle + the public API + events,
    ///     and wires three focused collaborators: <see cref="IPlayerHealth" /> (HP + i-frames),
    ///     <see cref="IPlayerMovement" /> (Rigidbody locomotion + knockback) and <see cref="IPlayerDash" /> (the
    ///     dash/dodge). The per-frame work lives in the states, which reach the collaborators through this
    ///     controller's thin delegating methods (MoveByInput / ApplyDashVelocity / …).
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private Renderer[] _renderers;
        [SerializeField] private TrailRenderer _dashTrail;
        private readonly IPlayerDash _dash = new PlayerDash();
        private readonly IPlayerHealth _health = new PlayerHealth();
        private readonly HitFlash _hitFlash = new();
        private readonly IPlayerMovement _movement = new PlayerMovement();
        private PlayerDashState _dashState;
        private PlayerData _data;
        private PlayerDeadState _deadState;
        private PlayerHitState _hitState;
        private IInputService _inputService;
        private bool _isDead;
        private PlayerMoveState _moveState;
        private GameSettings _settings;
        private ActorStateMachine _stateMachine;

        private PlayerPrimitiveVisual _visual;
        public event Action Dashed;
        public event Action Died;
        public event Action<int, int> HealthChanged;

        public int Health => _health.Current;
        public int MaxHealth => _health.Max;
        internal PlayerData Data => _data;

        /// <summary>Dash readiness for the HUD: 0 just after a dash, filling to 1 when the cooldown is up.</summary>
        public float DashCharge01 => _dash.Charge01;

        private void Awake()
        {
            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody>();

            ActorPhysicsUtility.NormalizeCapsuleRoot(transform);
            EnsurePrimitiveVisual();
            _renderers = GetComponentsInChildren<Renderer>();

            _hitFlash.Initialize(_renderers, _settings.Feel.HitFlashColor, _settings.Feel.HitFlashDuration);
            _movement.Initialize(transform, _rigidbody, _data, _inputService);
            _dash.Initialize(transform, _rigidbody, _data, _inputService, _dashTrail);
            _health.Initialize(_data.MaxHealth, _data.HitInvulnerability);
            _health.Changed += OnHealthChanged;
        }

        private void Update()
        {
            EnsureStateMachine();
            _health.Tick(Time.deltaTime);
            _dash.Tick(Time.deltaTime);
            TryDash();
            TickRingout();
            _hitFlash.Tick(Time.deltaTime);
            _stateMachine.Tick();
        }

        private void FixedUpdate()
        {
            EnsureStateMachine();
            _stateMachine.FixedTick();

            // The Rigidbody leaves Y rotation free (so RotateToInput can turn the player via the
            // transform), so a collision can impart spin. Facing is fully code-driven, so kill any
            // physics-induced angular velocity every step to stop the player pinwheeling when idle.
            if (!_isDead)
                _movement.KillAngularVelocity();
        }

        private void OnDestroy()
        {
            _health.Changed -= OnHealthChanged;
        }

        [Inject]
        public void Construct(IInputService inputService, GameSettings gameSettings)
        {
            _inputService = inputService;
            _settings = gameSettings;
            _data = gameSettings.PlayerData;
        }

        public void Kill()
        {
            Die();
        }

        public bool TakeDamage(int damage, Vector3 sourcePosition)
        {
            if (!_health.TakeDamage(damage))
                return false;

            _movement.ApplyKnockback(sourcePosition, _data.HitKnockbackForce);
            Debug.Log($"Player hit. Health: {_health.Current}");
            _hitFlash.Play();
            _visual?.PlayHit();

            if (_health.IsDepleted)
                Die();
            else
                ChangeToHitState();

            return true;
        }

        public bool TryHeal(int amount)
        {
            return _health.TryHeal(amount);
        }

        private void OnHealthChanged(int current, int max)
        {
            HealthChanged?.Invoke(current, max);
        }

        private void TickRingout()
        {
            if (!_isDead && transform.position.y < _settings.Feel.RingoutHeight)
                Die();
        }

        // --- thin delegates the states drive --------------------------------------------------
        internal void MoveByInput() => _movement.MoveByInput();
        internal void RotateToInput() => _movement.RotateToInput();
        internal void ApplyExtraGravity() => _movement.ApplyExtraGravity();
        internal void ApplyDashVelocity() => _dash.ApplyDashVelocity();
        internal void FaceDashDirection() => _dash.FaceDashDirection();
        internal void SetDashTrail(bool active) => _dash.SetTrail(active);

        private void TryDash()
        {
            if (_isDead || !_dash.IsReady)
                return;

            if (_stateMachine.ActiveState == _dashState)
                return;

            if (!_dash.WantsDash())
                return;

            StartDash();
        }

        private void StartDash()
        {
            _dash.Begin();
            _health.GrantInvulnerability(_data.DashInvulnerability);
            ChangeToDashState();
            Dashed?.Invoke();
        }

        // --- state transitions ----------------------------------------------------------------
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

        private void ChangeToDashState()
        {
            EnsureStateMachine();

            if (!_isDead)
                _stateMachine.ChangeState(_dashState);
        }

        private void Die()
        {
            if (_isDead)
                return;

            _isDead = true;
            _health.Kill();
            _movement.Stop();
            _hitFlash.Restore();
            Debug.Log("Player died.");
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