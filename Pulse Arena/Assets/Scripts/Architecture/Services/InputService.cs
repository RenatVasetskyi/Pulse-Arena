using Architecture.Services.Interfaces;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Architecture.Services
{
    public class InputService : IInputService
    {
        public bool IsEnabled { get; private set; } = true;
        
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
            get { return IsPushPressedThisFrame; }
        }

        public bool IsShootPressedThisFrame
        {
            get { return false; }
        }

        public bool IsPushPressedThisFrame
        {
            get { return IsOrbitBurstPressedThisFrame; }
        }

        public bool IsPullPressedThisFrame
        {
            get { return IsSlingshotPressedThisFrame; }
        }

        public bool IsSlingshotPressedThisFrame
        {
            get
            {
                if (!IsEnabled)
                    return false;

                bool keyPressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
                bool mousePressed = !IsPointerOverUi() && Mouse.current != null &&
                                    Mouse.current.rightButton.wasPressedThisFrame;

                return keyPressed || mousePressed;
            }
        }

        public bool IsSlingshotHeld
        {
            get
            {
                if (!IsEnabled)
                    return false;

                bool keyHeld = Keyboard.current != null && Keyboard.current.eKey.isPressed;
                bool mouseHeld = Mouse.current != null && Mouse.current.rightButton.isPressed;

                return keyHeld || mouseHeld;
            }
        }

        public bool IsSlingshotReleasedThisFrame
        {
            get
            {
                if (!IsEnabled)
                    return false;

                bool keyReleased = Keyboard.current != null && Keyboard.current.eKey.wasReleasedThisFrame;
                bool mouseReleased = !IsPointerOverUi() && Mouse.current != null &&
                                     Mouse.current.rightButton.wasReleasedThisFrame;

                return keyReleased || mouseReleased;
            }
        }

        public bool IsOrbitBurstPressedThisFrame
        {
            get
            {
                if (!IsEnabled)
                    return false;

                bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
                bool mousePressed = !IsPointerOverUi() && Mouse.current != null &&
                                    Mouse.current.leftButton.wasPressedThisFrame;

                return spacePressed || mousePressed;
            }
        }

        public void Enable()
        {
            IsEnabled = true;
        }

        public void Disable()
        {
            IsEnabled = false;
        }

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
