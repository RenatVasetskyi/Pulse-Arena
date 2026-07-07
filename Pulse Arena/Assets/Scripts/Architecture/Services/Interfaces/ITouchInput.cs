using UnityEngine;

namespace Architecture.Services.Interfaces
{
    /// <summary>
    /// On-screen touch controls (virtual joystick + lasso / dash / ultimate buttons) that feed the
    /// InputService. Implemented by the HUD; registered via IInputService.SetTouchInput.
    /// </summary>
    public interface ITouchInput
    {
        Vector2 Move { get; }
        bool LassoPressedThisFrame { get; }
        bool LassoHeld { get; }
        bool LassoReleasedThisFrame { get; }
        bool DashPressedThisFrame { get; }
        bool UltimatePressedThisFrame { get; }
    }
}
