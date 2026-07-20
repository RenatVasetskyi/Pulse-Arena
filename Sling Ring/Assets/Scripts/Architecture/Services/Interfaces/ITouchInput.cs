using UnityEngine;

namespace Architecture.Services.Interfaces
{
    /// <summary>
    ///     On-screen touch controls (virtual joystick + lasso / dash / ultimate buttons) that feed the
    ///     InputService. Implemented by the HUD; registered via IInputService.SetTouchInput.
    /// </summary>
    public interface ITouchInput
    {
        bool DashPressedThisFrame { get; }
        bool LassoHeld { get; }
        bool LassoPressedThisFrame { get; }
        bool LassoReleasedThisFrame { get; }
        Vector2 Move { get; }
        bool UltimatePressedThisFrame { get; }
    }
}