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
        // ~1000 units from origin — far past any legit arena spot. Catches an enemy flung to infinity (hard
        // launch / physics blow-up) so it rings out instead of drifting forever and spamming Renderer warnings.
        private const float MaxArenaSqrDistance = 1_000_000f;

        private readonly Action _changeToAttack;
        private readonly Action _changeToChase;
        private readonly Action _changeToFlip;
        private readonly Action _changeToGroundRecovery;
        private readonly Action _changeToIdle;
        private readonly Action _changeToTaunt;
        private readonly Game.Common.HitFlash _hitFlash;
        private readonly Func<bool> _isDead;
        private readonly Func<PlayerController> _playerTarget;
        private readonly Action _resolveRingout;
        private readonly Action _returnToPool;
        private readonly float _ringoutHeight;
        private readonly Action _startDeathReturn;
        private readonly Action _startRingout;
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
        public RingoutHandler Ringout { get; }

        // --- direct collaborator references the states drive ---
        public Rigidbody Rigidbody { get; }

        // --- the few genuine controller reads the states need ---
        public Transform Target => _target();
        public EnemyTimers Timers { get; }
        public Transform Transform { get; }
        public EnemyTypeData TypeData => _typeData();
        public IEnemyVisual Visual { get; }

        public EnemyContext(
            Rigidbody rigidbody,
            Transform transform,
            EnemyData data,
            EnemyMovement movement,
            IEnemyVisual visual,
            EnemyTimers timers,
            GroundRecoveryController groundRecovery,
            EnemyImpact impact,
            EnemyCollisionHandler collisions,
            RingoutHandler ringout,
            Game.Common.HitFlash hitFlash,
            float ringoutHeight,
            Func<Transform> target,
            Func<PlayerController> playerTarget,
            Func<bool> isDead,
            Func<EnemyTypeData> typeData,
            Action changeToIdle,
            Action changeToAttack,
            Action changeToChase,
            Action changeToGroundRecovery,
            Action changeToTaunt,
            Action changeToFlip,
            Action returnToPool,
            Action startRingout,
            Action startDeathReturn,
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
            Ringout = ringout;
            _hitFlash = hitFlash;
            _ringoutHeight = ringoutHeight;
            _target = target;
            _playerTarget = playerTarget;
            _isDead = isDead;
            _typeData = typeData;
            _changeToIdle = changeToIdle;
            _changeToAttack = changeToAttack;
            _changeToChase = changeToChase;
            _changeToGroundRecovery = changeToGroundRecovery;
            _changeToTaunt = changeToTaunt;
            _changeToFlip = changeToFlip;
            _returnToPool = returnToPool;
            _startRingout = startRingout;
            _startDeathReturn = startDeathReturn;
            _resolveRingout = resolveRingout;
        }

        // --- actor-wide per-frame work every GAMEPLAY state runs (the pause + dead + ringout states don't, so it
        //     freezes under pause and stops once dead for free) ---

        /// <summary>Frame tick: advance the hit-flash fade.</summary>
        public void TickCommon() => _hitFlash.Tick(Time.deltaTime);

        /// <summary>Physics tick: ring out if flung off the arena, then advance the shared timers.</summary>
        public void FixedTickCommon()
        {
            HandleOutOfBounds();
            Timers.TickFixed(Time.fixedDeltaTime);
        }

        // Flung below the ring-out floor or hurled to infinity → ring out (a NaN/Inf blow-up drops straight to the
        // pool, since it can't be rung out sanely).
        private void HandleOutOfBounds()
        {
            Vector3 position = Transform.position;

            if (!Game.Common.ActorPhysicsUtility.IsFinite(position))
            {
                _returnToPool();
                return;
            }

            bool belowFloor = position.y < _ringoutHeight;
            bool tooFarOut = new Vector2(position.x, position.z).sqrMagnitude > MaxArenaSqrDistance;

            if (belowFloor || tooFarOut)
                _startRingout();
        }

        // --- tiny physics/damage helpers operating purely over context-held collaborators ---

        /// <summary>Adds the extra downward acceleration so airborne enemies fall faster than default gravity.</summary>
        public void ApplyExtraGravity() =>
            Game.Common.ActorPhysicsUtility.ApplyExtraGravity(Rigidbody, Data.ExtraGravity);

        // --- state routing / pool / death (the transitions the states trigger) ---
        public void ChangeToIdleState() => _changeToIdle();
        public void ChangeToAttackState() => _changeToAttack();
        public void ChangeToChaseState() => _changeToChase();
        public void ChangeToGroundRecoveryState() => _changeToGroundRecovery();
        public void ChangeToTauntState() => _changeToTaunt();
        public void ChangeToFlipState() => _changeToFlip();

        /// <summary>
        ///     The controller-coupled half of the old EnterRingoutState body, in order: mark the controller
        ///     dead, zero the health bar, then run the RingoutHandler (award kill once, sting, feedback burst).
        ///     The physics/flag/timer half stays in the state Enter.
        /// </summary>
        public void ResolveRingout() => _resolveRingout();

        public void ReturnToPool() => _returnToPool();
        public void StartDeathReturn() => _startDeathReturn();

        /// <summary>Reset the small shared flags to their spawned defaults (states + the controller's pool reset share this).</summary>
        public void ClearFlags()
        {
            IsGrabbed = false;
            IsImpactProjectile = false;
            NeedsGroundRecovery = false;
            PitSinkCenter = null;
        }

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

        // --- shared pursue behaviour (the chase + flip states both drive with these) ---

        /// <summary>Drive toward the current target: NavMeshAgent when it can be placed on the mesh, physics fallback otherwise.</summary>
        public void DriveToTarget()
        {
            if (TryEnableAgentControl())
            {
                Movement.MoveToTarget(Target);
            }
            else
            {
                ApplyExtraGravity();
                Movement.MoveDirectlyToTarget(Target);
            }
        }

        /// <summary>The player is alive, in range, and the attack is off cooldown — the chase/flip may commit to a swing.</summary>
        public bool CanAttack()
        {
            return PlayerTarget != null && Timers.AttackCooldown.Remaining <= 0f && IsInAttackRange();
        }

        private bool IsInAttackRange()
        {
            if (PlayerTarget == null)
                return false;

            Vector3 offset = PlayerTarget.transform.position - Transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= Data.AttackRange * Data.AttackRange;
        }
    }
}