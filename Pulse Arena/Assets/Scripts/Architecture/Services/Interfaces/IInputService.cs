using UnityEngine;

namespace Architecture.Services.Interfaces
{
    public interface IInputService
    {
        bool IsDashPressedThisFrame { get; }
        bool IsEnabled { get; }
        bool IsOrbitBurstPressedThisFrame { get; }
        bool IsSlingshotHeld { get; }
        bool IsSlingshotPressedThisFrame { get; }
        bool IsSlingshotReleasedThisFrame { get; }
        bool IsUltimatePressedThisFrame { get; }
        Vector2 MoveDirection { get; }
        void Disable();

        void Enable();
        void SetTouchInput(ITouchInput touchInput);
    }
}