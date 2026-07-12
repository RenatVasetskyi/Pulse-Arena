using Game.Common.StateMachine;

namespace Game.Player.States
{
    /// <summary>
    ///     Base for every GAMEPLAY player state (Idle/Run/Dash/Hit). Wraps the state-specific hooks with the
    ///     actor-wide per-frame work — the i-frame / dash-cooldown / hit-flash countdowns + the ring-out check
    ///     (<see cref="PlayerContext.TickCommon" />) and the physics-spin kill
    ///     (<see cref="PlayerContext.FixedTickCommon" />) — so the controller ticks nothing itself, and the
    ///     pause + dead states (which do NOT derive from this) freeze that work for free.
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
