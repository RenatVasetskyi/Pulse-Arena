using Game.Common.StateMachine;

namespace Game.Enemy.States
{
    public class EnemyPhysicsRecoveryState : ActorState
    {
        private readonly EnemyController _enemy;

        public EnemyPhysicsRecoveryState(EnemyController enemy)
        {
            _enemy = enemy;
        }

        public override void Enter()
        {
            _enemy.EnterPhysicsRecoveryState();
        }

        public override void FixedTick()
        {
            _enemy.FixedTickPhysicsRecoveryState();
        }
    }
}
