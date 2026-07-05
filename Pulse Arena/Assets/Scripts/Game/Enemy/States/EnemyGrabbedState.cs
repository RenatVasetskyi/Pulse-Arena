using Game.Common.StateMachine;

namespace Game.Enemy.States
{
    public class EnemyGrabbedState : ActorState
    {
        private readonly EnemyController _enemy;

        public EnemyGrabbedState(EnemyController enemy)
        {
            _enemy = enemy;
        }

        public override void Enter()
        {
            _enemy.EnterGrabbedState();
        }

        public override void FixedTick()
        {
            _enemy.FixedTickGrabbedState();
        }
    }
}
