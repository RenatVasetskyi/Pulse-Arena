using System.Collections;
using Architecture.Services.Interfaces;
using Data;
using System;
using Game.Common;
using Game.Common.StateMachine;
using Game.Enemy.States;
using Game.Player;
using Game.Visuals;
using UI;
using UnityEngine;
using UnityEngine.AI;
using Zenject;

namespace Game.Enemy
{
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
        private Transform _target;
        private PlayerController _playerTarget;
        private readonly HitFlash _hitFlash = new();
        private readonly EnemyMovement _movement = new();
        private CapsuleCollider _capsule;
        private WorldHealthBar _healthBar;
        private EnemyPrimitiveVisual _visual;
        private readonly EnemyImpact _impact = new();
        private Action<EnemyController> _releaseToPool;
        private EnemyTypeData _typeData = EnemyTypeData.Default;
        private ActorStateMachine _stateMachine;
        private EnemyChaseState _chaseState;
        private EnemyGrabbedState _grabbedState;
        private EnemyPhysicsRecoveryState _physicsRecoveryState;
        private EnemyDeadState _deadState;
        private EnemyRingoutState _ringoutState;
        private ParticleSystem _ringoutBurst;
        private Coroutine _deathRoutine;
        private float _knockbackTimer;
        private float _stasisTimer;
        private float _heldDamageTimer;
        private float _attackCooldownTimer;
        private float _impactDamageCooldownTimer;
        private float _groundBounceCooldownTimer;
        private float _groundContactTimer;
        private float _physicsRecoveryTimer;
        private float _ringoutTimer;
        private bool _isRingout;
        private int _groundBounceCount;
        private readonly EnemyHealth _health = new();
        private bool _isDead;
        private bool _isGrabbed;
        private bool _isImpactProjectile;
        private bool _isInPool;
        private bool _needsGroundRecovery;

        public bool IsGrabbed
        {
            get { return _isGrabbed; }
        }

        public int Health => _health.Current;
        public int MaxHealth => _health.Max;
        public EnemyTypeData TypeData => _typeData;

        [Inject]
        public void Construct(GameSettings gameSettings, IScoreService scoreService, IAudioService audioService)
        {
            _settings = gameSettings;
            _data = gameSettings.EnemyData;
            _scoreService = scoreService;
            _audioService = audioService;
            _health.Changed += OnHealthChanged;
            _health.Died += OnHealthDepleted;
            _health.Reset(_data.MaxHealth);
            _movement.ConfigureAgent();
            CreateHealthBar();
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
            _isGrabbed = false;
            _isImpactProjectile = false;
            _needsGroundRecovery = false;
            _isRingout = false;
            _knockbackTimer = 0f;
            _stasisTimer = 0f;
            _impact.Clear();
            transform.localScale = Vector3.one;

            if (_rigidbody == null)
                return;

            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.isKinematic = false;
        }

        public void Knockback(Vector3 force)
        {
            _isImpactProjectile = false;
            _impact.ResetSweepOrigin();
            _stasisTimer = 0f;
            _physicsRecoveryTimer = 0f;
            _groundContactTimer = 0f;
            _knockbackTimer = _data.KnockbackDuration;
            ChangeToPhysicsRecoveryState();
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.AddForce(force, ForceMode.VelocityChange);
        }

        public void Grab()
        {
            if (_isDead)
                return;

            _isImpactProjectile = false;
            _impact.ResetSweepOrigin();
            _stasisTimer = 0f;
            _physicsRecoveryTimer = 0f;
            _knockbackTimer = 0f;
            ChangeToGrabbedState();
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

            _isImpactProjectile = true;
            _impact.ResetSweepOrigin();
            _impact.Clear();
            _groundBounceCount = 0;
            _groundBounceCooldownTimer = 0f;
            _groundContactTimer = 0f;
            _physicsRecoveryTimer = 0f;
            _stasisTimer = 0f;
            _knockbackTimer = duration;
            ChangeToPhysicsRecoveryState();
            _rigidbody.useGravity = true;
            _rigidbody.isKinematic = false;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.linearVelocity = velocity;
            _rigidbody.WakeUp();
        }

