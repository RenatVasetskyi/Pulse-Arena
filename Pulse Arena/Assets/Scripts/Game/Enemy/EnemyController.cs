using System.Collections;
using Architecture.Services.Interfaces;
using Data;
using System;
using Game.Common;
using Game.Common.StateMachine;
using Game.Enemy.Interfaces;
using Game.Enemy.States;
using Game.Player;
using Game.Visuals;
using UnityEngine;
using UnityEngine.AI;
using Zenject;

namespace Game.Enemy
{
    /// <summary>
    ///     The enemy's thin state router. It wires the collaborators (movement, impact, timers, ground
    ///     recovery, collisions, ringout, health-bar presenter) together behind a lean <see cref="EnemyContext" />,
    ///     owns the pool lifecycle + the public API (Knockback / Grab / Launch / …), and forwards Unity's
    ///     FixedUpdate + collision callbacks into the state machine / collision handler. All per-frame logic
    ///     lives in the seven state classes; the shared flags they flip live on the context (single source of
    ///     truth) and the controller reads/writes them through it. The death-return coroutine stays here —
    ///     coroutines need the MonoBehaviour. Each lifecycle step (spawn reset / pool teardown / wiring) is a
    ///     small single-purpose method the coordinator calls in order.
    /// </summary>
    public class EnemyController : MonoBehaviour, IPausable
    {
        private const float EnemyBodyScale = 0.7f; // shrink all enemies a bit (scales visual + collider together)

        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private NavMeshAgent _agent;
        private Vector3 _cachedAngularVelocity;
        private bool _cachedIsKinematic;
        private Vector3 _cachedLinearVelocity;
        private bool _cachedUseGravity;
        private bool _deathReturnPending;
        private bool _paused;
        private IPauseService _pauseService;
        private readonly EnemyCollisionHandler _collisions = new();
        private readonly GroundRecoveryController _groundRecovery = new();
        private readonly ActorHealth _health = new();
        private readonly EnemyHealthBarPresenter _healthBar = new();
        private readonly HitFlash _hitFlash = new();
        private readonly EnemyImpact _impact = new();
        private readonly EnemyMovement _movement = new();
        private readonly RingoutHandler _ringout = new();
        private readonly EnemyTimers _timers = new();
        private IAudioService _audioService;
        private CapsuleCollider _capsule;
        private EnemyChaseState _chaseState;
        private IComboService _comboService;
        private EnemyContext _context;
        private EnemyData _data;
        private EnemyDeadState _deadState;
        private Coroutine _deathRoutine;
        private EnemyGrabbedState _grabbedState;
        private EnemyGroundRecoveryState _groundRecoveryState;
        private bool _isDead;
        private bool _isInPool;
        private bool _isRingout;
        private EnemyKnockbackState _knockbackState;
        private PlayerController _playerTarget;
        private IEnemyRegistry _registry;
        private Action<EnemyController> _releaseToPool;
        private EnemyRingoutState _ringoutState;
        private IScorePopupService _scorePopups;
        private IScoreService _scoreService;

        private GameSettings _settings;
        private EnemyStasisState _stasisState;
        private ActorStateMachine _stateMachine;
        private Transform _target;
        private EnemyTypeData _typeData = EnemyTypeData.Default;
        private EnemyPrimitiveVisual _visual;
        public event Action<EnemyController> Destroyed;
        public event Action<int, int> HealthChanged;

        public bool IsGrabbed => _context != null && _context.IsGrabbed;

        /// <summary>
        ///     Whether the lasso / hook-marker may target this enemy. Excludes dead, ringing-out and pooled
        ///     enemies. Crucial: a rung-out enemy keeps Health &gt; 0 (only <c>_isDead</c> is set), so a plain
        ///     Health check would still let the lasso fly at it while it tumbles off the arena edge.
        /// </summary>
        public bool IsTargetable => !_isDead && !_isInPool && !IsGrabbed && _health.Current > 0;

