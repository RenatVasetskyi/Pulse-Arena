using Architecture.Services.Interfaces;
using Data;
using Game.Common;
using Game.Player.Interfaces;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    ///     The player's Rigidbody locomotion: input-driven horizontal velocity, rotate-to-input facing, the shared
    ///     extra-gravity pull, and the hit knockback impulse. Mirrors <see cref="Game.Enemy.EnemyMovement" />.
    /// </summary>
    public class PlayerMovement : IPlayerMovement
    {
        private PlayerData _data;
        private IInputService _input;
        private Rigidbody _rigidbody;
        private Transform _transform;

        public void Initialize(Transform transform, Rigidbody rigidbody, PlayerData data, IInputService input)
        {
            _transform = transform;
            _rigidbody = rigidbody;
            _data = data;
            _input = input;
        }

        public void ApplyExtraGravity()
        {
            ActorPhysicsUtility.ApplyExtraGravity(_rigidbody, _data.ExtraGravity);
        }

        public void ApplyKnockback(Vector3 sourcePosition, float force)
        {
            Vector3 direction = _transform.position - sourcePosition;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.001f)
                direction = -_transform.forward;

            _rigidbody.AddForce(direction.normalized * force, ForceMode.VelocityChange);
        }

        public void KillAngularVelocity()
        {
            if (_rigidbody != null)
                _rigidbody.angularVelocity = Vector3.zero;
        }

        public void MoveByInput()
        {
            Vector2 input = _input.MoveDirection;
            Vector3 direction = new Vector3(input.x, 0f, input.y);
            Vector3 horizontalVelocity = direction * _data.MoveSpeed;

            _rigidbody.linearVelocity = new Vector3(
                horizontalVelocity.x,
                _rigidbody.linearVelocity.y,
                horizontalVelocity.z);
        }

        // Rotate through the Rigidbody (MoveRotation), NOT transform.rotation, and from FixedUpdate: with the body
        // interpolating, setting transform.rotation directly each frame fights the interpolation and jerks on turns.
        public void RotateToInput()
        {
            Vector2 input = _input.MoveDirection;

            if (input.sqrMagnitude <= 0.01f)
                return;

            Vector3 direction = new Vector3(input.x, 0f, input.y);
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            _rigidbody.MoveRotation(Quaternion.RotateTowards(_rigidbody.rotation,
                targetRotation, _data.RotationSpeed * Time.fixedDeltaTime));
        }

        public void Stop()
        {
            if (_rigidbody == null)
                return;

            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }
    }
}