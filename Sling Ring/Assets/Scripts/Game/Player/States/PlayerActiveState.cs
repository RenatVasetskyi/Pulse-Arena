using Game.Common.StateMachine;

namespace Game.Player.States
{
    /// <summary>
    ///     Base for every gameplay player state (Idle/Run/Dash/Hit): wraps each state's hooks with the actor-wide
    ///     per-frame work — <see cref="PlayerContext.TickCommon" /> (i-frame/dash-cooldown/hit-flash countdowns +
    ///     ring-out) and the physics-spin kill <see cref="PlayerContext.FixedTickCommon" /> — so the controller
    ///     ticks nothing, and the non-deriving pause + dead states freeze that work for free.
    /// </summary>
    public abstract class PlayerActiveState : ActorState
    {
        protected readonly PlayerContext Context;

        protected PlayerActiveState(PlayerContext context)
        {
            Context = context;
        }

        public sealed override void Tick()
        {
            Context.TickCommon();
            OnTick();
        }

        public sealed override void FixedTick()
        {
            Context.FixedTickCommon();
            OnFixedTick();
        }

        protected virtual void OnTick()
        {
        }

        protected virtual void OnFixedTick()
        {
        }
    }
}
