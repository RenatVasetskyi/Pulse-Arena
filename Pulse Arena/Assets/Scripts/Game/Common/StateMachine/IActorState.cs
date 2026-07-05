namespace Game.Common.StateMachine
{
    public interface IActorState
    {
        void Enter();
        void Exit();
        void Tick();
        void FixedTick();
    }
}
