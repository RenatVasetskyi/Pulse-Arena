using Game.Common.StateMachine;

namespace Game.Enemy.States
{
    public class EnemyDeadState : ActorState
    {
        private readonly EnemyController _enemy;

        public EnemyDeadState(EnemyController enemy)
        {
            _enemy = enemy;
        }

        public override void Enter()
        {
            _enemy.EnterDeadState();
        }
    }
}
