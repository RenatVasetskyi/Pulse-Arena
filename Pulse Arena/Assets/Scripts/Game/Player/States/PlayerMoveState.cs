using Game.Common.StateMachine;

namespace Game.Player.States
{
    public class PlayerMoveState : ActorState
    {
        private readonly PlayerController _player;

        public PlayerMoveState(PlayerController player)
        {
            _player = player;
        }

        public override void FixedTick()
        {
            _player.MoveByInput();
            _player.ApplyExtraGravity();
        }

        public override void Tick()
        {
            _player.RotateToInput();
        }
    }
}