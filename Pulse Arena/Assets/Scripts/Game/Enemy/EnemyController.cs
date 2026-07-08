using System.Collections;
using Architecture.Services.Interfaces;
using Data;
using System;
using Game.Common;
using Game.Common.StateMachine;
using Game.Enemy.States;
using Game.Player;
using Game.Visuals;
using UnityEngine;
using UnityEngine.AI;
using Zenject;

namespace Game.Enemy
{
    /// <summary>
    /// The enemy's thin state router. It wires the collaborators (movement, impact, timers, ground
    /// recovery, collisions, ringout, health-bar presenter) together behind a lean <see cref="EnemyContext"/>,
    /// owns the pool lifecycle + the public API (Knockback / Grab / Launch / …), and forwards Unity's
    /// FixedUpdate + collision callbacks into the state machine / collision handler. All per-frame logic
    /// lives in the seven state classes; the shared flags they flip live on the context (single source of
    /// truth) and the controller reads/writes them through it. The death-return coroutine stays here —
    /// coroutines need the MonoBehaviour.
    /// </summary>
    public class EnemyController : MonoBehaviour
    {
        public event Action<EnemyController> Destroyed;
        public event Action<int, int> HealthChanged;

        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private NavMeshAgent _agent;

        private GameSettings _settings;
        private EnemyData _data;
        private IScoreService _scoreService;
        private IAudioService _audioService;
        private IComboService _comboService;
        private Transform _target;
        private PlayerController _playerTarget;
        private readonly HitFlash _hitFlash = new();
        private readonly EnemyMovement _movement = new();
        private CapsuleCollider _capsule;
        private readonly EnemyHealthBarPresenter _healthBar = new();
        private IScorePopupService _scorePopups;
        private EnemyPrimitiveVisual _visual;
        private readonly EnemyImpact _impact = new();
        private readonly EnemyTimers _timers = new();
        private readonly GroundRecoveryController _groundRecovery = new();
        private readonly EnemyCollisionHandler _collisions = new();
        private readonly RingoutHandler _ringout = new();
        private Action<EnemyController> _releaseToPool;
        private EnemyTypeData _typeData = EnemyTypeData.Default;
        private ActorStateMachine _stateMachine;
        private EnemyContext _context;
        private EnemyChaseState _chaseState;
        private EnemyGrabbedState _grabbedState;
        private EnemyStasisState _stasisState;
        private EnemyKnockbackState _knockbackState;
        private EnemyGroundRecoveryState _groundRecoveryState;
        private EnemyDeadState _deadState;
        private EnemyRingoutState _ringoutState;
        private Coroutine _deathRoutine;
        private bool _isRingout;
        private readonly EnemyHealth _health = new();
        private bool _isDead;
        private bool _isInPool;

        public bool IsGrabbed => _context != null && _context.IsGrabbed;

        public int Health => _health.Current;
        public int MaxHealth => _health.Max;
        public EnemyTypeData TypeData => _typeData;

        /// <summary>The primitive visual, exposed so the collision handler can play the ground-bounce squash.</summary>
        public EnemyPrimitiveVisual Visual => _visual;

        [Inject]
        public void Construct(GameSettings gameSettings, IScoreService scoreService, IAudioService audioService,
            IComboService comboService, IScorePopupService scorePopups)
        {
            _settings = gameSettings;
            _data = gameSettings.EnemyData;
            _scoreService = scoreService;
            _audioService = audioService;
            _comboService = comboService;
            _scorePopups = scorePopups;
            _health.Changed += OnHealthChanged;
            _health.Died += OnHealthDepleted;
            _health.Reset(_data.MaxHealth);
            _movement.ConfigureAgent();
            _ringout.Initialize(transform, _data, _settings.Vfx, _scoreService, _comboService,
                _scorePopups, _audioService, () => _typeData);
            _healthBar.Create(transform, _settings.Prefabs.WorldHealthBarPrefab, _health.Max, _data.HealthBarHeight);
        }

        public void SetPoolReturnAction(Action<EnemyController> releaseToPool)
        {
            _releaseToPool = releaseToPool;
        }

