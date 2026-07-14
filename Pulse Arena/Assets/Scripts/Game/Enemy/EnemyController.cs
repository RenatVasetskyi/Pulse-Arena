using System;
using System.Collections.Generic;
using Architecture.Services.Interfaces;
using Data;
using Game.Common;
using Game.Common.StateMachine;
using Game.Enemy.Behaviors;
using Game.Enemy.Interfaces;
using Game.Enemy.States;
using Game.Player;
using Game.Visuals;
using UnityEngine;
using UnityEngine.AI;
using Zenject;
using Random = UnityEngine.Random;

namespace Game.Enemy
{
    /// <summary>
    ///     The enemy's thin state router. It wires the collaborators (movement, impact, timers, ground
    ///     recovery, collisions, ringout, health-bar presenter) together behind a lean <see cref="EnemyContext" />,
    ///     owns the pool lifecycle + the public API (Knockback / Grab / Launch / …), and forwards Unity's
    ///     FixedUpdate + collision callbacks into the state machine / collision handler. All per-frame logic
    ///     lives in the seven state classes; the shared flags they flip live on the context (single source of
    ///     truth) and the controller reads/writes them through it. Death pools the corpse off the visual's
    ///     <see cref="IEnemyVisual.DeathCompleted" /> event (the death clip's own animation event), not a guessed
    ///     timer. Each lifecycle step (spawn reset / pool teardown / wiring) is a small single-purpose method the
    ///     coordinator calls in order.
    /// </summary>
    public class EnemyController : MonoBehaviour, IPausable
    {
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private NavMeshAgent _agent;
        [SerializeField] private MonoBehaviour _visualBehaviour;

        [Tooltip("Overall body scale for THIS enemy prefab — scales the visual + collider together on spawn, so each creature carries its own footprint.")]
        [SerializeField] private float _bodyScale = 0.7f;

        [Tooltip("World-space height of the floating HP bar for THIS prefab (taller models need it raised so the head does not overlap it). Negative = use the shared EnemyData.HealthBarHeight.")]
        [SerializeField] private float _healthBarHeight = -1f;
        private IEnemyVisual _visual;
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

        // The active combat brain (selected per spawn from EnemyTypeData.Behavior) + the per-instance cache so
        // reused pooled enemies never re-allocate one for a behaviour they have already run.
        private readonly Dictionary<EnemyBehaviorId, IEnemyBehavior> _behaviors = new();
        private IEnemyBehavior _behavior;
        private EnemyAttackState _attackState;
        private IAudioService _audioService;
        private CapsuleCollider _capsule;
        private EnemyChaseState _chaseState;
        private IComboService _comboService;
        private EnemyContext _context;
        private EnemyData _data;
        private EnemyDeadState _deadState;
        private EnemyFlipState _flipState;
        private EnemyGrabbedState _grabbedState;
        private EnemyGroundRecoveryState _groundRecoveryState;
        private EnemyIdleState _idleState;
        private bool _isDead;
        private bool _isInPool;
        private bool _isRingout;
        private EnemyKnockbackState _knockbackState;
        private EnemyPausedState _pausedState;
        private PlayerController _playerTarget;
        private IEnemyRegistry _registry;
        private Action<EnemyController> _releaseToPool;
        private EnemyRingoutState _ringoutState;
        private IScorePopupService _scorePopups;
        private IScoreService _scoreService;
        private float _spawnSizeFactor = 1f;
        private float _spawnSpeedFactor = 1f;

        private GameSettings _settings;
        private EnemyStasisState _stasisState;
        private EnemyTauntState _tauntState;
        private ActorStateMachine _stateMachine;
        private Transform _target;
        private EnemyTypeData _typeData = EnemyTypeData.Default;
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
        public IEnemyVisual Visual => _visual;

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
            _behavior = ResolveBehavior(_typeData.Behavior);
            ResetForSpawn();
            _target = target;
            _playerTarget = target.GetComponentInParent<PlayerController>();
            _movement.ConfigureAgent();
            _visual?.ApplyTypeStyle(_typeData);
            EnterSpawnState();
        }

        // Models with a spawn taunt (the karate) stand their ground and taunt first; the rest hunt immediately.
        private void EnterSpawnState()
        {
            if (_visual != null && _visual.HasSpawnTaunt)
                ChangeToTauntState();
            else
                ChangeToChaseState();
        }

