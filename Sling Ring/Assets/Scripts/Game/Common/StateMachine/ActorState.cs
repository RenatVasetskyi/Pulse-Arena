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

        public virtual void FixedTick()
        {
        }

        public virtual void Tick()
        {
        }
    }
}