        public void Initialize(Transform target, EnemyTypeData typeData = null)
        {
            _typeData = typeData ?? EnemyTypeData.Default;
            ResetForSpawn();
            _target = target;
            _playerTarget = target.GetComponentInParent<PlayerController>();
            _movement.ConfigureAgent();
            _visual?.ApplyTypeStyle(_typeData);
            ChangeToChaseState();
        }

        public void PrepareForPool()
        {
            _hitFlash.Restore();

            if (_deathRoutine != null)
            {
                StopCoroutine(_deathRoutine);
                _deathRoutine = null;
            }

            _stateMachine?.Clear();
            _movement.DisableAgent();
            _target = null;
            _playerTarget = null;
            _context.IsGrabbed = false;
            _context.IsImpactProjectile = false;
            _context.NeedsGroundRecovery = false;
            _isRingout = false;
            _timers.Knockback.Clear();
            _timers.Stasis.Clear();
            _impact.Clear();
            transform.localScale = Vector3.one;

            if (_rigidbody == null)
                return;

            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.isKinematic = false;
        }

        // --- public API ------------------------------------------------------------------------
        // Each only arms the trigger (timers + flags + force) then transitions to the owning state; the
        // rigidbody setup lives in the state Enter. Order preserved: the AddForce / velocity assignment
        // runs AFTER ChangeToXState so the state Enter has already woken the body first.

        public void Knockback(Vector3 force)
        {
            _context.IsImpactProjectile = false;
            _impact.ResetSweepOrigin();
            _timers.Stasis.Clear();
            _timers.PhysicsRecoveryElapsed = 0f;
            _timers.GroundContact.Clear();
            _timers.Knockback.Set(_data.KnockbackDuration);
            ChangeToKnockbackState();
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.AddForce(force, ForceMode.VelocityChange);
        }

        public void Grab()
        {
            if (_isDead)
                return;

            _context.IsImpactProjectile = false;
            _impact.ResetSweepOrigin();
            _timers.Stasis.Clear();
            _timers.PhysicsRecoveryElapsed = 0f;
            _timers.Knockback.Clear();
            ChangeToGrabbedState();
            _rigidbody.linearVelocity = Vector3.zero;
        }

        public void MoveGrabbed(Vector3 targetPosition, float followSpeed)
        {
            if (_isDead || !_context.IsGrabbed)
                return;

            Vector3 velocity = (targetPosition - transform.position) * followSpeed;
            _rigidbody.linearVelocity = velocity;
        }

        public void Launch(Vector3 velocity, float duration)
        {
            if (_isDead)
                return;

            _context.IsImpactProjectile = true;
            _impact.ResetSweepOrigin();
            _impact.Clear();
            _collisions.ResetGroundBounce();
            _timers.GroundBounceCooldown.Clear();
            _timers.GroundContact.Clear();
            _timers.PhysicsRecoveryElapsed = 0f;
            _timers.Stasis.Clear();
            _timers.Knockback.Set(duration);
            ChangeToKnockbackState();
            _rigidbody.useGravity = true;
            _rigidbody.isKinematic = false;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.linearVelocity = velocity;
            _rigidbody.WakeUp();
        }

        public bool TakeDamage(int damage)
        {
            if (_isDead)
                return false;

            _visual?.PlayHit();
            _audioService?.PlaySfx(GameSfx.Impact);
            _health.TakeDamage(damage);   // fires Changed (bar/event); on 0 fires Died (score + dead state)

            if (_isDead)
                return true;

            _hitFlash.Play();
            return false;
        }

        public bool Kill()
        {
            return Die();
        }

        /// <summary>Rings the enemy out on the spot — used by arena pits it gets flung into.</summary>
        public void FallIntoPit()
        {
            StartRingout();
        }

        public bool TryGetRopeBounds(out Bounds bounds)
        {
            if (_visual != null && _visual.TryGetRopeBounds(out bounds))
                return true;

            if (_capsule != null)
            {
                bounds = _capsule.bounds;
                return true;
            }

            bounds = default;
            return false;
        }

        private bool Die()
        {
            if (_isDead)
                return false;

            _health.Kill();   // fires Changed(0) + Died → OnHealthDepleted
            return true;
        }

        // --- health event wiring ---------------------------------------------------------------

