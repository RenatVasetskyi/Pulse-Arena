using UnityEngine;

namespace Architecture.Services.Interfaces
{
    public interface IInputService
    {
        Vector2 MoveDirection { get; }
        bool IsSlingshotPressedThisFrame { get; }
        bool IsSlingshotHeld { get; }
        bool IsSlingshotReleasedThisFrame { get; }
        bool IsOrbitBurstPressedThisFrame { get; }
        bool IsEnabled { get; }

        void Enable();
        void Disable();
        void SetTouchInput(ITouchInput touchInput);
    }
}
