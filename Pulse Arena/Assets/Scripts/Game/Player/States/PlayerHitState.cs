using Game.Common.StateMachine;
using UnityEngine;

namespace Game.Player.States
{
    public class PlayerHitState : ActorState
    {
        private readonly PlayerController _player;
        private float _knockbackTimer;

        public PlayerHitState(PlayerController player)
        {
            _player = player;
        }

        public override void Enter()
        {
            _knockbackTimer = _player.Data.HitKnockbackDuration;
        }

        public override void Tick()
        {
            _player.RotateToInput();
        }

        public override void FixedTick()
        {
            _player.ApplyExtraGravity();

            _knockbackTimer -= Time.fixedDeltaTime;

            if (_knockbackTimer <= 0f)
                _player.ChangeToMoveState();
        }
    }
}
