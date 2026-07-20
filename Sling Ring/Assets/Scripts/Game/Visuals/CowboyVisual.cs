using System.Collections.Generic;
using Data;
using Game.Combat;
using UnityEngine;

namespace Game.Visuals
{
    /// <summary>
    ///     The player's skinned cowboy visual: drives a Humanoid <see cref="Animator" /> from gameplay — a <c>Speed</c>
    ///     parameter mirrors planar velocity (Idle↔Run) and Hit/Dash/Death are one-shots. Implements
    ///     <see cref="IPlayerVisual" /> so <c>PlayerController</c> swaps it in for the primitive. Mechanical pause
    ///     freezes the Animator (speed 0), never <c>Time.timeScale</c>; trigger sets are guarded against missing clips.
    /// </summary>
    public class CowboyVisual : MonoBehaviour, IPlayerVisual
    {
        private static readonly int DashHash = Animator.StringToHash("Dash");
        private static readonly int DashMultHash = Animator.StringToHash("DashMult");
        private static readonly int DeathHash = Animator.StringToHash("Death");
        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
        private static readonly int MoveMultHash = Animator.StringToHash("MoveMult");
        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        [SerializeField] private Animator _animator;
        [SerializeField] private float _speedDamp = 0.03f;
        [SerializeField] private float _animationSpeed = 1.4f;
        [SerializeField] private float _dashClipLength = 1.17f;
        private readonly HashSet<int> _parameterHashes = new();
        private bool _parametersCached;
        private bool _paused;
        private Rigidbody _rigidbody;
        private PlayerVisualData _visualData = new();

        public void Initialize(Rigidbody rigidbody, EnemySlingshot slingshot, PlayerVisualData visualData)
        {
            _rigidbody = rigidbody;

            if (visualData != null)
                _visualData = visualData;

            if (_animator != null)
                _animator.speed = _animationSpeed;
        }

        private void Update()
        {
            if (_paused || _animator == null || _rigidbody == null)
                return;

            Vector3 planar = _rigidbody.linearVelocity;
            planar.y = 0f;
            float speed = planar.magnitude < _visualData.MoveThreshold ? 0f : planar.magnitude;

            // Velocity is SET not accelerated, so there's nothing to smooth here; the Idle↔Run transition blend
            // softens the swap, and damping the parameter only lags the run→idle switch.
            if (_speedDamp > 0.001f)
                _animator.SetFloat(SpeedHash, speed, _speedDamp, Time.deltaTime);
            else
                _animator.SetFloat(SpeedHash, speed);

            // Scale run playback by stick push (velocity): light push walks, full push runs. Drives the Run state's
            // Speed-Multiplier parameter; clamped so it never freezes or blurs.
            float moveMult = _visualData.MoveAnimationMaxSpeed > 0.01f
                ? Mathf.Clamp(speed / _visualData.MoveAnimationMaxSpeed, 0.6f, 1.15f)
                : 1f;
            _animator.SetFloat(MoveMultHash, moveMult);
        }

        /// <summary>Mechanical pause: freeze the Animator on the current frame (never <c>Time.timeScale</c>).</summary>
        public void SetPaused(bool value)
        {
            _paused = value;

            if (_animator != null)
                _animator.speed = value ? 0f : _animationSpeed;
        }

        public void PlayHit()
        {
            if (HasParameter(HitHash))
                _animator.SetTrigger(HitHash);
        }

        public void PlayDash(float dashDuration)
        {
            if (_animator == null)
                return;

            // Stretch/squash the roll clip so it plays over EXACTLY the dash duration, via the Dash state's
            // Speed-Multiplier parameter. _animationSpeed is factored out since it also scales playback.
            float dur = Mathf.Max(0.05f, dashDuration);
            _animator.SetFloat(DashMultHash, _dashClipLength / (dur * Mathf.Max(0.01f, _animationSpeed)));

            if (HasParameter(DashHash))
                _animator.SetTrigger(DashHash);
        }

        public void PlayDeath()
        {
            if (HasParameter(IsDeadHash))
                _animator.SetBool(IsDeadHash, true);

            if (HasParameter(DeathHash))
                _animator.SetTrigger(DeathHash);
        }

        private bool HasParameter(int hash)
        {
            EnsureParameterCache();

            return _parameterHashes.Contains(hash);
        }

        // Animator.parameters marshals a FRESH array on every read (the allocation trap that cost the enemy visual
        // its GC budget). The parameter set is fixed at author time, so snapshot the hashes once.
        private void EnsureParameterCache()
        {
            if (_parametersCached || _animator == null)
                return;

            AnimatorControllerParameter[] parameters = _animator.parameters;

            for (int i = 0; i < parameters.Length; i++)
                _parameterHashes.Add(parameters[i].nameHash);

            _parametersCached = true;
        }
    }
}