        public int Health => _health.Current;
        public int MaxHealth => _health.Max;
        public EnemyTypeData TypeData => _typeData;

        /// <summary>The primitive visual, exposed so the collision handler can play the ground-bounce squash.</summary>
        public EnemyPrimitiveVisual Visual => _visual;

        [Inject]
        public void Construct(GameSettings gameSettings, IScoreService scoreService, IAudioService audioService,
            IComboService comboService, IScorePopupService scorePopups, IEnemyRegistry enemyRegistry,
            IPauseService pauseService)
        {
            _settings = gameSettings;
            _data = gameSettings.EnemyData;
            _scoreService = scoreService;
            _audioService = audioService;
            _comboService = comboService;
            _scorePopups = scorePopups;
            _registry = enemyRegistry;
            _pauseService = pauseService;

            WireHealth();
            _movement.ConfigureAgent();
            InitializeRingout();
            CreateHealthBar();
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

        // --- Unity lifecycle -------------------------------------------------------------------

        private void Awake()
        {
            ResolveComponents();
            InitializeCollaborators();
            SetupVisualsAndFlash();
            BuildContext();
        }

        private void Update()
        {
            if (_paused)
                return;

            _hitFlash.Tick(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            EnsureStateMachine();

            if (_paused)
                return;

            if (!_isDead && !_isInPool)
                HandleOutOfBounds();

            if (_isDead)
            {
                _stateMachine.FixedTick();
                return;
            }

            TickTimers();
            _stateMachine.FixedTick();
        }

        private void OnDestroy()
        {
            // Covers the non-pooled Destroy path (ReturnToPool with no release action) + scene teardown,
            // neither of which runs PrepareForPool, so the registry never keeps a dangling entry.
            _registry?.Unregister(_rigidbody);
            _pauseService?.Unregister(this);

            if (!_isInPool)
                Destroyed?.Invoke(this);
        }

        /// <summary>
        ///     Mechanical pause: freeze the body (cache velocity + gravity/kinematic, then zero + kinematic so
        ///     PhysX stops), stop the agent, freeze the visual + its spawn tween, and suspend the death-return
        ///     coroutine (WaitForSeconds keeps counting under a mechanical pause). FSM + timers freeze because
        ///     FixedUpdate early-returns, so Resume continues the exact state at the exact countdowns.
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
                _cachedIsKinematic = _rigidbody.isKinematic;
                _cachedUseGravity = _rigidbody.useGravity;
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
                _rigidbody.isKinematic = true;
            }

            _movement.Pause();
            _visual?.SetPaused(true);
            _ringout.PauseEffect();

            _deathReturnPending = _deathRoutine != null;
            StopDeathReturn();
        }

        public void Resume()
        {
            if (!_paused)
                return;

            _paused = false;

            if (_deathReturnPending)
            {
                _deathReturnPending = false;
                StartDeathReturn();
            }

            _visual?.SetPaused(false);
            _ringout.ResumeEffect();
            _movement.Resume();

            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = _cachedIsKinematic;
                _rigidbody.useGravity = _cachedUseGravity;
                _rigidbody.linearVelocity = _cachedLinearVelocity;
                _rigidbody.angularVelocity = _cachedAngularVelocity;

                if (!_rigidbody.isKinematic)
                    _rigidbody.WakeUp();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            _collisions.OnCollisionEnter(collision, _context.IsImpactProjectile, _isDead);
        }

        private void OnCollisionStay(Collision collision)
        {
            _collisions.OnCollisionStay(collision);
        }

        /// <summary>Rings the enemy out on the spot — used by arena pits it gets flung into.</summary>
        public void FallIntoPit()
        {
            StartRingout();
        }

        public void Grab()
        {
            if (_isDead)
                return;

            ResetImpulseState();
            _timers.Knockback.Clear();
            ChangeToGrabbedState();
            _rigidbody.linearVelocity = Vector3.zero;
        }

        public bool Kill()
        {
            return Die();
        }

