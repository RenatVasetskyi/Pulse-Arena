using Architecture.Services.Interfaces;
using Data;
using UnityEngine;

namespace Game.Player.Interfaces
{
    /// <summary>
    ///     The player's Rigidbody locomotion: input-driven horizontal velocity, rotate-to-input facing, the shared
    ///     extra-gravity pull, and the hit knockback impulse. Owns the "how it moves"; the states own the "when".
    /// </summary>
    public interface IPlayerMovement
    {
        void Initialize(Transform transform, Rigidbody rigidbody, PlayerData data, IInputService input);
        void ApplyExtraGravity();
        void ApplyKnockback(Vector3 sourcePosition, float force);
        void KillAngularVelocity();
        void MoveByInput();
        void RotateToInput();
        void Stop();
    }
}