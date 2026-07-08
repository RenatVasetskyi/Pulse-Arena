using Architecture.Services.Interfaces;
using Data;
using Game.Player.Interfaces;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// The dash / dodge feature: a short fixed-direction Rigidbody burst on a cooldown, with a trail. Owns the
    /// dash direction + cooldown; the controller grants the dodge i-frames and drives the dash state.
    /// </summary>
    public class PlayerDash : IPlayerDash
    {
        private Transform _transform;
        private Rigidbody _rigidbody;
        private PlayerData _data;
        private IInputService _input;
        private TrailRenderer _trail;
        private float _cooldownTimer;
        private Vector3 _direction;

        public bool IsReady => _cooldownTimer <= 0f;

        public float Charge01 => _data != null && _data.DashCooldown > 0f
            ? 1f - Mathf.Clamp01(_cooldownTimer / _data.DashCooldown)
            : 1f;

        public void Initialize(Transform transform, Rigidbody rigidbody, PlayerData data,
            IInputService input, TrailRenderer trail)
        {
            _transform = transform;
            _rigidbody = rigidbody;
            _data = data;
            _input = input;
            _trail = trail;
            SetTrail(false);
        }

        public bool WantsDash()
        {
            return _input.IsDashPressedThisFrame;
        }

        public void Begin()
        {
            Vector2 input = _input.MoveDirection;
            Vector3 direction = new Vector3(input.x, 0f, input.y);

            if (direction.sqrMagnitude < 0.01f)
                direction = _transform.forward;

            _direction = direction.normalized;
            _cooldownTimer = _data.DashCooldown;
        }

        public void ApplyDashVelocity()
        {
            _rigidbody.linearVelocity = new Vector3(
                _direction.x * _data.DashSpeed,
                _rigidbody.linearVelocity.y,
                _direction.z * _data.DashSpeed);
        }

        public void FaceDashDirection()
        {
            if (_direction.sqrMagnitude > 0.01f)
                _transform.rotation = Quaternion.LookRotation(_direction);
        }

        public void SetTrail(bool active)
        {
            if (_trail == null)
                return;

            if (active)
                _trail.Clear();

            _trail.emitting = active;
        }

        public void Tick(float deltaTime)
        {
            if (_cooldownTimer > 0f)
                _cooldownTimer -= deltaTime;
        }
    }
}
