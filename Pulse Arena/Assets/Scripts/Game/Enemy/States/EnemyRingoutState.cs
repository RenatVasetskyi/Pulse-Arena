using Game.Common.StateMachine;

namespace Game.Enemy.States
{
    public class EnemyRingoutState : ActorState
    {
        private readonly EnemyController _enemy;

        public EnemyRingoutState(EnemyController enemy)
        {
            _enemy = enemy;
        }

        public override void Enter()
        {
            _enemy.EnterRingoutState();
        }

        public override void FixedTick()
        {
            _enemy.FixedTickRingoutState();
        }
    }
}
