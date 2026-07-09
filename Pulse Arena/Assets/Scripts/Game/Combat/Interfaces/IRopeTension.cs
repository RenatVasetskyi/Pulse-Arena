using System;
using Data;

namespace Game.Combat.Interfaces
{
    /// <summary>
    ///     The rope's tension simulation while an enemy is spun: pure math (no Unity types), driven by the
    ///     grabbed enemy's weight/type-rate and the charge. Directly unit-testable. The slingshot breaks the rope
    ///     when <see cref="IsBroken" /> and drives the tension warning bar from <see cref="Warning" />.
    /// </summary>
    public interface IRopeTension
    {
        event Action<float> Changed;
        bool IsBroken { get; }
        float Value { get; }
        float Warning { get; }

        void Initialize(SlingshotData data);
        void Reset();
        void Tick(float weight, float typeRate, float chargeProgress, float deltaTime);
    }
}