        public void PullTo(Vector3 targetPosition, float force, float upwardForceRatio, float stasisDuration)
        {
            _isImpactProjectile = false;
            _impact.ResetSweepOrigin();
            _physicsRecoveryTimer = 0f;
            _groundContactTimer = 0f;
            _stasisTimer = Mathf.Max(_stasisTimer, stasisDuration);
            _knockbackTimer = 0f;
            ChangeToPhysicsRecoveryState();

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

        private bool Die()
        {
            if (_isDead)
                return false;

            _health.Kill();   // fires Changed(0) + Died → OnHealthDepleted
            return true;
        }

        private void OnHealthChanged(int current, int max)
        {
            HealthChanged?.Invoke(current, max);
            _healthBar?.SetHealth(current, max);
        }

        private void OnHealthDepleted()
        {
            if (_isDead)
                return;

            _isDead = true;
            _scoreService.Add(GetScoreReward());
            ChangeToDeadState();
        }

        private void StopForDeath()
        {
            _movement.DisableAgent();
            _isGrabbed = false;
            _isImpactProjectile = false;
            _needsGroundRecovery = false;
            _knockbackTimer = 0f;
            _stasisTimer = 0f;

            if (_rigidbody == null)
                return;

            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.isKinematic = true;
        }

        private IEnumerator ReturnAfterDeath()
        {
            yield return new WaitForSeconds(_settings.EnemyVisuals.DeathPopDuration);
            _deathRoutine = null;
            ReturnToPool();
        }

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

            NormalizeCapsuleRoot();
            EnsurePrimitiveVisual();
            _hitFlash.Initialize(GetComponentsInChildren<Renderer>(), _data.HitFlashColor, _data.HitFlashDuration);
        }

        private void Update()
        {
            _hitFlash.Tick(Time.deltaTime);
        }

