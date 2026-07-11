using Architecture.Services.Interfaces;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Architecture.Services
{
    public class InputService : IInputService
    {
        private readonly EventSystem _eventSystem;
        private ITouchInput _touch;

        public bool IsDashPressedThisFrame =>
            IsEnabled && ((Keyboard.current != null && Keyboard.current.leftShiftKey.wasPressedThisFrame)
                          || (_touch != null && _touch.DashPressedThisFrame));

        public bool IsEnabled { get; private set; } = true;

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

        public bool IsSlingshotHeld =>
            IsEnabled && (KeyboardEHeld() || MouseRightHeld() ||
                          (_touch != null && _touch.LassoHeld));

        public bool IsSlingshotPressedThisFrame =>
            IsEnabled && (KeyboardEPressed() || MouseRightPressed() ||
                          (_touch != null && _touch.LassoPressedThisFrame));

        public bool IsSlingshotReleasedThisFrame =>
            IsEnabled && (KeyboardEReleased() || MouseRightReleased() ||
                          (_touch != null && _touch.LassoReleasedThisFrame));

        public bool IsUltimatePressedThisFrame =>
            IsEnabled && ((Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
                          || (_touch != null && _touch.UltimatePressedThisFrame));

        public Vector2 MoveDirection
        {
            get
            {
                if (!IsEnabled)
                    return Vector2.zero;

                Vector2 keyboard = KeyboardMove();

                if (keyboard.sqrMagnitude > 0.01f)
                    return keyboard;

                return _touch != null ? Vector2.ClampMagnitude(_touch.Move, 1f) : Vector2.zero;
            }
        }

        public InputService(EventSystem eventSystem)
        {
            _eventSystem = eventSystem;
        }

        public void Disable()
        {
            IsEnabled = false;
        }

        public void Enable()
        {
            IsEnabled = true;
        }

        public void SetTouchInput(ITouchInput touchInput)
        {
            _touch = touchInput;
        }

        private static Vector2 KeyboardMove()
        {
            if (Keyboard.current == null)
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

        private static bool KeyboardEPressed()
        {
            return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        }

        private static bool KeyboardEHeld()
        {
            return Keyboard.current != null && Keyboard.current.eKey.isPressed;
        }

        private static bool KeyboardEReleased()
        {
            return Keyboard.current != null && Keyboard.current.eKey.wasReleasedThisFrame;
        }

        private bool MouseRightPressed()
        {
            return !IsPointerOverUi() && Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
        }

        private static bool MouseRightHeld()
        {
            return Mouse.current != null && Mouse.current.rightButton.isPressed;
        }

        private bool MouseRightReleased()
        {
            return !IsPointerOverUi() && Mouse.current != null && Mouse.current.rightButton.wasReleasedThisFrame;
        }

        private bool IsPointerOverUi()
        {
            return _eventSystem != null && _eventSystem.IsPointerOverGameObject();
        }
    }
}