        // --- Unity lifecycle -------------------------------------------------------------------

        private void Awake()
        {
            ResolveComponents();
            InitializeCollaborators();
            SetupVisualsAndFlash();
            BuildContext();
            BuildStateMachine();
        }

        // The controller only drives the machine + the actor-wide ambient tick (which lives in the context); the
        // paused check reads the machine's own pause flag, so the EnemyPausedState is the single pause source.
        private void Update()
        {
            if (_stateMachine.IsPaused)
                return;

            _context.TickCommon();
        }

        // The out-of-bounds ring-out CAN transition, so it must run OUTSIDE the state tick (via the context) —
        // otherwise a rung-out state would keep acting the same frame. Timers ride along; dead skips both.
        private void FixedUpdate()
        {
            if (_stateMachine.IsPaused)
                return;

            if (_isDead)
            {
                _stateMachine.FixedTick();
                return;
            }

            if (!_isInPool)
                _context.FixedTickCommon();

            _stateMachine.FixedTick();
        }

        private void OnDestroy()
        {
            // Covers the non-pooled Destroy path (ReturnToPool with no release action) + scene teardown,
            // neither of which runs PrepareForPool, so the registry never keeps a dangling entry.
            _registry?.Unregister(_rigidbody);
            _pauseService?.Unregister(this);
            StopDeathReturn(); // drop the visual's DeathCompleted subscription

            if (!_isInPool)
                Destroyed?.Invoke(this);
        }

        // Mechanical pause = overlay the EnemyPausedState on the running state (it caches + freezes the body,
        // halts the agent, gates the visual + ringout effect on Enter, restores on Exit); the suspended state keeps
        // its exact frame + countdowns, and the ambient tick below is gated on the machine's paused flag.
        public void Pause()
        {
            _stateMachine.Pause(_pausedState);
        }

        public void Resume()
        {
            _stateMachine.Resume();
        }

        private void OnCollisionEnter(Collision collision)
        {
            _collisions.OnCollisionEnter(collision, _context.IsImpactProjectile, _isDead);
        }

        private void OnCollisionStay(Collision collision)
        {
            _collisions.OnCollisionStay(collision);
        }

        /// <summary>
        ///     Sucked into an arena pit: ring out by sinking straight down into the maw at <paramref name="pitCenter" />
        ///     (centering + descending at <paramref name="sinkSpeed" />) rather than tumbling off an edge. The collider
        ///     drop that lets the body fall through the floor into the hole is shared with the edge path in
        ///     <see cref="StartRingout" />.
        /// </summary>
        public void FallIntoPit(Vector3 pitCenter, float sinkSpeed)
        {
            _context.PitSinkCenter = pitCenter;
            _context.PitSinkSpeed = sinkSpeed;
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
            _context.ClearFlags();
            _isRingout = false;
            _timers.Knockback.Clear();
            _timers.Stasis.Clear();
            _impact.Clear();
            transform.localScale = Vector3.one * _bodyScale;
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
            float barHeight = _healthBarHeight >= 0f ? _healthBarHeight : _data.HealthBarHeight;
            _healthBar.Create(transform, _settings.Prefabs.WorldHealthBarPrefab, _health.Max, barHeight);
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
            _healthBar.Hide(); // drop the (now-empty) bar the instant the death animation starts
            _ringout.AwardKill(out _);
            ChangeToDeadState();
        }

        // _rigidbody, _agent and _visual are all baked on the enemy prefab (inspector-wired). The agent is
        // baked disabled and stays parked here until a state hands control to it via TryEnableAgent (once it is
        // safely on the NavMesh), so a pooled enemy spawned off-mesh at the origin never fires an OnEnable warp.
        private void ResolveComponents()
        {
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
            InitializeVisual();
            _hitFlash.Initialize(GetComponentsInChildren<Renderer>(),
                _settings.Feel.HitFlashColor, _settings.Feel.HitFlashDuration);
        }