        // --- public API ------------------------------------------------------------------------
        // Each only arms the trigger (timers + flags + force) then transitions to the owning state; the
        // rigidbody setup lives in the state Enter. Order preserved: the AddForce / velocity assignment
        // runs AFTER ChangeToXState so the state Enter has already woken the body first.

        public void Knockback(Vector3 force)
        {
            ResetImpulseState();
            _timers.GroundContact.Clear();
            _timers.Knockback.Set(_data.KnockbackDuration);
            ChangeToKnockbackState();
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.AddForce(force, ForceMode.VelocityChange);
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
            ApplyLaunchVelocity(velocity);
        }

        public void MoveGrabbed(Vector3 targetPosition, float followSpeed)
        {
            if (_isDead || !_context.IsGrabbed)
                return;

            Vector3 velocity = (targetPosition - transform.position) * followSpeed;
            _rigidbody.linearVelocity = velocity;
        }

        public void PrepareForPool()
        {
            _registry.Unregister(_rigidbody);
            _pauseService.Unregister(this); // no longer pausable once back in the pool
            _hitFlash.Restore();
            StopDeathReturn();
            _stateMachine?.Clear();
            _movement.DisableAgent();
            ClearTargets();
            ClearContextFlags();
            _isRingout = false;
            _timers.Knockback.Clear();
            _timers.Stasis.Clear();
            _impact.Clear();
            transform.localScale = Vector3.one * EnemyBodyScale;
            ParkRigidbody();
        }

        public void SetPoolReturnAction(Action<EnemyController> releaseToPool)
        {
            _releaseToPool = releaseToPool;
        }

