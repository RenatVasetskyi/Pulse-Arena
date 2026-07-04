using System.Collections;
using System.Collections.Generic;
using Architecture.Services.Interfaces;
using Data;
using System;
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
        private const float GroundContactMemory = 0.12f;

        public event Action<EnemyController> Destroyed;
        public event Action<int, int> HealthChanged;

        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private NavMeshAgent _agent;
        [SerializeField] private Renderer[] _renderers;

        private EnemyData _data;
        private IScoreService _scoreService;
        private Transform _target;
        private PlayerController _playerTarget;
        private Material[][] _originalMaterials;
        private Material _hitFlashMaterial;
        private WorldHealthBar _healthBar;
        private EnemyPrimitiveVisual _visual;
        private readonly Dictionary<EnemyController, float> _impactHitTimers = new();
        private Coroutine _flashRoutine;
        private Vector3 _lastImpactPosition;
        private float _knockbackTimer;
        private float _stasisTimer;
        private float _attackCooldownTimer;
        private float _impactDamageCooldownTimer;
        private float _destinationUpdateTimer;
        private float _groundBounceCooldownTimer;
        private float _groundContactTimer;
        private float _physicsRecoveryTimer;
        private int _groundBounceCount;
        private int _maxHealth;
        private int _health;
        private bool _isDead;
        private bool _isGrabbed;
        private bool _isImpactProjectile;
        private bool _needsGroundRecovery;
        private bool _usesAgent;

        public bool IsGrabbed
        {
            get { return _isGrabbed; }
        }

        public int Health => _health;
        public int MaxHealth => _maxHealth;

        [Inject]
        public void Construct(GameSettings gameSettings, IScoreService scoreService)
        {
            _data = gameSettings.EnemyData;
            _scoreService = scoreService;
            _maxHealth = Mathf.Max(1, _data.MaxHealth);
            _health = _maxHealth;
            ConfigureAgent();
            CreateHealthBar();
        }

        public void Initialize(Transform target)
        {
            _target = target;
            _playerTarget = target.GetComponentInParent<PlayerController>();
            ConfigureAgent();
            TryEnableAgentControl();
        }

        public void Knockback(Vector3 force)
        {
            DisableAgentControl();
            _isGrabbed = false;
            _isImpactProjectile = false;
            _needsGroundRecovery = true;
            _visual?.SetGrabbed(false);
            _visual?.SetThrown(false);
            _lastImpactPosition = transform.position;
            _stasisTimer = 0f;
            _physicsRecoveryTimer = 0f;
            _groundContactTimer = 0f;
            _knockbackTimer = _data.KnockbackDuration;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.AddForce(force, ForceMode.VelocityChange);
        }

        public void Grab()
        {
            if (_isDead)
                return;

            DisableAgentControl();
            _isGrabbed = true;
            _isImpactProjectile = false;
            _needsGroundRecovery = false;
            _visual?.SetGrabbed(true);
            _visual?.SetThrown(false);
            _lastImpactPosition = transform.position;
            _stasisTimer = 0f;
            _physicsRecoveryTimer = 0f;
            _knockbackTimer = 0f;
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

            DisableAgentControl();
            _isGrabbed = false;
            _isImpactProjectile = true;
            _needsGroundRecovery = true;
            _visual?.SetGrabbed(false);
            _visual?.SetThrown(true);
            _lastImpactPosition = transform.position;
            _impactHitTimers.Clear();
            _groundBounceCount = 0;
            _groundBounceCooldownTimer = 0f;
            _groundContactTimer = 0f;
            _physicsRecoveryTimer = 0f;
            _stasisTimer = 0f;
            _knockbackTimer = duration;
            _rigidbody.useGravity = true;
            _rigidbody.isKinematic = false;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.linearVelocity = velocity;
            _rigidbody.WakeUp();
        }

        public void PullTo(Vector3 targetPosition, float force, float upwardForceRatio, float stasisDuration)
        {
            DisableAgentControl();
            _isImpactProjectile = false;
            _needsGroundRecovery = true;
            _lastImpactPosition = transform.position;
            _physicsRecoveryTimer = 0f;
            _groundContactTimer = 0f;
            _stasisTimer = Mathf.Max(_stasisTimer, stasisDuration);
            _knockbackTimer = 0f;

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
            _scoreService.Add(_data.ScoreReward);
            StopForDeath();
            _visual?.PlayDeath();
            StartCoroutine(DestroyAfterDeath());

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

        private IEnumerator DestroyAfterDeath()
        {
            yield return new WaitForSeconds(0.38f);
            Destroy(gameObject);
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
            CapsuleCollider capsule = GetComponent<CapsuleCollider>();

            if (capsule == null)
                return;

            Vector3 center = capsule.center;
            center.y = capsule.height * 0.5f;
            capsule.center = center;
        }

        private void CreateHealthBar()
        {
            if (_healthBar != null)
                return;

            _healthBar = WorldHealthBar.Create(transform, _maxHealth, _data.HealthBarHeight);
            _healthBar.SetHealth(_health, _maxHealth);
        }

        private void EnsurePrimitiveVisual()
        {
            _visual = GetComponentInChildren<EnemyPrimitiveVisual>();

            if (_visual == null)
                _visual = EnemyPrimitiveVisual.Create(transform, _rigidbody);
            else
                _visual.Initialize(_rigidbody);
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
            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer enemyRenderer = _renderers[i];

                if (enemyRenderer == null)
                    continue;

                Material[] flashMaterials = new Material[enemyRenderer.sharedMaterials.Length];

                for (int j = 0; j < flashMaterials.Length; j++)
                    flashMaterials[j] = _hitFlashMaterial;

                enemyRenderer.sharedMaterials = flashMaterials;
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

        private void OnDestroy()
        {
            Destroyed?.Invoke(this);
        }

        private void FixedUpdate()
        {
            TickTimers();

            if (_isDead)
                return;

            if (_isGrabbed)
                return;

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
            }

            if (TryEnableAgentControl())
                MoveToTargetByNavMesh();
            else
            {
                ApplyExtraGravity();
                MoveToTargetDirectly();
            }

            TryAttackTarget();
        }

        private void OnCollisionEnter(Collision collision)
        {
            bool isGroundCollision = IsGroundCollision(collision);

            if (isGroundCollision)
                MarkGroundContact();

            if (_isDead || !_isImpactProjectile || _knockbackTimer <= 0f)
                return;

            if (collision.collider.GetComponentInParent<PlayerController>() != null)
                return;

            if (isGroundCollision)
            {
                TryBounceFromGround();
                return;
            }

            if (_impactDamageCooldownTimer > 0f)
                return;

            if (_rigidbody.linearVelocity.magnitude < _data.ImpactDamageMinSpeed)
                return;

            if (!TryDamageOtherEnemy(collision))
                return;

            _impactDamageCooldownTimer = _data.ImpactDamageCooldown;
            TakeDamage(_data.ImpactDamage);
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
            EnemyController otherEnemy = collision.collider.GetComponentInParent<EnemyController>();

            if (otherEnemy == null || otherEnemy == this || _impactHitTimers.ContainsKey(otherEnemy))
                return false;

            HitEnemyWithImpact(otherEnemy);
            otherEnemy.TakeDamage(_data.ImpactDamage);
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
                RaycastHit[] sweepHits = Physics.SphereCastAll(sweepStart, _data.ImpactDamageRadius,
                    sweep.normalized, sweep.magnitude, ~0, QueryTriggerInteraction.Ignore);

                foreach (RaycastHit sweepHit in sweepHits)
                {
                    if (TryHitEnemyDuringImpact(sweepHit.collider))
                        damagedEnemy = true;
                }
            }

            Collider[] hits = Physics.OverlapSphere(sweepEnd, _data.ImpactDamageRadius, ~0,
                QueryTriggerInteraction.Ignore);

            foreach (Collider hit in hits)
            {
                if (TryHitEnemyDuringImpact(hit))
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
            EnemyController otherEnemy = hit.GetComponentInParent<EnemyController>();

            if (otherEnemy == null || otherEnemy == this || _impactHitTimers.ContainsKey(otherEnemy))
                return false;

            HitEnemyWithImpact(otherEnemy);
            otherEnemy.TakeDamage(_data.ImpactDamage);
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
            otherEnemy.Knockback(knockbackDirection * _data.ImpactKnockbackForce);
        }

        private bool IsGroundCollision(Collision collision)
        {
            foreach (ContactPoint contact in collision.contacts)
            {
                if (contact.normal.y > 0.65f)
                    return true;
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
            _groundContactTimer = GroundContactMemory;
        }

        private bool CanFinishPhysicsRecovery()
        {
            if (_rigidbody == null)
                return true;

            if (_groundContactTimer > 0f)
            {
                TrySnapToPhysicalGround();
                return true;
            }

            if (_physicsRecoveryTimer < _data.GroundRecoveryForceAfter)
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
        }

        private bool TrySnapToPhysicalGround()
        {
            Vector3 origin = transform.position + Vector3.up * GetGroundProbeUpOffset();
            float distance = _data.GroundRecoveryProbeDistance + GetGroundProbeUpOffset();
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, distance, ~0, QueryTriggerInteraction.Ignore);

            if (hits.Length == 0)
                return false;

            RaycastHit bestHit = default;
            float bestDistance = float.MaxValue;
            bool foundGround = false;

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null ||
                    hit.collider.GetComponentInParent<EnemyController>() == this ||
                    hit.normal.y <= 0.55f)
                {
                    continue;
                }

                if (hit.distance >= bestDistance)
                    continue;

                bestDistance = hit.distance;
                bestHit = hit;
                foundGround = true;
            }

            if (!foundGround)
                return false;

            Vector3 position = transform.position;
            position.y = bestHit.point.y + GetColliderBottomOffset();
            transform.position = position;

            return true;
        }

        private float GetGroundProbeUpOffset()
        {
            return Mathf.Max(0.5f, GetColliderBottomOffset() + 0.25f);
        }

        private float GetColliderBottomOffset()
        {
            CapsuleCollider capsule = GetComponent<CapsuleCollider>();

            if (capsule == null)
                return Mathf.Max(0.1f, _data.AgentHeight * 0.5f * transform.lossyScale.y);

            float scaledHeight = capsule.height * Mathf.Abs(transform.lossyScale.y);
            float scaledCenterY = capsule.center.y * transform.lossyScale.y;

            return Mathf.Max(0f, scaledHeight * 0.5f - scaledCenterY);
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

            if (_agent == null || _target == null || _isDead || _isGrabbed)
                return false;

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
            if (_agent.isOnNavMesh)
                return true;

            if (!TrySampleRecoveryNavMesh(out NavMeshHit hit))
            {
                return false;
            }

            return _agent.Warp(hit.position);
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

            _agent.speed = _data.MoveSpeed;
            _agent.acceleration = _data.AgentAcceleration;
            _agent.angularSpeed = _data.AgentAngularSpeed;
            _agent.stoppingDistance = _data.AgentStoppingDistance;
            _agent.radius = _data.AgentRadius;
            _agent.height = _data.AgentHeight;
            _agent.baseOffset = GetColliderBottomOffset();
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
            Vector3 horizontalVelocity = direction * _data.MoveSpeed;

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
