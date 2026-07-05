namespace Game.Common.StateMachine
{
    public abstract class ActorState : IActorState
    {
        public virtual void Enter()
        {
        }

        public virtual void Exit()
        {
        }

        public virtual void Tick()
        {
        }

        public virtual void FixedTick()
        {
        }
    }
}
