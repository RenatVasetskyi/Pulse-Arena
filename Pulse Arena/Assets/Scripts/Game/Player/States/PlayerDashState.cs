using UnityEngine;

namespace Game.Player.States
{
    /// <summary>
    ///     A short fixed-direction dash / dodge: movement is locked to the dash vector (no steering, no dash-input)
    ///     and the player is invulnerable for its duration. Enter turns on the dash animation + trail and commits
    ///     the facing; on timeout it hands back to Idle/Run depending on whether input is held.
    /// </summary>
    public class PlayerDashState : PlayerActiveState
    {
        private float _timer;

        public PlayerDashState(PlayerContext context) : base(context)
        {
        }

        public override void Enter()
        {
            _timer = Context.Data.DashDuration;
            Context.Dash.FaceDashDirection();
            Context.Dash.SetTrail(true);
            Context.Visual?.PlayDash(Context.Data.DashDuration);
        }

        public override void Exit()
        {
            Context.Dash.SetTrail(false);
        }

        protected override void OnFixedTick()
        {
            Context.Dash.ApplyDashVelocity();
            Context.Movement.ApplyExtraGravity();

            _timer -= Time.fixedDeltaTime;

            if (_timer <= 0f)
                ReturnToLocomotion();
        }

        private void ReturnToLocomotion()
        {
            if (Context.HasMoveInput)
                Context.ChangeToRunState();
            else
                Context.ChangeToIdleState();
        }
    }
}
