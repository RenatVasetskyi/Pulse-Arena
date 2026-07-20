using System;
using Data;
using Game.Common;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Enemy
{
    /// <summary>
    ///     Owns the enemy's locomotion: the NavMeshAgent + Rigidbody hybrid. The controller keeps
    ///     game state (dead/grabbed/knocked) and gates when the agent may take over; this class
    ///     handles the "how" — placing the agent on the NavMesh, driving it, or the physics fallback.
    /// </summary>
    public class EnemyMovement
    {
        private NavMeshAgent _agent;
        private bool _agentWasStopped;
        private EnemyData _data;
        private float _destinationUpdateTimer;
        private GroundingData _grounding;
        private Func<float> _moveSpeed;
        private Vector3 _pausedAgentVelocity;
        private Rigidbody _rigidbody;
        private Transform _transform;

        public bool UsesAgent { get; private set; }

        /// <summary>
        ///     Mechanical pause: halt the NavMeshAgent in place. We use isStopped + cached velocity rather than
        ///     disabling it — disabling forces a Warp/re-place on re-enable (a position snap), so the enemy
        ///     would not resume from the same spot.
        /// </summary>
        public void Pause()
        {
            if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh)
                return;

            _pausedAgentVelocity = _agent.velocity;
            _agentWasStopped = _agent.isStopped;
            _agent.velocity = Vector3.zero;
            _agent.isStopped = true;
        }

        public void Resume()
        {
            if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh)
                return;

            _agent.isStopped = _agentWasStopped;
            _agent.velocity = _pausedAgentVelocity;
        }

        public void Initialize(Transform transform, Rigidbody rigidbody, NavMeshAgent agent,
            EnemyData data, Func<float> moveSpeedProvider, GroundingData grounding)
        {
            _transform = transform;
            _rigidbody = rigidbody;
            _agent = agent;
            _data = data;
            _moveSpeed = moveSpeedProvider;
            _grounding = grounding;
        }

        public void ConfigureAgent()
        {
            if (_agent == null || _data == null)
                return;

            _agent.speed = _moveSpeed();
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

        public void DisableAgent()
        {
            if (_agent != null && _agent.enabled)
            {
                if (_agent.isOnNavMesh)
                    _agent.ResetPath();

                _agent.enabled = false;
            }

            UsesAgent = false;

            if (_rigidbody != null)
                _rigidbody.isKinematic = false;
        }

        /// <summary>
        ///     Halts the enemy where it stands when the target is lost (the player died): stops the agent and
        ///     clears its path so it does not keep walking to the player's last position, and zeroes horizontal
        ///     rigidbody velocity for the physics-fallback case (vertical is kept so gravity still applies).
        ///     Idempotent — safe to call every FixedTick. The <c>isStopped</c> flag it sets is cleared again by
        ///     <see cref="TryEnableAgent" /> the next time the agent takes over, so a re-used pooled enemy (or a
        ///     re-acquired target) never spawns frozen.
        /// </summary>
        public void StopInPlace()
        {
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
            {
                if (_agent.hasPath)
                    _agent.ResetPath();

                _agent.velocity = Vector3.zero; // crisp halt — no autoBraking glide toward the dead player's last spot
                _agent.isStopped = true;
            }

            if (_rigidbody != null && !_rigidbody.isKinematic)
                _rigidbody.linearVelocity = new Vector3(0f, _rigidbody.linearVelocity.y, 0f);
        }

        public void MoveDirectlyToTarget(Transform target)
        {
            Vector3 offset = target.position - _transform.position;
            offset.y = 0f;

            if (offset.sqrMagnitude <= _data.AgentStoppingDistance * _data.AgentStoppingDistance)
            {
                _rigidbody.linearVelocity = new Vector3(0f, _rigidbody.linearVelocity.y, 0f);
                RotateTo(offset);
                return;
            }

            Vector3 direction = offset.normalized;
            direction.y = 0f;
            Vector3 horizontalVelocity = direction * _moveSpeed();

            _rigidbody.linearVelocity = new Vector3(
                horizontalVelocity.x,
                _rigidbody.linearVelocity.y,
                horizontalVelocity.z);

            RotateTo(direction);
        }

        public void MoveToTarget(Transform target)
        {
            if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh)
            {
                UsesAgent = false;
                return;
            }

            // Clear any halt a temporary StopInPlace left (e.g. the attack state stops the agent but keeps
            // UsesAgent, so TryEnableAgent — which normally clears isStopped — never runs on the chase resume).
            if (_agent.isStopped)
                _agent.isStopped = false;

            if (IsWithinStoppingDistance(target))
            {
                if (_agent.hasPath)
                    _agent.ResetPath();

                RotateTo(target.position - _transform.position);
                return;
            }

            UpdateDestination(target);

            RotateTo(_agent.desiredVelocity);
        }

        // Throttled NavMesh destination refresh: re-samples and re-targets only every DestinationUpdateInterval,
        // so a chasing enemy doesn't call SetDestination every physics step.
        private void UpdateDestination(Transform target)
        {
            _destinationUpdateTimer -= Time.fixedDeltaTime;

            if (_destinationUpdateTimer > 0f)
                return;

            Vector3 destination = target.position;

            if (NavMesh.SamplePosition(target.position, out NavMeshHit hit,
                    _data.NavMeshSampleDistance, NavMesh.AllAreas))
            {
                destination = hit.position;
            }

            _agent.SetDestination(destination);
            _destinationUpdateTimer = _data.DestinationUpdateInterval;
        }

        /// <summary>
        ///     Enables agent control. The controller must have already checked its own state
        ///     (not dead/grabbed/recovering/knocked) before calling this.
        /// </summary>
        public bool TryEnableAgent()
        {
            if (_agent == null)
                return false;

            if (!_agent.enabled)
                _agent.enabled = true;

            if (!TryPlaceAgentOnNavMesh())
            {
                _agent.enabled = false;
                UsesAgent = false;
                _rigidbody.isKinematic = false;
                return false;
            }

            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.isKinematic = true;
            _agent.isStopped = false; // clear any halt left by a lost-target StopInPlace before the agent drives again
            UsesAgent = true;
            _destinationUpdateTimer = 0f;

            return true;
        }

        /// <summary>Snap to face the target — the attack state commits its aim on entry, then holds it for the swing.</summary>
        public void FaceTarget(Transform target)
        {
            if (target == null || _transform == null)
                return;

            Vector3 offset = target.position - _transform.position;
            offset.y = 0f;

            if (offset.sqrMagnitude <= 0.01f)
                return;

            _transform.rotation = Quaternion.LookRotation(offset);
        }

        private void RotateTo(Vector3 direction)
        {
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.01f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            _transform.rotation = Quaternion.Lerp(_transform.rotation,
                targetRotation, _data.RotationSpeed * Time.fixedDeltaTime);
        }

        private bool IsWithinStoppingDistance(Transform target)
        {
            Vector3 offset = target.position - _transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= _data.AgentStoppingDistance * _data.AgentStoppingDistance;
        }

        private bool TryPlaceAgentOnNavMesh()
        {
            if (!ActorGroundingUtility.TryGetGroundedPosition(_transform, _grounding, _data.GroundRecoveryProbeDistance,
                    0.02f, out Vector3 groundedPosition))
            {
                return false;
            }

            _transform.position = groundedPosition;

            if (!TrySampleRecoveryNavMesh(out NavMeshHit hit))
            {
                return _agent.isOnNavMesh;
            }

            bool warped = _agent.Warp(hit.position);

            if (warped)
                _transform.position = groundedPosition;

            return warped || _agent.isOnNavMesh;
        }

        private bool TrySampleRecoveryNavMesh(out NavMeshHit hit)
        {
            float sampleDistance = Mathf.Max(_data.NavMeshSampleDistance, _data.GroundRecoveryProbeDistance);
            return NavMesh.SamplePosition(_transform.position, out hit, sampleDistance, NavMesh.AllAreas);
        }
    }
}