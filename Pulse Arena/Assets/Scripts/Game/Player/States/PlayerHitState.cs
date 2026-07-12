using UnityEngine;

namespace Game.Player.States
{
    /// <summary>
    ///     Brief hitstun after taking damage: plays the hit reaction on entry, rides out the knockback (facing
    ///     stays steerable, and a dash can still cancel it), then returns to Idle/Run. The controller applies the
    ///     flash + knockback impulse before the transition; this state owns the timed recovery + the hit clip.
    /// </summary>
    public class PlayerHitState : PlayerActiveState
    {
        private float _knockbackTimer;

        public PlayerHitState(PlayerContext context) : base(context)
        {
        }

        public override void Enter()
        {
            _knockbackTimer = Context.Data.HitKnockbackDuration;
            Context.Visual?.PlayHit();
        }

        protected override void OnFixedTick()
        {
            Context.Movement.ApplyExtraGravity();

            _knockbackTimer -= Time.fixedDeltaTime;

            if (_knockbackTimer <= 0f)
                ReturnToLocomotion();
        }

        protected override void OnTick()
        {
            if (Context.TryStartDash())
                return;

            Context.Movement.RotateToInput();
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
