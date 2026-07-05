using Game.Common.StateMachine;

namespace Game.Enemy.States
{
    public class EnemyChaseState : ActorState
    {
        private readonly EnemyController _enemy;

        public EnemyChaseState(EnemyController enemy)
        {
            _enemy = enemy;
        }

        public override void Enter()
        {
            _enemy.EnterChaseState();
        }

        public override void FixedTick()
        {
            _enemy.FixedTickChaseState();
        }
    }
}