        private void OnHealthChanged(int current, int max)
        {
            HealthChanged?.Invoke(current, max);
            _healthBar.SetHealth(current, max);
        }

        private void OnHealthDepleted()
        {
            if (_isDead)
                return;

            _isDead = true;
            _ringout.AwardKill(out _);
            ChangeToDeadState();
        }

        // --- Unity lifecycle -------------------------------------------------------------------

        private void Awake()
        {
            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody>();

            if (_agent == null)
                _agent = GetComponent<NavMeshAgent>();

            if (_agent == null)
                _agent = gameObject.AddComponent<NavMeshAgent>();

            _agent.enabled = false;
            _movement.Initialize(transform, _rigidbody, _agent, _data, GetMoveSpeed);
            _impact.Initialize(this, transform, _rigidbody, _data, () => _typeData);
            _groundRecovery.Initialize(transform, _rigidbody, _data, _timers);
            _collisions.Initialize(this, transform, _rigidbody, _data, _impact, _timers, _groundRecovery);

            _capsule = ActorPhysicsUtility.NormalizeCapsuleRoot(transform);
            EnsurePrimitiveVisual();
            _hitFlash.Initialize(GetComponentsInChildren<Renderer>(), _settings.Feel.HitFlashColor, _settings.Feel.HitFlashDuration);

            _context = new EnemyContext(
                _rigidbody, transform, _data, _movement, _visual, _timers, _groundRecovery, _impact, _collisions,
                () => _target, () => _playerTarget, () => _isDead, () => _typeData,
                ChangeToChaseState, ChangeToGroundRecoveryState, ReturnToPool, StartDeathReturn,
                StopForDeath, ResolveRingout);
        }