        private void BuildContext()
        {
            _context = new EnemyContext(
                _rigidbody, transform, _data, _movement, _visual, _timers, _groundRecovery, _impact, _collisions,
                _ringout, _hitFlash, _settings.Feel.RingoutHeight,
                () => IsPlayerAlive() ? _target : null, () => IsPlayerAlive() ? _playerTarget : null,
                () => _isDead, () => _typeData, () => _behavior,
                ChangeToIdleState, ChangeToAttackState, ChangeToChaseState, ChangeToGroundRecoveryState,
                ChangeToTauntState, ChangeToFlipState,
                ReturnToPool, StartRingout, StartDeathReturn, ResolveRingout);
        }

        // Ignore the player once they die — the chase/attack states early-return on a null target, so enemies
        // stop hunting + hitting a corpse and idle in place.
        private bool IsPlayerAlive()
        {
            return _playerTarget != null && !_playerTarget.IsDead;
        }

        // Pick this type's combat brain, caching it per instance so a pooled enemy reused as the same behaviour
        // never re-allocates one. It is Initialize'd with the (already-built, instance-stable) context the first
        // time it is seen, so one wiring covers every reuse.
        private IEnemyBehavior ResolveBehavior(EnemyBehaviorId id)
        {
            if (!_behaviors.TryGetValue(id, out IEnemyBehavior behavior))
            {
                behavior = EnemyBehaviorFactory.Create(id);
                behavior.Initialize(_context);
                _behaviors[id] = behavior;
            }

            return behavior;
        }

        // --- pool lifecycle --------------------------------------------------------------------

        private void ResetForSpawn()
        {
            StopDeathReturn();
            ResetSpawnFlags();
            RollSpawnVariation();
            _registry.Register(_rigidbody, this); // live for the span it is out of the pool
            _pauseService.Register(this);          // pausable only while out of the pool
            ResetCollaboratorsForSpawn();
            transform.localScale = Vector3.one * _bodyScale * _spawnSizeFactor;
            _healthBar.Show(); // re-show the bar a previous death hid on this pooled instance
            _health.Initialize(GetTypeAdjustedMaxHealth()); // fires Changed → HealthChanged + health bar
            _visual?.ResetState();
            ResetRigidbodyForSpawn();
        }

