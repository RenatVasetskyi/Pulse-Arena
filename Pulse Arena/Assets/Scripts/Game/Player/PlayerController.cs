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
    ///     The player's thin state ROUTER. It owns the Unity lifecycle + the public API + events and wires the
    ///     collaborators (<see cref="IActorHealth" />, <see cref="IPlayerMovement" />, <see cref="IPlayerDash" />)
    ///     into a <see cref="PlayerContext" /> the states drive, but does NO per-frame work itself: Update /
    ///     FixedUpdate just tick the state machine, and pause is the <see cref="PlayerPausedState" /> (Enter
    ///     freezes, Exit restores). Behaviour lives in the states + the context; the controller only kicks off the
    ///     transitions the states can't see themselves (damage → Hit, death → Dead) and the freeze-on-death body.
    /// </summary>
    public class PlayerController : MonoBehaviour, IPausable
    {
        private const float MoveInputThreshold = 0.01f; // input magnitude² above which the player counts as running

        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private Renderer[] _renderers;
        [SerializeField] private TrailRenderer _dashTrail;
        [SerializeField] private EnemySlingshot _slingshot;
        [SerializeField] private MonoBehaviour _visualBehaviour;
        private readonly IPlayerDash _dash = new PlayerDash();
        private readonly IActorHealth _health = new ActorHealth();
        private readonly HitFlash _hitFlash = new();
        private readonly IPlayerMovement _movement = new PlayerMovement();
        private PlayerContext _context;
        private PlayerDashState _dashState;
        private PlayerData _data;
        private PlayerDeadState _deadState;
        private PlayerHitState _hitState;
        private PlayerIdleState _idleState;
        private IInputService _inputService;
        private bool _isDead;
        private IPauseService _pauseService;
        private PlayerPausedState _pausedState;
        private PlayerRunState _runState;
        private GameSettings _settings;
        private ActorStateMachine _stateMachine;
        private IPlayerVisual _visual;
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

        // Mechanical pause = overlay the PlayerPausedState on the running state (it caches + freezes the body and
        // gates the visual on Enter, restores on Exit); the suspended state keeps its exact frame + countdowns.
        public void Pause()
        {
            _stateMachine.Pause(_pausedState);
        }

        public void Resume()
        {
            _stateMachine.Resume();
        }

        private void Awake()
        {
            ActorPhysicsUtility.NormalizeCapsuleRoot(transform);
            InitializeVisual();
            _renderers = GetComponentsInChildren<Renderer>();

            _hitFlash.Initialize(_renderers, _settings.Feel.HitFlashMaterial, _settings.Feel.HitFlashDuration);
            _movement.Initialize(transform, _rigidbody, _data, _inputService);
            _dash.Initialize(transform, _rigidbody, _data, _inputService, _dashTrail);
            _health.Initialize(_data.MaxHealth, _data.HitInvulnerability);
            _health.Changed += OnHealthChanged;
            _pauseService?.Register(this);
            BuildStateMachine();
        }

        private void Update()
        {
            _stateMachine.Tick();
        }

        private void FixedUpdate()
        {
            _stateMachine.FixedTick();
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

        public bool IsDead => _isDead;

        public bool TakeDamage(int damage, Vector3 sourcePosition)
        {
            if (_isDead || !_health.TakeDamage(damage))
                return false;

            _movement.Stop(); // kill input momentum so a hit interrupts movement (no coasting through the hitstun)
            _movement.ApplyKnockback(sourcePosition, _data.HitKnockbackForce);
            _hitFlash.Play();

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

        // Called by PlayerContext.TryStartDash (from the grounded states) once it has confirmed the dash is ready
        // and pressed — grant the dodge i-frames + enter the dash state (which plays the clip).
        private void StartDash()
        {
            _dash.Begin();
            _health.GrantInvulnerability(_data.DashInvulnerability);
            ChangeToDashState();
            Dashed?.Invoke();
        }

        // --- state transitions ----------------------------------------------------------------
        private void ChangeToIdleState()
        {
            if (!_isDead)
                _stateMachine.ChangeState(_idleState);
        }

        private void ChangeToRunState()
        {
            if (!_isDead)
                _stateMachine.ChangeState(_runState);
        }

        private void ChangeToHitState()
        {
            if (!_isDead)
                _stateMachine.ChangeState(_hitState);
        }

        private void ChangeToDashState()
        {
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
            FreezeBodyOnDeath();
            ReleaseSlingshotGrab();
            _hitFlash.Restore();
            _stateMachine.ChangeState(_deadState);
            Died?.Invoke();
        }

        // Drop any enemy still held on the lasso when the player dies, so the rope doesn't keep spinning it.
        private void ReleaseSlingshotGrab()
        {
            _slingshot?.ForceRelease();
        }

        // Kill all momentum and freeze the body so the corpse doesn't slide or get shoved around while the death
        // animation plays. The visual topple lives on the child visual, so it is unaffected by isKinematic.
        private void FreezeBodyOnDeath()
        {
            if (_rigidbody == null)
                return;

            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.isKinematic = true;
        }

        // Built once at the end of Awake — after the collaborators + visual are initialized (BuildContext reads
        // them) and before any Update/FixedUpdate or transition runs, so the states never need a lazy guard.
        private void BuildStateMachine()
        {
            _stateMachine = new ActorStateMachine();
            _context = BuildContext();
            _idleState = new PlayerIdleState(_context);
            _runState = new PlayerRunState(_context);
            _hitState = new PlayerHitState(_context);
            _dashState = new PlayerDashState(_context);
            _deadState = new PlayerDeadState(_context);
            _pausedState = new PlayerPausedState(_context);
            _stateMachine.ChangeState(_idleState);
        }

        private PlayerContext BuildContext()
        {
            return new PlayerContext(
                _movement, _dash, _visual, _rigidbody, _health, _hitFlash, _data,
                HasMoveInput, HasFallenOff,
                ChangeToIdleState, ChangeToRunState, ChangeToDashState, ChangeToHitState,
                StartDash, Die);
        }

        private bool HasMoveInput()
        {
            return _inputService.MoveDirection.sqrMagnitude > MoveInputThreshold;
        }

        private bool HasFallenOff()
        {
            return transform.position.y < _settings.Feel.RingoutHeight;
        }

        // The visual + the slingshot are both baked on the player prefab and inspector-wired (the slingshot's own
        // LassoOrigin transform too), so this just hands the assigned refs to the visual — the throw arm-swing
        // subscribes to the slingshot's LassoThrown here. Nothing is fetched or assembled at runtime.
        private void InitializeVisual()
        {
            _visual = _visualBehaviour as IPlayerVisual;

            if (_visual == null)
            {
                Debug.LogError("Player visual (IPlayerVisual) is not assigned on the player prefab.", this);
                return;
            }

            _visual.Initialize(_rigidbody, _slingshot, _settings != null ? _settings.PlayerVisuals : null);
        }
    }
}