        private void Update()
        {
            _hitFlash.Tick(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            EnsureStateMachine();

            if (!_isDead && !_isInPool && transform.position.y < _settings.Feel.RingoutHeight)
                StartRingout();

            if (_isDead)
            {
                _stateMachine.FixedTick();
                return;
            }

            TickTimers();
            _stateMachine.FixedTick();
        }

        private void OnCollisionEnter(Collision collision)
        {
            _collisions.OnCollisionEnter(collision, _context.IsImpactProjectile, _isDead);
        }

        private void OnCollisionStay(Collision collision)
        {
            _collisions.OnCollisionStay(collision);
        }

        private void OnDestroy()
        {
            if (!_isInPool)
                Destroyed?.Invoke(this);
        }

        // --- pool lifecycle --------------------------------------------------------------------

        private void ResetForSpawn()
        {
            EnsureStateMachine();

            _hitFlash.Restore();

            if (_deathRoutine != null)
            {
                StopCoroutine(_deathRoutine);
                _deathRoutine = null;
            }

            _stateMachine.Clear();
            _movement.DisableAgent();

            _isInPool = false;
            _isDead = false;
            _context.IsGrabbed = false;
            _context.IsImpactProjectile = false;
            _context.NeedsGroundRecovery = false;
            _ringout.ResetForSpawn();
            _timers.ResetAll();
            _isRingout = false;
            _collisions.ResetForSpawn();
            _impact.Clear();
            transform.localScale = Vector3.one;

            _health.Reset(GetTypeAdjustedMaxHealth());   // fires Changed → HealthChanged + health bar
            _visual?.ResetState();

            if (_rigidbody == null)
                return;

            _rigidbody.useGravity = true;
            _rigidbody.isKinematic = false;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            // Restore the upright constraints the ringout tumble removed.
            _rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            transform.rotation = Quaternion.identity;
            _rigidbody.WakeUp();
        }

        private void ReturnToPool()
        {
            if (_isInPool)
                return;

            _isInPool = true;
            Destroyed?.Invoke(this);

            if (_releaseToPool != null)
            {
                _releaseToPool(this);
                return;
            }

            Destroy(gameObject);
        }

        // The death coroutine MUST stay on the MonoBehaviour; EnemyDeadState triggers it via
        // EnemyContext.StartDeathReturn → this one-liner.
        private void StartDeathReturn()
        {
            if (_deathRoutine == null)
                _deathRoutine = StartCoroutine(ReturnAfterDeath());
        }

        private IEnumerator ReturnAfterDeath()
        {
            yield return new WaitForSeconds(_settings.EnemyVisuals.DeathPopDuration);
            _deathRoutine = null;
            ReturnToPool();
        }

        // --- state machine + transitions -------------------------------------------------------

        private void EnsureStateMachine()
        {
            if (_stateMachine != null)
                return;

            _stateMachine = new ActorStateMachine();
            _chaseState = new EnemyChaseState(_context);
            _grabbedState = new EnemyGrabbedState(_context);
            _stasisState = new EnemyStasisState(_context);
            _groundRecoveryState = new EnemyGroundRecoveryState(_context);
            _knockbackState = new EnemyKnockbackState(_context, _groundRecoveryState);
            _deadState = new EnemyDeadState(_context);
            _ringoutState = new EnemyRingoutState(_context);
        }

        internal void ChangeToChaseState()
        {
            EnsureStateMachine();

            if (!_isDead)
                _stateMachine.ChangeState(_chaseState);
        }

        private void ChangeToGrabbedState()
        {
            EnsureStateMachine();

            if (!_isDead)
                _stateMachine.ChangeState(_grabbedState);
        }

        internal void ChangeToStasisState()
        {
            EnsureStateMachine();

            if (!_isDead)
                _stateMachine.ChangeState(_stasisState);
        }

        internal void ChangeToKnockbackState()
        {
            EnsureStateMachine();

            if (!_isDead)
                _stateMachine.ChangeState(_knockbackState);
        }

        internal void ChangeToGroundRecoveryState()
        {
            EnsureStateMachine();

            if (!_isDead)
                _stateMachine.ChangeState(_groundRecoveryState);
        }

        private void ChangeToDeadState()
        {
            EnsureStateMachine();
            _stateMachine.ChangeState(_deadState);
        }

        private void StartRingout()
        {
            if (_isRingout || _isDead || _isInPool)
                return;

            _isRingout = true;
            EnsureStateMachine();
            _stateMachine.ChangeState(_ringoutState);
        }

        // --- state-owned side effects the controller still hosts -------------------------------
        // Reached through EnemyContext callbacks: they touch controller-private state (dead flag,
        // health-bar presenter, RingoutHandler). The physics/flag/timer bodies live in the states.

        // The old StopForDeath body (EnemyDeadState.Enter → EnemyContext.StopForDeath).
        private void StopForDeath()
        {
            _movement.DisableAgent();
            _context.IsGrabbed = false;
            _context.IsImpactProjectile = false;
            _context.NeedsGroundRecovery = false;
            _timers.Knockback.Clear();
            _timers.Stasis.Clear();

            if (_rigidbody == null)
                return;

            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.isKinematic = true;
        }

        // The controller-coupled slice of the old EnterRingoutState body, in original order: mark dead,
        // zero the health bar, run the RingoutHandler (EnemyRingoutState.Enter → EnemyContext.ResolveRingout).
        private void ResolveRingout()
        {
            _isDead = true;
            _healthBar.SetHealth(0, _health.Max);
            _ringout.ResolveRingout();
        }

        // --- shared timer tick + small helpers -------------------------------------------------

        // Shared cooldowns that tick in EVERY non-dead state (EnemyTimers.TickFixed — includes Stasis, as
        // the original did). The knockback-timer decrement + its expiry side effect live in
        // EnemyKnockbackState, not here. The impact hit-set is a dictionary the controller still ticks.
        private void TickTimers()
        {
            _timers.TickFixed(Time.fixedDeltaTime);
            _impact.Tick(Time.fixedDeltaTime);
        }

        private void EnsurePrimitiveVisual()
        {
            _visual = GetComponentInChildren<EnemyPrimitiveVisual>();

            if (_visual == null)
            {
                Debug.LogError("EnemyPrimitiveVisual is missing on the enemy prefab.", this);
                return;
            }

            _visual.Initialize(_rigidbody, _settings.EnemyVisuals);
        }

        private int GetTypeAdjustedMaxHealth()
        {
            return Mathf.Max(1, Mathf.RoundToInt(_data.MaxHealth * _typeData.HealthMultiplier));
        }

        private float GetMoveSpeed()
        {
            return _data.MoveSpeed * _typeData.MoveSpeedMultiplier;
        }
    }
}
