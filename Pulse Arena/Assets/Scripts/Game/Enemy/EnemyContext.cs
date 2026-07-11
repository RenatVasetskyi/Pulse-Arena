using System;
using Data;
using Game.Player;
using Game.Visuals;
using UnityEngine;

namespace Game.Enemy
{
    /// <summary>
    ///     Lean handle the enemy states use to reach their collaborators. It holds DIRECT references to the
    ///     helpers the states drive (rigidbody, transform, data, movement, visual, timers, ground recovery,
    ///     impact, collision handler), owns the small SHARED mutable flags the states flip
    ///     (<see cref="IsImpactProjectile" />, <see cref="NeedsGroundRecovery" />, <see cref="IsGrabbed" />) so
    ///     they read/write them without controller accessors, and forwards the FEW genuine controller
    ///     callbacks the states truly need — the target reads, the <c>IsDead</c> read, the state transitions,
    ///     the pool release, and the death-return coroutine trigger (which must stay on the MonoBehaviour).
    ///     Everything else — health, scoring, the public API — stays private to <see cref="EnemyController" />.
    ///     The states read timers directly through <c>Timers.Knockback.Remaining</c> etc. and flags through
    ///     <c>IsGrabbed</c> etc.; there are no per-timer/per-flag façade accessors any more.
    /// </summary>
    public sealed class EnemyContext
    {
        private readonly Action _changeToChase;
        private readonly Action _changeToGroundRecovery;
        private readonly Func<bool> _isDead;
        private readonly Func<PlayerController> _playerTarget;
        private readonly Action _resolveRingout;
        private readonly Action _returnToPool;
        private readonly Action _startDeathReturn;
        private readonly Action _stopForDeath;
        private readonly Func<Transform> _target;

        private readonly Func<EnemyTypeData> _typeData;

        // --- small SHARED mutable flags the states flip (held on the context itself) ---
        public bool IsGrabbed;
        public bool IsImpactProjectile;
        public bool NeedsGroundRecovery;

        // Set by FallIntoPit: when it has a value the ringout sinks the enemy straight down into this maw
        // (centering + descending at PitSinkSpeed) instead of tumbling off an arena edge. Null = edge ringout.
        public Vector3? PitSinkCenter;
        public float PitSinkSpeed;
        public EnemyCollisionHandler Collisions { get; }
        public EnemyData Data { get; }
        public GroundRecoveryController GroundRecovery { get; }
        public EnemyImpact Impact { get; }
        public bool IsDead => _isDead();
        public EnemyMovement Movement { get; }
        public PlayerController PlayerTarget => _playerTarget();

        // --- direct collaborator references the states drive ---
        public Rigidbody Rigidbody { get; }

        // --- the few genuine controller reads the states need ---
        public Transform Target => _target();
        public EnemyTimers Timers { get; }
        public Transform Transform { get; }
        public EnemyTypeData TypeData => _typeData();
        public EnemyPrimitiveVisual Visual { get; }

        public EnemyContext(
            Rigidbody rigidbody,
            Transform transform,
            EnemyData data,
            EnemyMovement movement,
            EnemyPrimitiveVisual visual,
            EnemyTimers timers,
            GroundRecoveryController groundRecovery,
            EnemyImpact impact,
            EnemyCollisionHandler collisions,
            Func<Transform> target,
            Func<PlayerController> playerTarget,
            Func<bool> isDead,
            Func<EnemyTypeData> typeData,
            Action changeToChase,
            Action changeToGroundRecovery,
            Action returnToPool,
            Action startDeathReturn,
            Action stopForDeath,
            Action resolveRingout)
        {
            Rigidbody = rigidbody;
            Transform = transform;
            Data = data;
            Movement = movement;
            Visual = visual;
            Timers = timers;
            GroundRecovery = groundRecovery;
            Impact = impact;
            Collisions = collisions;
            _target = target;
            _playerTarget = playerTarget;
            _isDead = isDead;
            _typeData = typeData;
            _changeToChase = changeToChase;
            _changeToGroundRecovery = changeToGroundRecovery;
            _returnToPool = returnToPool;
            _startDeathReturn = startDeathReturn;
            _stopForDeath = stopForDeath;
            _resolveRingout = resolveRingout;
        }

        // --- tiny physics/damage helpers operating purely over context-held collaborators ---

        /// <summary>Adds the extra downward acceleration so airborne enemies fall faster than default gravity.</summary>
        public void ApplyExtraGravity() =>
            Game.Common.ActorPhysicsUtility.ApplyExtraGravity(Rigidbody, Data.ExtraGravity);

        // --- state routing / pool / death (the transitions the states trigger) ---
        public void ChangeToChaseState() => _changeToChase();
        public void ChangeToGroundRecoveryState() => _changeToGroundRecovery();

        /// <summary>
        ///     The controller-coupled half of the old EnterRingoutState body, in order: mark the controller
        ///     dead, zero the health bar, then run the RingoutHandler (award kill once, sting, feedback burst).
        ///     The physics/flag/timer half stays in the state Enter.
        /// </summary>
        public void ResolveRingout() => _resolveRingout();

        public void ReturnToPool() => _returnToPool();
        public void StartDeathReturn() => _startDeathReturn();

        /// <summary>
        ///     The old StopForDeath body (controller-owned): disable the agent, clear the shared flags, clear
        ///     the knockback/stasis timers and freeze the rigidbody. Kept as a controller callback because it
        ///     flips the same shared state the controller's lifecycle methods do — a single source of truth.
        /// </summary>
        public void StopForDeath() => _stopForDeath();

        /// <summary>The sweep-along-trajectory impact-damage tick (drives the collision handler's sweep).</summary>
        public void SweepImpactDamage() => Collisions.SweepImpactDamage();

        /// <summary>
        ///     Gate + hand off agent control (the old controller TryEnableAgentControl): if the agent is
        ///     already driving, keep it; otherwise refuse while the enemy has no target, is dead, grabbed,
        ///     pending ground recovery, or still under knockback, and try to place the agent on the mesh.
        /// </summary>
        public bool TryEnableAgentControl()
        {
            if (Movement.UsesAgent)
                return true;

            if (Target == null || IsDead || IsGrabbed ||
                NeedsGroundRecovery || Timers.Knockback.Remaining > 0f)
            {
                return false;
            }

            return Movement.TryEnableAgent();
        }
    }
}