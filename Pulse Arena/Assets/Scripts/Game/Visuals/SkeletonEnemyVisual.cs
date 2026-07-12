using System;
using Data;
using UnityEngine;

namespace Game.Visuals
{
    /// <summary>
    ///     A skinned enemy visual (the PSX skeleton) driven by an <see cref="Animator" />: a Speed parameter
    ///     mirrors the enemy's actual world movement (Idle↔Run) and Death is a one-shot. Implements
    ///     <see cref="IEnemyVisual" /> so <c>EnemyController</c> swaps it in for the primitive blob without any
    ///     controller change. Speed is sampled from the ROOT's world-position delta — NOT
    ///     <c>Rigidbody.linearVelocity</c> — because a chasing enemy is moved by its NavMeshAgent (which drives
    ///     the transform, leaving rigidbody velocity ~0), so a velocity read would sit in Idle while it walks.
    ///     Movement anim is suppressed (Speed 0) while grabbed/thrown/dead so a spun-on-the-lasso enemy doesn't
    ///     read as "running". Death completes via an animation event on the death clip (<see cref="OnDeathAnimationEnd" />)
    ///     which fires <see cref="DeathCompleted" /> so the controller pools the corpse exactly when the clip
    ///     ends — no guessed timer; a Death-state <c>normalizedTime</c> check is the safety net, and a model with
    ///     no Death state signals next frame. The primitive's bespoke grabbed/thrown/attack/bounce squash has no
    ///     skeletal equivalent, so those are inert here (the enemy still flails physically via the Rigidbody).
    ///     Mechanical pause freezes the Animator (speed 0), so a paused death naturally waits. Trigger sets are
    ///     guarded so a controller missing a clip doesn't warn.
    /// </summary>
    public class SkeletonEnemyVisual : MonoBehaviour, IEnemyVisual
    {
        private const float MoveSpeedSmoothing = 12f; // how fast the sampled move-speed follows reality

        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int DeathHash = Animator.StringToHash("Death");
        private static readonly int IdleHash = Animator.StringToHash("Idle");
        private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        [SerializeField] private Animator _animator;
        [SerializeField] private Renderer _renderer;

        [Tooltip("Seconds from PlayAttack to the swing's contact frame (this model's strike moment). Negative = use the gameplay default.")]
        [SerializeField] private float _attackHitDelay = -1f;

        [Tooltip("Global Animator playback speed for this model (Idle/Run/Death). Attack is sped up per-state in the controller, not here.")]
        [SerializeField] private float _animationSpeed = 1f;
        private Vector3 _baseScale = Vector3.one;
        private Collider _bodyCollider;
        private bool _deathSignaled;
        private bool _isDead;
        private bool _isGrabbed;
        private bool _isThrown;
        private Vector3 _lastPosition;
        private float _moveSpeed;
        private bool _paused;
        private Rigidbody _rigidbody;

        public event Action DeathCompleted;

        public float AttackHitDelay => _attackHitDelay;

        public void Initialize(Rigidbody rigidbody, EnemyVisualData visualData)
        {
            _rigidbody = rigidbody;
            _bodyCollider = rigidbody != null ? rigidbody.GetComponent<Collider>() : null;
            _baseScale = transform.localScale;
            _lastPosition = MovementReferencePosition();

            if (_animator != null)
                _animator.speed = _animationSpeed;
        }

        private void Update()
        {
            if (_paused || _animator == null)
                return;

            if (_isDead)
            {
                TickDeathCompletion();
                return;
            }

            if (!HasParameter(SpeedHash))
                return;

            _animator.SetFloat(SpeedHash, SampleMoveSpeed(Time.deltaTime));
        }

        public void SetPaused(bool value)
        {
            _paused = value;

            if (_animator != null)
                _animator.speed = value ? 0f : _animationSpeed;
        }

        public void ApplyTypeStyle(EnemyTypeData type)
        {
            if (type == null)
                return;

            transform.localScale = _baseScale * Mathf.Max(0.1f, type.VisualScale);
        }

        public void PlayDeath()
        {
            _isDead = true;
            _deathSignaled = false;

            if (HasParameter(IsDeadHash))
                _animator.SetBool(IsDeadHash, true);

            if (HasParameter(DeathHash))
                _animator.SetTrigger(DeathHash);
        }

