using Architecture.Services.Interfaces;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Architecture.Services
{
    public class InputService : IInputService
    {
        public Vector2 MoveDirection
        {
            get
            {
                if (!IsEnabled || Keyboard.current == null)
                    return Vector2.zero;

                Vector2 direction = Vector2.zero;

                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                    direction.x -= 1f;

                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                    direction.x += 1f;

                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                    direction.y -= 1f;

                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                    direction.y += 1f;

                return Vector2.ClampMagnitude(direction, 1f);
            }
        }

        public bool IsPulsePressedThisFrame
        {
            get
            {
                if (!IsEnabled)
                    return false;

                bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
                bool mousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

                return spacePressed || mousePressed;
            }
        }

        public bool IsEnabled { get; private set; } = true;

        public void Enable()
        {
            IsEnabled = true;
        }

        public void Disable()
        {
            IsEnabled = false;
        }
    }
}
