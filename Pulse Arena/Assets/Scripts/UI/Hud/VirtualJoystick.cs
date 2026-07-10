using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Hud
{
    /// <summary>
    ///     Floating on-screen movement stick: hidden until touched. A press anywhere inside this
    ///     object's rect (the movement zone) pops the joystick up under the finger; dragging steers;
    ///     releasing hides it again. Value is the normalised direction (-1..1). Put this component on
    ///     a large transparent raycast zone and assign the visual base (moved to the finger) and its
    ///     handle knob (both live outside this zone so they can render on top).
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform _background;
        [SerializeField] private RectTransform _handle;
        [SerializeField] private float _radius = 85f;

        public Vector2 Value { get; private set; }

        private void Awake()
        {
            SetVisible(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            MoveToFinger(eventData);
            SetVisible(true);
            Recenter();
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_background == null)
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _background, eventData.position, eventData.pressEventCamera, out Vector2 local);

            Vector2 clamped = Vector2.ClampMagnitude(local, _radius);
            Value = clamped / _radius;

            if (_handle != null)
                _handle.anchoredPosition = clamped;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Value = Vector2.zero;
            Recenter();
            SetVisible(false);
        }

        private void MoveToFinger(PointerEventData eventData)
        {
            if (_background == null)
                return;

            RectTransform parent = _background.parent as RectTransform;

            if (parent != null && RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    parent, eventData.position, eventData.pressEventCamera, out Vector3 world))
                _background.position = world;
        }

        private void Recenter()
        {
            if (_handle != null)
                _handle.anchoredPosition = Vector2.zero;
        }

        private void SetVisible(bool visible)
        {
            if (_background != null && _background.gameObject.activeSelf != visible)
                _background.gameObject.SetActive(visible);
        }
    }
}