        // No skeletal equivalent — the primitive-only squash beat. The physical enemy still reacts via the Rigidbody.
        public void PlayGroundBounce()
        {
        }

        public void PlayHit()
        {
        }

        public void PlayAttack()
        {
            if (HasParameter(AttackHash))
                _animator.SetTrigger(AttackHash);
        }

        // Leave the attack clip the moment the attack state exits (crossfade to Idle; Speed then blends to Run) so
        // the Animator never runs the swing while the controller has resumed chasing. No-op if there's no Idle state.
        public void EndAttack()
        {
            if (_animator == null || _isDead)
                return;

            if (_animator.HasState(0, IdleHash))
                _animator.CrossFade(IdleHash, 0.12f);
        }

        // Animation-event target on the death clip's final frame — fires DeathCompleted the instant the clip ends.
        public void OnDeathAnimationEnd()
        {
            if (_isDead)
                RaiseDeathCompleted();
        }

        public void ResetState()
        {
            _isGrabbed = false;
            _isThrown = false;
            _isDead = false;
            _deathSignaled = false;
            _moveSpeed = 0f;
            _lastPosition = MovementReferencePosition();

            if (_animator == null)
                return;

            _animator.Rebind(); // pooled reuse: back to the default (Idle) state + default params
            _animator.Update(0f);
            _animator.speed = _animationSpeed;
        }

        public void SetGrabbed(bool isGrabbed)
        {
            _isGrabbed = isGrabbed;
        }

        public void SetThrown(bool isThrown)
        {
            _isThrown = isThrown;
        }

        public bool TryGetRopeBounds(out Bounds bounds)
        {
            // Wrap the physical body capsule, NOT the skinned mesh: a rigged mesh reports an inflated
            // arm-spread/bind-pose AABB (extents.x ~3× the torso), so the lasso would wrap far too wide. The
            // root collider is the torso size and is shared by every enemy model, so the rope hugs identically
            // whichever model spawned.
            if (_bodyCollider != null)
            {
                bounds = _bodyCollider.bounds;
                return true;
            }

            if (_renderer != null)
            {
                bounds = _renderer.bounds;
                return true;
            }

            bounds = default;
            return false;
        }

        // The death clip's animation event is the primary completion signal; this is the safety net — a model
        // with no Death state signals next frame (nothing to play), otherwise signal once the Death state finishes.
        private void TickDeathCompletion()
        {
            if (_deathSignaled)
                return;

            if (!HasParameter(DeathHash))
            {
                RaiseDeathCompleted();
                return;
            }

            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);

            if (state.IsName("Death") && !_animator.IsInTransition(0) && state.normalizedTime >= 1f)
                RaiseDeathCompleted();
        }

        private void RaiseDeathCompleted()
        {
            if (_deathSignaled)
                return;

            _deathSignaled = true;
            DeathCompleted?.Invoke();
        }

        // Actual planar movement from the root's world-position delta (agent- OR physics-driven, unlike
        // rigidbody.velocity). Forced to 0 while grabbed/thrown/dead so the spun/flung/dead enemy stays out of Run.
        private float SampleMoveSpeed(float deltaTime)
        {
            Vector3 current = MovementReferencePosition();
            Vector3 delta = current - _lastPosition;
            delta.y = 0f;
            _lastPosition = current;

            if (deltaTime <= 0f)
                return _moveSpeed;

            float instant = _isGrabbed || _isThrown || _isDead ? 0f : delta.magnitude / deltaTime;
            _moveSpeed = Mathf.Lerp(_moveSpeed, instant, deltaTime * MoveSpeedSmoothing);
            return _moveSpeed;
        }

        private Vector3 MovementReferencePosition()
        {
            return _rigidbody != null ? _rigidbody.transform.position : transform.position;
        }

        private bool HasParameter(int hash)
        {
            if (_animator == null)
                return false;

            AnimatorControllerParameter[] parameters = _animator.parameters;

            for (int i = 0; i < parameters.Length; i++)
                if (parameters[i].nameHash == hash)
                    return true;

            return false;
        }
    }
}
