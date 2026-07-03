using UnityEngine;

namespace Architecture.Services.Interfaces
{
    public interface IInputService
    {
        Vector2 MoveDirection { get; }
        bool IsPulsePressedThisFrame { get; }
        bool IsEnabled { get; }

        void Enable();
        void Disable();
    }
}