        private void ResetSpawnFlags()
        {
            _isInPool = false;
            _isDead = false;
            _isRingout = false;
            _context.ClearFlags();
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
            if (_capsule != null)
                _capsule.enabled = true; // re-arm the collider a pit-sink disabled

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

        // EnemyDeadState triggers this via EnemyContext.StartDeathReturn: wait for the visual to finish its death
        // presentation (its clip's animation event), THEN pool the corpse — no guessed duration. A visual-less
        // enemy has nothing to play, so it pools immediately.
        private void StartDeathReturn()
        {
            if (_visual == null)
            {
                ReturnToPool();
                return;
            }

            _visual.DeathCompleted -= OnDeathCompleted; // idempotent: never double-subscribe
            _visual.DeathCompleted += OnDeathCompleted;
        }

        private void StopDeathReturn()
        {
            if (_visual != null)
                _visual.DeathCompleted -= OnDeathCompleted;
        }

        // The visual's death clip has finished — pool the corpse now.
        private void OnDeathCompleted()
        {
            StopDeathReturn();
            ReturnToPool();
        }

        // --- state machine + transitions -------------------------------------------------------

        // Built once at the end of Awake — after BuildContext, and Awake runs once per pooled instance (the
        // machine survives pool reuse: PrepareForPool only Clears the active state, never the machine). So the
        // transitions + FixedTick never need a lazy guard.
        private void BuildStateMachine()
        {
            _stateMachine = new ActorStateMachine();
            _idleState = new EnemyIdleState(_context);
            _chaseState = new EnemyChaseState(_context);
            _attackState = new EnemyAttackState(_context);
            _tauntState = new EnemyTauntState(_context);
            _flipState = new EnemyFlipState(_context);
            _grabbedState = new EnemyGrabbedState(_context);
            _stasisState = new EnemyStasisState(_context);
            _groundRecoveryState = new EnemyGroundRecoveryState(_context);
            _knockbackState = new EnemyKnockbackState(_context, _groundRecoveryState);
            _deadState = new EnemyDeadState(_context);
            _ringoutState = new EnemyRingoutState(_context);
            _pausedState = new EnemyPausedState(_context);
        }

        internal void ChangeToChaseState()
        {
            if (!_isDead)
                _stateMachine.ChangeState(_chaseState);
        }

        internal void ChangeToIdleState()
        {
            if (!_isDead)
                _stateMachine.ChangeState(_idleState);
        }

        internal void ChangeToAttackState()
        {
            if (!_isDead)
                _stateMachine.ChangeState(_attackState);
        }

        internal void ChangeToTauntState()
        {
            if (!_isDead)
                _stateMachine.ChangeState(_tauntState);
        }

        internal void ChangeToFlipState()
        {
            if (!_isDead)
                _stateMachine.ChangeState(_flipState);
        }

        private void ChangeToGrabbedState()
        {
            if (!_isDead)
                _stateMachine.ChangeState(_grabbedState);
        }

        internal void ChangeToStasisState()
        {
            if (!_isDead)
                _stateMachine.ChangeState(_stasisState);
        }

        internal void ChangeToKnockbackState()
        {
            if (!_isDead)
                _stateMachine.ChangeState(_knockbackState);
        }

        internal void ChangeToGroundRecoveryState()
        {
            if (!_isDead)
                _stateMachine.ChangeState(_groundRecoveryState);
        }

        private void ChangeToDeadState()
        {
            _stateMachine.ChangeState(_deadState);
        }

        private void StartRingout()
        {
            if (_isRingout || _isDead || _isInPool)
                return;

            _isRingout = true;

            // The enemy is leaving the arena (tumbling off an edge or sinking into a pit). Drop its collider so the
            // dying body neither rests on the floor nor collides with — and spins wildly off — the player who walks
            // into it. The tumble/sink is driven by velocity + gravity, not collision, so nothing else needs it.
            if (_capsule != null)
                _capsule.enabled = false;

            _stateMachine.ChangeState(_ringoutState);
        }

        // --- state-owned side effects the controller still hosts -------------------------------
        // Reached through an EnemyContext callback because it touches controller-private state (the dead flag,
        // health-bar presenter, RingoutHandler) a state can't set. StopForDeath moved into EnemyDeadState.Enter.

        // The controller-coupled slice of the old EnterRingoutState body, in original order: mark dead,
        // zero the health bar, run the RingoutHandler (EnemyRingoutState.Enter → EnemyContext.ResolveRingout).
        private void ResolveRingout()
        {
            _isDead = true;
            _healthBar.Hide(); // ring-out has no death clip, but the bar should still vanish with the tumbling body
            _ringout.ResolveRingout();
        }

        // --- shared timer tick + small helpers -------------------------------------------------

        // Shared cooldowns that tick in EVERY non-dead state (EnemyTimers.TickFixed — includes Stasis, as
        // the original did). The knockback-timer decrement + its expiry side effect live in
        // EnemyKnockbackState, not here. The impact hit-set needs no per-frame tick — it is a HashSet cleared
        // once per throw.
        // The visual is baked on the enemy prefab and inspector-wired, so this just hands the assigned ref its
        // dependencies — nothing is fetched at runtime.
        private void InitializeVisual()
        {
            _visual = _visualBehaviour as IEnemyVisual;

            if (_visual == null)
            {
                Debug.LogError("Enemy visual (IEnemyVisual) is not assigned on the enemy prefab.", this);
                return;
            }

            _visual.Initialize(_rigidbody);
        }

        private int GetTypeAdjustedMaxHealth()
        {
            return Mathf.Max(1, Mathf.RoundToInt(_data.MaxHealth * _typeData.HealthMultiplier));
        }

        private float GetMoveSpeed()
        {
            return _data.MoveSpeed * _typeData.MoveSpeedMultiplier * _spawnSpeedFactor;
        }

        // Per-spawn variety: roll a speed factor and an INVERSELY-tied size factor from ONE roll, so a faster enemy
        // always comes out smaller and a slower one bigger (the wolves/skeletons differ in build, not just palette).
        private void RollSpawnVariation()
        {
            float t = Random.value;
            _spawnSpeedFactor = Mathf.Lerp(1f - _data.SpeedVariation, 1f + _data.SpeedVariation, t);
            _spawnSizeFactor = Mathf.Lerp(1f + _data.SizeVariation, 1f - _data.SizeVariation, t);
        }
    }
}