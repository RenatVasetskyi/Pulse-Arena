using Architecture.Services.Interfaces;
using Data;
using UnityEngine;

namespace Game.Player.Interfaces
{
    /// <summary>
    ///     The dash / dodge feature: a short fixed-direction Rigidbody burst on a cooldown. Owns the dash direction,
    ///     cooldown and trail. The controller grants the dodge i-frames (via <see cref="Game.Common.Interfaces.IActorHealth" />) and drives
    ///     the dash state; this class only knows "how to dash".
    /// </summary>
    public interface IPlayerDash
    {
        /// <summary>Dash readiness for the HUD: 0 just after a dash, filling to 1 when the cooldown is up.</summary>
        float Charge01 { get; }

        bool IsReady { get; }
        void ApplyDashVelocity();
        void Begin();
        void FaceDashDirection();

        void Initialize(Transform transform, Rigidbody rigidbody, PlayerData data, IInputService input,
            TrailRenderer trail);

        void SetTrail(bool active);
        void Tick(float deltaTime);
        bool WantsDash();
    }
}