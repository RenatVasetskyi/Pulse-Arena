using Game.Common.StateMachine;
using UnityEngine;

namespace Game.Player.States
{
    /// <summary>
    ///     A short burst dash in a fixed direction. Movement is locked to the dash vector for its duration
    ///     (no steering), and the player is invulnerable for the dash — so it doubles as a dodge. Returns to
    ///     the move state when the timer runs out.
    /// </summary>
    public class PlayerDashState : ActorState
    {
        private readonly PlayerController _player;
        private float _timer;

        public PlayerDashState(PlayerController player)
        {
            _player = player;
        }

        public override void Enter()
        {
            _timer = _player.Data.DashDuration;
            _player.FaceDashDirection();
            _player.SetDashTrail(true);
        }

        public override void Exit()
        {
            _player.SetDashTrail(false);
        }

        public override void FixedTick()
        {
            _player.ApplyDashVelocity();
            _player.ApplyExtraGravity();

            _timer -= Time.fixedDeltaTime;

            if (_timer <= 0f)
                _player.ChangeToMoveState();
        }
    }
}