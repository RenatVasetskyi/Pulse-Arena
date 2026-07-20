namespace Game.Common.StateMachine
{
    public interface IActorState
    {
        void Enter();
        void Exit();
        void FixedTick();
        void Tick();
    }
}