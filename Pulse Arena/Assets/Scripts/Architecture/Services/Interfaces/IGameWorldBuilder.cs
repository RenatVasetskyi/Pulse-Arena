namespace Architecture.Services.Interfaces
{
    /// <summary>
    /// Contract for building and tearing down the whole game world. The concrete implementation
    /// (Bootstrap.GameWorldBuilder) knows about every gameplay system, but the states only depend on this
    /// abstraction — so LoadGameState / GameLoopState can live in the Core assembly and be triggered from UI
    /// (the menu's "Play") without the assembly cycle a direct reference would create.
    /// </summary>
    public interface IGameWorldBuilder
    {
        /// <summary>Creates the arena + player via factories, wires the HUD / feedback / flow and starts spawning.</summary>
        void Build();

        /// <summary>Destroys the world and resets the app-lifetime services so the next Build starts clean.</summary>
        void Teardown();
    }
}
