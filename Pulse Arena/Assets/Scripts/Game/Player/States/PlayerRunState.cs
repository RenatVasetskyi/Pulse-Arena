namespace Game.Player.States
{
    /// <summary>
    ///     Locomotion: drive the Rigidbody by input and face the movement direction. Drops to
    ///     <see cref="PlayerIdleState" /> when input stops, or to the dash on a dash press. The Idle↔Run animation
    ///     is the visual's speed blend, so this drives no clip.
    /// </summary>
    public class PlayerRunState : PlayerActiveState
    {
        public PlayerRunState(PlayerContext context) : base(context)
        {
        }

        protected override void OnFixedTick()
        {
            Context.Movement.MoveByInput();
            Context.Movement.ApplyExtraGravity();
        }

        protected override void OnTick()
        {
            if (Context.TryStartDash())
                return;

            Context.Movement.RotateToInput();

            if (!Context.HasMoveInput)
                Context.ChangeToIdleState();
        }
    }
}
