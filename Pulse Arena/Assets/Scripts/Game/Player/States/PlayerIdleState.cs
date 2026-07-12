namespace Game.Player.States
{
    /// <summary>
    ///     Grounded and still: zeroes horizontal velocity each step (so a shove doesn't slide the player) while
    ///     gravity keeps it planted, no facing change. Switches to <see cref="PlayerRunState" /> on move input, or
    ///     to the dash on a dash press. The Idle↔Run animation is the visual's speed blend, so this drives no clip.
    /// </summary>
    public class PlayerIdleState : PlayerActiveState
    {
        public PlayerIdleState(PlayerContext context) : base(context)
        {
        }

        protected override void OnFixedTick()
        {
            Context.Movement.MoveByInput(); // no input → zeroes horizontal, so the player holds its ground
            Context.Movement.ApplyExtraGravity();
        }

        protected override void OnTick()
        {
            if (Context.TryStartDash())
                return;

            if (Context.HasMoveInput)
                Context.ChangeToRunState();
        }
    }
}