        private void NormalizeCapsuleRoot()
        {
            _capsule = GetComponent<CapsuleCollider>();

            if (_capsule == null)
                return;

            Vector3 center = _capsule.center;
            center.y = _capsule.height * 0.5f;
            _capsule.center = center;
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

        private void CreateHealthBar()
        {
            if (_healthBar != null)
                return;

            GameObject prefab = _settings.Prefabs.WorldHealthBarPrefab;

            if (prefab == null)
                return;

            _healthBar = Instantiate(prefab, transform, false).GetComponent<WorldHealthBar>();
            _healthBar.Initialize(_health.Max, _data.HealthBarHeight, _settings.Ui);
            _healthBar.SetHealth(_health.Current, _health.Max);
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

        private void OnDestroy()
        {
            if (!_isInPool)
                Destroyed?.Invoke(this);
        }

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
            _isGrabbed = false;
            _isImpactProjectile = false;
            _needsGroundRecovery = false;
            _knockbackTimer = 0f;
            _stasisTimer = 0f;
            _heldDamageTimer = 0f;
            _attackCooldownTimer = 0f;
            _impactDamageCooldownTimer = 0f;
            _groundBounceCooldownTimer = 0f;
            _groundContactTimer = 0f;
            _physicsRecoveryTimer = 0f;
            _ringoutTimer = 0f;
            _isRingout = false;
            _groundBounceCount = 0;
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

        private void EnsureStateMachine()
        {
            if (_stateMachine != null)
                return;

            _stateMachine = new ActorStateMachine();
            _chaseState = new EnemyChaseState(this);
            _grabbedState = new EnemyGrabbedState(this);
            _physicsRecoveryState = new EnemyPhysicsRecoveryState(this);
            _deadState = new EnemyDeadState(this);
            _ringoutState = new EnemyRingoutState(this);
        }

        private void ChangeToChaseState()
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

        private void ChangeToPhysicsRecoveryState()
        {
            EnsureStateMachine();

            if (!_isDead)
                _stateMachine.ChangeState(_physicsRecoveryState);
        }

        private void ChangeToDeadState()
        {
            EnsureStateMachine();
            _stateMachine.ChangeState(_deadState);
        }

        internal void EnterChaseState()
        {
            _isGrabbed = false;
            _needsGroundRecovery = false;
            _isImpactProjectile = false;
            _visual?.SetGrabbed(false);
            _visual?.SetThrown(false);
            TryEnableAgentControl();
        }

        internal void EnterGrabbedState()
        {
            _movement.DisableAgent();
            _isGrabbed = true;
            _needsGroundRecovery = false;
            _isImpactProjectile = false;
            _heldDamageTimer = _data.HeldDamageGrace;
            _visual?.SetGrabbed(true);
            _visual?.SetThrown(false);

            if (_rigidbody == null)
                return;

            _rigidbody.useGravity = true;
            _rigidbody.isKinematic = false;
            _rigidbody.WakeUp();
        }

        internal void EnterPhysicsRecoveryState()
        {
            _movement.DisableAgent();
            _isGrabbed = false;
            _needsGroundRecovery = true;
            _visual?.SetGrabbed(false);
            _visual?.SetThrown(_isImpactProjectile);

            if (_rigidbody == null)
                return;

            _rigidbody.useGravity = true;
            _rigidbody.isKinematic = false;
            _rigidbody.WakeUp();
        }

        internal void EnterDeadState()
        {
            StopForDeath();
            _visual?.PlayDeath();

            if (_deathRoutine == null)
                _deathRoutine = StartCoroutine(ReturnAfterDeath());
        }

        private void FixedUpdate()
        {
            EnsureStateMachine();

            if (!_isDead && !_isInPool && transform.position.y < _data.RingoutHeight)
                StartRingout();

            if (_isDead)
            {
                _stateMachine.FixedTick();
                return;
            }

            TickTimers();
            _stateMachine.FixedTick();
        }

        private void StartRingout()
        {
            if (_isRingout || _isDead || _isInPool)
                return;

            _isRingout = true;
            EnsureStateMachine();
            _stateMachine.ChangeState(_ringoutState);
        }

        internal void EnterRingoutState()
        {
            _isDead = true;
            _isGrabbed = false;
            _isImpactProjectile = false;
            _needsGroundRecovery = false;
            _knockbackTimer = 0f;
            _stasisTimer = 0f;
            _ringoutTimer = 0f;
            _movement.DisableAgent();
            _healthBar?.SetHealth(0, _health.Max);
            _scoreService.Add(GetScoreReward());
            _audioService?.PlaySfx(GameSfx.Ringout);
            SpawnRingoutFeedback();

            if (_rigidbody == null)
                return;

            _rigidbody.isKinematic = false;
            _rigidbody.useGravity = true;
        }

        internal void FixedTickRingoutState()
        {
            _ringoutTimer += Time.fixedDeltaTime;
            ApplyExtraGravity();

            float progress = Mathf.Clamp01(_ringoutTimer / Mathf.Max(0.1f, _data.RingoutDuration));
            transform.localScale = Vector3.one * Mathf.Lerp(1f, _data.RingoutShrinkScale, progress);

            if (progress >= 1f)
                ReturnToPool();
        }

        private void SpawnRingoutFeedback()
        {
            Vector3 feedbackPosition = new(transform.position.x, _data.RingoutTextHeight, transform.position.z);
            FloatingScoreText.Create(_settings.Prefabs.FloatingScoreTextPrefab, feedbackPosition,
                $"+{GetScoreReward()}", _settings.Vfx);
            PlayRingoutBurst(feedbackPosition);
        }

        private void PlayRingoutBurst(Vector3 position)
        {
            EnsureRingoutBurst();
            _ringoutBurst.transform.position = position;
            _ringoutBurst.Emit(_settings.Vfx.RingoutBurstCount);
        }

        private void EnsureRingoutBurst()
        {
            if (_ringoutBurst != null)
                return;

            GameObject burstObject = new("Ringout Burst");
            burstObject.transform.SetParent(transform.parent, false);
            _ringoutBurst = burstObject.AddComponent<ParticleSystem>();
            VfxData vfx = _settings.Vfx;

            ParticleSystem.MainModule main = _ringoutBurst.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(vfx.RingoutBurstLifetimeMin, vfx.RingoutBurstLifetimeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(vfx.RingoutBurstSpeedMin, vfx.RingoutBurstSpeedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(vfx.RingoutBurstSizeMin, vfx.RingoutBurstSizeMax);
            main.startColor = new ParticleSystem.MinMaxGradient(vfx.RingoutColorA, vfx.RingoutColorB);
            main.gravityModifier = vfx.RingoutBurstGravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = _ringoutBurst.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = _ringoutBurst.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            ParticleSystemRenderer particleRenderer = burstObject.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("Sprites/Default");
            particleRenderer.material = new Material(shader)
            {
                name = "Ringout Burst"
            };
        }

        internal void FixedTickChaseState()
        {
            if (_target == null)
                return;

            if (TryEnableAgentControl())
                _movement.MoveToTarget(_target);
            else
            {
                ApplyExtraGravity();
                _movement.MoveDirectlyToTarget(_target);
            }

            TryAttackTarget();
        }

        internal void FixedTickGrabbedState()
        {
            if (!_typeData.DamagesPlayerWhileHeld || _playerTarget == null || _isDead)
                return;

            if (_heldDamageTimer > 0f)
            {
                _heldDamageTimer -= Time.fixedDeltaTime;
                return;
            }

            Vector3 offset = _playerTarget.transform.position - transform.position;
            offset.y = 0f;

            if (offset.sqrMagnitude > _data.AttackRange * _data.AttackRange)
                return;

            if (_playerTarget.TakeDamage(_data.ContactDamage, transform.position))
                _heldDamageTimer = _typeData.HeldDamageInterval;
        }

        internal void FixedTickPhysicsRecoveryState()
        {
            if (_target == null)
                return;

            if (_stasisTimer > 0f)
            {
                ApplyExtraGravity();
                return;
            }

            if (_knockbackTimer > 0f)
            {
                ApplyExtraGravity();

                if (_isImpactProjectile)
                    SweepImpactDamage();

                return;
            }

            if (_needsGroundRecovery)
            {
                _physicsRecoveryTimer += Time.fixedDeltaTime;
                ApplyExtraGravity();

                if (_isImpactProjectile)
                    SweepImpactDamage();

                if (!CanFinishPhysicsRecovery())
                    return;

                FinishPhysicsRecovery();
                ChangeToChaseState();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            bool isGroundCollision = IsGroundCollision(collision);

            if (isGroundCollision)
                MarkGroundContact();

            if (_isDead || !_isImpactProjectile || _knockbackTimer <= 0f)
                return;

            Rigidbody collisionBody = collision.rigidbody;

            if (collisionBody != null && collisionBody.GetComponent<PlayerController>() != null)
                return;

            if (isGroundCollision)
            {
                TryBounceFromGround();
                return;
            }

            if (_impactDamageCooldownTimer > 0f)
                return;

            bool hitEnemy = collisionBody != null && collisionBody.GetComponent<EnemyController>() != null;

            if (hitEnemy)
            {
                if (_rigidbody.linearVelocity.magnitude < _data.ImpactDamageMinSpeed)
                    return;

                if (!_impact.TryDamageOnCollision(collision))
                    return;

                _impactDamageCooldownTimer = _data.ImpactDamageCooldown;
                TakeDamage(_data.ImpactDamage);
                return;
            }

            if (collision.relativeVelocity.magnitude < _data.ImpactDamageMinSpeed)
                return;

            _impactDamageCooldownTimer = _data.ImpactDamageCooldown;
            TakeDamage(Mathf.Max(1, _data.WallImpactDamage));
        }

        private void OnCollisionStay(Collision collision)
        {
            if (IsGroundCollision(collision))
                MarkGroundContact();
        }

        private void TryBounceFromGround()
        {
            if (_groundBounceCooldownTimer > 0f || _groundBounceCount >= _data.GroundBounceCount)
                return;

            Vector3 velocity = _rigidbody.linearVelocity;
            Vector3 planarVelocity = new(velocity.x, 0f, velocity.z);

            if (Mathf.Abs(velocity.y) < _data.GroundBounceMinVerticalSpeed &&
                planarVelocity.magnitude < _data.ImpactDamageMinSpeed)
                return;

            _groundBounceCount++;
            _groundBounceCooldownTimer = _data.GroundBounceCooldown;
            _knockbackTimer = Mathf.Max(_knockbackTimer, _data.GroundBounceKeepAliveDuration);

            Vector3 bounceVelocity = planarVelocity * _data.GroundBounceHorizontalDamping;
            bounceVelocity.y = _data.GroundBounceUpwardVelocity;

            _rigidbody.linearVelocity = bounceVelocity;
            _visual?.PlayGroundBounce();
        }

        private void SweepImpactDamage()
        {
            if (!_impact.DamageDuringSweep())
                return;

            _impactDamageCooldownTimer = _data.ImpactDamageCooldown;
            TakeDamage(_data.ImpactDamage);
        }

        private bool IsGroundCollision(Collision collision)
        {
            foreach (ContactPoint contact in collision.contacts)
            {
                if (contact.normal.y > 0.65f &&
                    ActorGroundingUtility.IsGroundCollider(contact.otherCollider))
                {
                    return true;
                }
            }

            return false;
        }

        private void TickTimers()
        {
            if (_stasisTimer > 0f)
                _stasisTimer -= Time.fixedDeltaTime;

            if (_knockbackTimer > 0f)
                _knockbackTimer -= Time.fixedDeltaTime;
            else if (!_needsGroundRecovery)
            {
                _isImpactProjectile = false;
                _visual?.SetThrown(false);
            }

            if (_attackCooldownTimer > 0f)
                _attackCooldownTimer -= Time.fixedDeltaTime;

            if (_impactDamageCooldownTimer > 0f)
                _impactDamageCooldownTimer -= Time.fixedDeltaTime;

            if (_groundBounceCooldownTimer > 0f)
                _groundBounceCooldownTimer -= Time.fixedDeltaTime;

            if (_groundContactTimer > 0f)
                _groundContactTimer -= Time.fixedDeltaTime;

            _impact.Tick(Time.fixedDeltaTime);
        }

        private void MarkGroundContact()
        {
            _groundContactTimer = _data.GroundContactMemory;
        }

        private bool CanFinishPhysicsRecovery()
        {
            if (_rigidbody == null)
                return true;

            if (_groundContactTimer <= 0f)
                return false;

            return TrySnapToPhysicalGround();
        }

        private void FinishPhysicsRecovery()
        {
            _needsGroundRecovery = false;
            _isImpactProjectile = false;
            _visual?.SetThrown(false);
            _physicsRecoveryTimer = 0f;

            if (_rigidbody == null)
                return;

            Vector3 velocity = _rigidbody.linearVelocity;
            _rigidbody.linearVelocity = new Vector3(velocity.x, 0f, velocity.z);
            _rigidbody.angularVelocity = Vector3.zero;
            ActorGroundingUtility.SnapToGround(transform, _data.GroundRecoveryProbeDistance);
        }

        private bool TrySnapToPhysicalGround()
        {
            return ActorGroundingUtility.SnapToGround(transform, _data.GroundRecoveryProbeDistance);
        }

        private void ApplyExtraGravity()
        {
            _rigidbody.AddForce(Vector3.down * _data.ExtraGravity, ForceMode.Acceleration);
        }

        private bool TryEnableAgentControl()
        {
            if (_movement.UsesAgent)
                return true;

            if (_target == null || _isDead || _isGrabbed ||
                _needsGroundRecovery || _knockbackTimer > 0f)
            {
                return false;
            }

            return _movement.TryEnableAgent();
        }

        private int GetTypeAdjustedMaxHealth()
        {
            return Mathf.Max(1, Mathf.RoundToInt(_data.MaxHealth * _typeData.HealthMultiplier));
        }

        private float GetMoveSpeed()
        {
            return _data.MoveSpeed * _typeData.MoveSpeedMultiplier;
        }

        private int GetScoreReward()
        {
            return Mathf.Max(1, Mathf.RoundToInt(_data.ScoreReward * _typeData.ScoreMultiplier));
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