        public bool TakeDamage(int damage)
        {
            if (_isDead)
                return false;

            PlayHitFeedback();
            _health.TakeDamage(damage); // fires Changed (bar/event); on 0 fires Died (score + dead state)

            if (_isDead)
                return true;

            _hitFlash.Play();
            return false;
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

        private void WireHealth()
        {
            _health.Changed += OnHealthChanged;
            _health.Died += OnHealthDepleted;
            _health.Initialize(_data.MaxHealth);
        }

        private void InitializeRingout()
        {
            _ringout.Initialize(transform, _data, _settings.Vfx, _scoreService, _comboService,
                _scorePopups, _audioService, () => _typeData);
        }

        private void CreateHealthBar()
        {
            _healthBar.Create(transform, _settings.Prefabs.WorldHealthBarPrefab, _health.Max, _data.HealthBarHeight);
        }

        // The shared prelude of Knockback + Grab: leave impact-projectile mode, reset the sweep origin and
        // clear the stasis / physics-recovery timers before a fresh melee impulse.
        private void ResetImpulseState()
        {
            _context.IsImpactProjectile = false;
            _impact.ResetSweepOrigin();
            _timers.Stasis.Clear();
            _timers.PhysicsRecoveryElapsed = 0f;
        }

        private void ApplyLaunchVelocity(Vector3 velocity)
        {
            _rigidbody.useGravity = true;
            _rigidbody.isKinematic = false;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.linearVelocity = velocity;
            _rigidbody.WakeUp();
        }

        private void PlayHitFeedback()
        {
            _visual?.PlayHit();
            _audioService?.PlaySfx(GameSfx.Impact);
        }

        private bool Die()
        {
            if (_isDead)
                return false;

            _health.Kill(); // fires Changed(0) + Died → OnHealthDepleted
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

        private void ResolveComponents()
        {
            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody>();

            if (_agent == null)
                _agent = GetComponent<NavMeshAgent>();

            if (_agent == null)
                _agent = gameObject.AddComponent<NavMeshAgent>();

            _agent.enabled = false;
            _capsule = ActorPhysicsUtility.NormalizeCapsuleRoot(transform);
        }

        private void InitializeCollaborators()
        {
            _movement.Initialize(transform, _rigidbody, _agent, _data, GetMoveSpeed, _settings.Grounding);
            _impact.Initialize(this, transform, _rigidbody, _data, () => _typeData, _registry);
            _groundRecovery.Initialize(transform, _rigidbody, _data, _timers, _settings.Grounding);
            _collisions.Initialize(this, transform, _rigidbody, _data, _impact, _timers, _groundRecovery);
        }

        private void SetupVisualsAndFlash()
        {
            EnsurePrimitiveVisual();
            _hitFlash.Initialize(GetComponentsInChildren<Renderer>(),
                _settings.Feel.HitFlashColor, _settings.Feel.HitFlashDuration);
        }

        private void BuildContext()
        {
            _context = new EnemyContext(
                _rigidbody, transform, _data, _movement, _visual, _timers, _groundRecovery, _impact, _collisions,
                () => IsPlayerAlive() ? _target : null, () => IsPlayerAlive() ? _playerTarget : null,
                () => _isDead, () => _typeData,
                ChangeToChaseState, ChangeToGroundRecoveryState, ReturnToPool, StartDeathReturn,
                StopForDeath, ResolveRingout);
        }

        // Ignore the player once they die — the chase/attack states early-return on a null target, so enemies
        // stop hunting + hitting a corpse and idle in place.
        private bool IsPlayerAlive()
        {
            return _playerTarget != null && !_playerTarget.IsDead;
        }

        // --- pool lifecycle --------------------------------------------------------------------

        private void ResetForSpawn()
        {
            EnsureStateMachine();
            StopDeathReturn();
            ResetSpawnFlags();
            _registry.Register(_rigidbody, this); // live for the span it is out of the pool
            _pauseService.Register(this);          // pausable only while out of the pool
            ResetCollaboratorsForSpawn();
            transform.localScale = Vector3.one * EnemyBodyScale;
            _health.Initialize(GetTypeAdjustedMaxHealth()); // fires Changed → HealthChanged + health bar
            _visual?.ResetState();
            ResetRigidbodyForSpawn();
        }

        private void ResetSpawnFlags()
        {
            _isInPool = false;
            _isDead = false;
            _isRingout = false;
            _paused = false; // a pooled enemy reused while the flag lingered would spawn frozen
            _deathReturnPending = false;
            ClearContextFlags();
        }

        private void ResetCollaboratorsForSpawn()
        {
            _hitFlash.Restore();
            _stateMachine.Clear();
            _movement.DisableAgent();
            _ringout.ResetForSpawn();
            _timers.ResetAll();
            _collisions.ResetForSpawn();
            _impact.Clear();
        }

        private void ResetRigidbodyForSpawn()
        {
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

        // Zeroes velocity and lets the body fall again — the pool-teardown counterpart of the spawn reset.
        private void ParkRigidbody()
        {
            if (_rigidbody == null)
                return;

            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.isKinematic = false;
        }

        private void ClearTargets()
        {
            _target = null;
            _playerTarget = null;
        }

        private void ClearContextFlags()
        {
            _context.IsGrabbed = false;
            _context.IsImpactProjectile = false;
            _context.NeedsGroundRecovery = false;
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

        private void StopDeathReturn()
        {
            if (_deathRoutine == null)
                return;

            StopCoroutine(_deathRoutine);
            _deathRoutine = null;
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

        // ~1000 units from origin — far past any legit arena spot. Catches an enemy flung to infinity
        // (hard launch / physics blow-up) so it rings out instead of drifting forever (invisible) and
        // spamming Renderer "Invalid worldAABB" from its visual.
        private const float MaxArenaSqrDistance = 1_000_000f;

        private void HandleOutOfBounds()
        {
            Vector3 position = transform.position;

            if (!ActorPhysicsUtility.IsFinite(position))
            {
                ReturnToPool(); // physics blew up (NaN/Inf) — drop it silently, no score/feedback at NaN
                return;
            }

            bool belowFloor = position.y < _settings.Feel.RingoutHeight;
            bool tooFarOut = new Vector2(position.x, position.z).sqrMagnitude > MaxArenaSqrDistance;

            if (belowFloor || tooFarOut)
                StartRingout();
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
            ClearContextFlags();
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