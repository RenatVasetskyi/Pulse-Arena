using System;
using Architecture.Services.Interfaces;
using Data;
using Game.Combat;
using Game.Common;
using Game.Common.Interfaces;
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
    ///     and wires three focused collaborators: <see cref="IActorHealth" /> (HP + i-frames),
    ///     <see cref="IPlayerMovement" /> (Rigidbody locomotion + knockback) and <see cref="IPlayerDash" /> (the
    ///     dash/dodge). The per-frame work lives in the states, which reach the collaborators through this
    ///     controller's thin delegating methods (MoveByInput / ApplyDashVelocity / …).
    /// </summary>
    public class PlayerController : MonoBehaviour, IPausable
    {
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private Renderer[] _renderers;
        [SerializeField] private TrailRenderer _dashTrail;
        private readonly IPlayerDash _dash = new PlayerDash();
        private readonly IActorHealth _health = new ActorHealth();
        private readonly HitFlash _hitFlash = new();
        private readonly IPlayerMovement _movement = new PlayerMovement();
        private Vector3 _cachedAngularVelocity;
        private Vector3 _cachedLinearVelocity;
        private PlayerDashState _dashState;
        private PlayerData _data;
        private PlayerDeadState _deadState;
        private PlayerHitState _hitState;
        private IInputService _inputService;
        private bool _isDead;
        private PlayerMoveState _moveState;
        private bool _paused;
        private IPauseService _pauseService;
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

        [Inject]
        public void Construct(IInputService inputService, GameSettings gameSettings, IPauseService pauseService)
        {
            _inputService = inputService;
            _settings = gameSettings;
            _data = gameSettings.PlayerData;
            _pauseService = pauseService;
        }

        /// <summary>
        ///     Mechanical pause: freeze the body (cache velocities, go kinematic so PhysX stops integrating
        ///     gravity/velocity) and stop the procedural visual. The FSM + all delta-timers freeze for free
        ///     because Update/FixedUpdate early-return, so Resume continues the exact same state + countdowns.
        /// </summary>
        public void Pause()
        {
            if (_paused)
                return;

            _paused = true;

            if (_rigidbody != null)
            {
                _cachedLinearVelocity = _rigidbody.linearVelocity;
                _cachedAngularVelocity = _rigidbody.angularVelocity;
                _rigidbody.isKinematic = true;
            }

            _visual?.SetPaused(true);
        }

        public void Resume()
        {
            if (!_paused)
                return;

            _paused = false;

            // Restore the body BEFORE un-gating the visual — the visual's move-blend reads linearVelocity.
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = false;
                _rigidbody.linearVelocity = _cachedLinearVelocity;
                _rigidbody.angularVelocity = _cachedAngularVelocity;
                _rigidbody.WakeUp();
            }

            _visual?.SetPaused(false);
        }

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
            _pauseService?.Register(this);
        }

        private void Update()
        {
            EnsureStateMachine();

            if (_paused)
                return;

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

            if (_paused)
                return;

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
            _pauseService?.Unregister(this);
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