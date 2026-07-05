using System.Collections;
using System.Collections.Generic;
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
        private static readonly RaycastHit[] SweepHitBuffer = new RaycastHit[32];
        private static readonly Collider[] OverlapBuffer = new Collider[32];

        public event Action<EnemyController> Destroyed;
        public event Action<int, int> HealthChanged;

        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private NavMeshAgent _agent;
        [SerializeField] private Renderer[] _renderers;

        private GameSettings _settings;
        private EnemyData _data;
        private IScoreService _scoreService;
        private Transform _target;
        private PlayerController _playerTarget;
        private Material[][] _originalMaterials;
        private Material[][] _flashMaterials;
        private Material _hitFlashMaterial;
        private CapsuleCollider _capsule;
        private WorldHealthBar _healthBar;
        private EnemyPrimitiveVisual _visual;
        private readonly Dictionary<EnemyController, float> _impactHitTimers = new();
        private Action<EnemyController> _releaseToPool;
        private EnemyTypeData _typeData = EnemyTypeData.Default;
        private ActorStateMachine _stateMachine;
        private EnemyChaseState _chaseState;
        private EnemyGrabbedState _grabbedState;
        private EnemyPhysicsRecoveryState _physicsRecoveryState;
        private EnemyDeadState _deadState;
        private EnemyRingoutState _ringoutState;
        private ParticleSystem _ringoutBurst;
        private Coroutine _flashRoutine;
        private Coroutine _deathRoutine;
        private Vector3 _lastImpactPosition;
        private float _knockbackTimer;
        private float _stasisTimer;
        private float _heldDamageTimer;
        private float _attackCooldownTimer;
        private float _impactDamageCooldownTimer;
        private float _destinationUpdateTimer;
        private float _groundBounceCooldownTimer;
        private float _groundContactTimer;
        private float _physicsRecoveryTimer;
        private float _ringoutTimer;
        private bool _isRingout;
        private int _groundBounceCount;
        private int _maxHealth;
        private int _health;
        private bool _isDead;
        private bool _isGrabbed;
        private bool _isImpactProjectile;
        private bool _isInPool;
        private bool _needsGroundRecovery;
        private bool _usesAgent;

        public bool IsGrabbed
        {
            get { return _isGrabbed; }
        }

        public int Health => _health;
        public int MaxHealth => _maxHealth;
        public EnemyTypeData TypeData => _typeData;

        [Inject]
        public void Construct(GameSettings gameSettings, IScoreService scoreService)
        {
            _settings = gameSettings;
            _data = gameSettings.EnemyData;
            _scoreService = scoreService;
            _maxHealth = Mathf.Max(1, _data.MaxHealth);
            _health = _maxHealth;
            ConfigureAgent();
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
            ConfigureAgent();
            _visual?.ApplyTypeStyle(_typeData);
            ChangeToChaseState();
        }

        public void PrepareForPool()
        {
            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
                _flashRoutine = null;
            }

            if (_deathRoutine != null)
            {
                StopCoroutine(_deathRoutine);
                _deathRoutine = null;
            }

            RestoreMaterials();
            _stateMachine?.Clear();
            DisableAgentControl();
            _target = null;
            _playerTarget = null;
            _isGrabbed = false;
            _isImpactProjectile = false;
            _needsGroundRecovery = false;
            _isRingout = false;
            _knockbackTimer = 0f;
            _stasisTimer = 0f;
            _impactHitTimers.Clear();
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
            _lastImpactPosition = transform.position;
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
            _lastImpactPosition = transform.position;
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
            _lastImpactPosition = transform.position;
            _impactHitTimers.Clear();
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
            _lastImpactPosition = transform.position;
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

            _health -= Mathf.Max(0, damage);
            HealthChanged?.Invoke(Mathf.Max(0, _health), _maxHealth);
            _healthBar?.SetHealth(Mathf.Max(0, _health), _maxHealth);
            _visual?.PlayHit();

            if (_health > 0)
            {
                FlashHit();
                return false;
            }

            return Die();
        }

        public bool Kill()
        {
            return Die();
        }

        private bool Die()
        {
            if (_isDead)
                return false;

            _isDead = true;
            HealthChanged?.Invoke(0, _maxHealth);
            _healthBar?.SetHealth(0, _maxHealth);
            _scoreService.Add(GetScoreReward());
            ChangeToDeadState();

            return true;
        }

        private void StopForDeath()
        {
            DisableAgentControl();
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

            NormalizeCapsuleRoot();
            DisablePlaceholderRenderers();
            EnsurePrimitiveVisual();
            _renderers = GetComponentsInChildren<Renderer>();

            CacheMaterials();
            CreateHitFlashMaterial();
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

            _healthBar = WorldHealthBar.Create(transform, _maxHealth, _data.HealthBarHeight, _settings.Ui);
            _healthBar.SetHealth(_health, _maxHealth);
        }

        private void EnsurePrimitiveVisual()
        {
            _visual = GetComponentInChildren<EnemyPrimitiveVisual>();

            if (_visual == null)
                _visual = EnemyPrimitiveVisual.Create(transform, _rigidbody, _settings.EnemyVisuals);
            else
                _visual.Initialize(_rigidbody, _settings.EnemyVisuals);
        }

        private void DisablePlaceholderRenderers()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();

            foreach (Renderer placeholderRenderer in renderers)
            {
                if (placeholderRenderer == null ||
                    placeholderRenderer.GetComponentInParent<EnemyPrimitiveVisual>() != null)
                    continue;

                placeholderRenderer.enabled = false;
            }
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
            EnsureFlashMaterials();

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null && _flashMaterials[i] != null)
                    _renderers[i].sharedMaterials = _flashMaterials[i];
            }
        }

        private void EnsureFlashMaterials()
        {
            if (_flashMaterials != null)
                return;

            _flashMaterials = new Material[_renderers.Length][];

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null)
                    continue;

                Material[] flashMaterials = new Material[_renderers[i].sharedMaterials.Length];

                for (int j = 0; j < flashMaterials.Length; j++)
                    flashMaterials[j] = _hitFlashMaterial;

                _flashMaterials[i] = flashMaterials;
            }
        }

        private void RestoreMaterials()
        {
            if (_renderers == null || _originalMaterials == null)
                return;

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null ||
                    i >= _originalMaterials.Length ||
                    _originalMaterials[i] == null)
                {
                    continue;
                }

                _renderers[i].sharedMaterials = _originalMaterials[i];
            }
        }

        private void OnDestroy()
        {
            if (!_isInPool)
                Destroyed?.Invoke(this);
        }

        private void ResetForSpawn()
        {
            EnsureStateMachine();

            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
                _flashRoutine = null;
            }

            if (_deathRoutine != null)
            {
                StopCoroutine(_deathRoutine);
                _deathRoutine = null;
            }

            RestoreMaterials();
            _stateMachine.Clear();
            DisableAgentControl();

            _isInPool = false;
            _isDead = false;
            _isGrabbed = false;
            _isImpactProjectile = false;
            _needsGroundRecovery = false;
            _usesAgent = false;
            _knockbackTimer = 0f;
            _stasisTimer = 0f;
            _heldDamageTimer = 0f;
            _attackCooldownTimer = 0f;
            _impactDamageCooldownTimer = 0f;
            _destinationUpdateTimer = 0f;
            _groundBounceCooldownTimer = 0f;
            _groundContactTimer = 0f;
            _physicsRecoveryTimer = 0f;
            _ringoutTimer = 0f;
            _isRingout = false;
            _groundBounceCount = 0;
            _impactHitTimers.Clear();
            transform.localScale = Vector3.one;

            _maxHealth = GetTypeAdjustedMaxHealth();
            _health = _maxHealth;
            HealthChanged?.Invoke(_health, _maxHealth);
            _healthBar?.SetHealth(_health, _maxHealth);
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
            DisableAgentControl();
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
            DisableAgentControl();
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
            DisableAgentControl();
            _healthBar?.SetHealth(0, _maxHealth);
            _scoreService.Add(GetScoreReward());
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
            FloatingScoreText.Create(feedbackPosition, $"+{GetScoreReward()}", _settings.Vfx);
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
                MoveToTargetByNavMesh();
            else
            {
                ApplyExtraGravity();
                MoveToTargetDirectly();
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
                    DamageEnemiesDuringImpact();

                return;
            }

            if (_needsGroundRecovery)
            {
                _physicsRecoveryTimer += Time.fixedDeltaTime;
                ApplyExtraGravity();

                if (_isImpactProjectile)
                    DamageEnemiesDuringImpact();

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

                if (!TryDamageOtherEnemy(collision))
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

        private bool TryDamageOtherEnemy(Collision collision)
        {
            Rigidbody otherBody = collision.rigidbody;
            EnemyController otherEnemy = otherBody != null ? otherBody.GetComponent<EnemyController>() : null;

            if (otherEnemy == null || otherEnemy == this || _impactHitTimers.ContainsKey(otherEnemy))
                return false;

            HitEnemyWithImpact(otherEnemy);
            otherEnemy.TakeDamage(GetImpactDamage());
            _impactHitTimers[otherEnemy] = _data.ImpactDamageCooldown;

            return true;
        }

        private void DamageEnemiesDuringImpact()
        {
            if (_rigidbody.linearVelocity.magnitude < _data.ImpactDamageMinSpeed)
            {
                _lastImpactPosition = transform.position;
                return;
            }

            Vector3 velocity = _rigidbody.linearVelocity;
            Vector3 currentPosition = transform.position;
            Vector3 sweepStart = _lastImpactPosition;
            Vector3 sweepEnd = currentPosition;

            if (velocity.sqrMagnitude > 0.001f)
                sweepEnd += velocity.normalized * _data.ImpactDamageForwardOffset;

            Vector3 sweep = sweepEnd - sweepStart;
            bool damagedEnemy = false;

            if (sweep.sqrMagnitude > 0.001f)
            {
                int sweepHitCount = Physics.SphereCastNonAlloc(sweepStart, _data.ImpactDamageRadius,
                    sweep.normalized, SweepHitBuffer, sweep.magnitude, ~0, QueryTriggerInteraction.Ignore);

                for (int i = 0; i < sweepHitCount; i++)
                {
                    if (TryHitEnemyDuringImpact(SweepHitBuffer[i].collider))
                        damagedEnemy = true;
                }
            }

            int overlapCount = Physics.OverlapSphereNonAlloc(sweepEnd, _data.ImpactDamageRadius, OverlapBuffer,
                ~0, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < overlapCount; i++)
            {
                if (TryHitEnemyDuringImpact(OverlapBuffer[i]))
                    damagedEnemy = true;
            }

            if (damagedEnemy)
            {
                _impactDamageCooldownTimer = _data.ImpactDamageCooldown;
                TakeDamage(_data.ImpactDamage);
            }

            _lastImpactPosition = currentPosition;
        }

        private bool TryHitEnemyDuringImpact(Collider hit)
        {
            Rigidbody hitBody = hit.attachedRigidbody;
            EnemyController otherEnemy = hitBody != null ? hitBody.GetComponent<EnemyController>() : null;

            if (otherEnemy == null || otherEnemy == this || _impactHitTimers.ContainsKey(otherEnemy))
                return false;

            HitEnemyWithImpact(otherEnemy);
            otherEnemy.TakeDamage(GetImpactDamage());
            _impactHitTimers[otherEnemy] = _data.ImpactDamageCooldown;

            return true;
        }

        private void HitEnemyWithImpact(EnemyController otherEnemy)
        {
            Vector3 direction = otherEnemy.transform.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.001f)
                direction = _rigidbody.linearVelocity;

            direction.y = 0f;

            Vector3 knockbackDirection = (direction.normalized + Vector3.up * _data.ImpactKnockbackUpwardRatio).normalized;
            otherEnemy.Knockback(knockbackDirection * (_data.ImpactKnockbackForce * _typeData.ImpactKnockbackMultiplier));
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

            TickImpactHitTimers();
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

        private void TickImpactHitTimers()
        {
            if (_impactHitTimers.Count == 0)
                return;

            List<EnemyController> enemies = new(_impactHitTimers.Keys);

            foreach (EnemyController enemy in enemies)
            {
                if (enemy == null)
                {
                    _impactHitTimers.Remove(enemy);
                    continue;
                }

                _impactHitTimers[enemy] -= Time.fixedDeltaTime;

                if (_impactHitTimers[enemy] <= 0f)
                    _impactHitTimers.Remove(enemy);
            }
        }

        private void ApplyExtraGravity()
        {
            _rigidbody.AddForce(Vector3.down * _data.ExtraGravity, ForceMode.Acceleration);
        }

        private bool TryEnableAgentControl()
        {
            if (_usesAgent)
                return true;

            if (_agent == null || _target == null || _isDead || _isGrabbed ||
                _needsGroundRecovery || _knockbackTimer > 0f)
            {
                return false;
            }

            if (!_agent.enabled)
                _agent.enabled = true;

            if (!TryPlaceAgentOnNavMesh())
            {
                _agent.enabled = false;
                _usesAgent = false;
                _rigidbody.isKinematic = false;
                return false;
            }

            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.isKinematic = true;
            _usesAgent = true;
            _destinationUpdateTimer = 0f;

            return true;
        }

        private void DisableAgentControl()
        {
            if (_agent != null && _agent.enabled)
            {
                if (_agent.isOnNavMesh)
                    _agent.ResetPath();

                _agent.enabled = false;
            }

            _usesAgent = false;

            if (_rigidbody != null)
                _rigidbody.isKinematic = false;
        }

        private bool TryPlaceAgentOnNavMesh()
        {
            if (!ActorGroundingUtility.TryGetGroundedPosition(transform, _data.GroundRecoveryProbeDistance,
                    0.02f, out Vector3 groundedPosition))
            {
                return false;
            }

            transform.position = groundedPosition;

            if (!TrySampleRecoveryNavMesh(out NavMeshHit hit))
            {
                return _agent.isOnNavMesh;
            }

            bool warped = _agent.Warp(hit.position);

            if (warped)
                transform.position = groundedPosition;

            return warped || _agent.isOnNavMesh;
        }

        private bool TrySampleRecoveryNavMesh(out NavMeshHit hit)
        {
            float sampleDistance = Mathf.Max(_data.NavMeshSampleDistance, _data.GroundRecoveryProbeDistance);
            return NavMesh.SamplePosition(transform.position, out hit, sampleDistance, NavMesh.AllAreas);
        }

        private void ConfigureAgent()
        {
            if (_agent == null || _data == null)
                return;

            _agent.speed = GetMoveSpeed();
            _agent.acceleration = _data.AgentAcceleration;
            _agent.angularSpeed = _data.AgentAngularSpeed;
            _agent.stoppingDistance = _data.AgentStoppingDistance;
            _agent.radius = _data.AgentRadius;
            _agent.height = _data.AgentHeight;
            _agent.baseOffset = 0f;
            _agent.updateRotation = false;
            _agent.updatePosition = true;
            _agent.autoBraking = false;
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        }

        private void MoveToTargetByNavMesh()
        {
            if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh)
            {
                _usesAgent = false;
                return;
            }

            if (IsWithinStoppingDistance())
            {
                if (_agent.hasPath)
                    _agent.ResetPath();

                RotateTo(_target.position - transform.position);
                return;
            }

            _destinationUpdateTimer -= Time.fixedDeltaTime;

            if (_destinationUpdateTimer <= 0f)
            {
                Vector3 destination = _target.position;

                if (NavMesh.SamplePosition(_target.position, out NavMeshHit hit,
                        _data.NavMeshSampleDistance, NavMesh.AllAreas))
                {
                    destination = hit.position;
                }

                _agent.SetDestination(destination);
                _destinationUpdateTimer = _data.DestinationUpdateInterval;
            }

            RotateTo(_agent.desiredVelocity);
        }

        private void MoveToTargetDirectly()
        {
            Vector3 offset = _target.position - transform.position;
            offset.y = 0f;

            if (offset.sqrMagnitude <= _data.AgentStoppingDistance * _data.AgentStoppingDistance)
            {
                _rigidbody.linearVelocity = new Vector3(0f, _rigidbody.linearVelocity.y, 0f);
                RotateTo(offset);
                return;
            }

            Vector3 direction = offset.normalized;
            direction.y = 0f;
            Vector3 horizontalVelocity = direction * GetMoveSpeed();

            _rigidbody.linearVelocity = new Vector3(
                horizontalVelocity.x,
                _rigidbody.linearVelocity.y,
                horizontalVelocity.z);

            RotateTo(direction);
        }

        private bool IsWithinStoppingDistance()
        {
            Vector3 offset = _target.position - transform.position;
            offset.y = 0f;

            return offset.sqrMagnitude <= _data.AgentStoppingDistance * _data.AgentStoppingDistance;
        }

        private void RotateTo(Vector3 direction)
        {
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.01f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Lerp(transform.rotation,
                targetRotation, _data.RotationSpeed * Time.fixedDeltaTime);
        }

        private int GetTypeAdjustedMaxHealth()
        {
            return Mathf.Max(1, Mathf.RoundToInt(_data.MaxHealth * _typeData.HealthMultiplier));
        }

        private int GetImpactDamage()
        {
            return Mathf.Max(1, Mathf.RoundToInt(_data.ImpactDamage * _typeData.ImpactDamageMultiplier));
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
