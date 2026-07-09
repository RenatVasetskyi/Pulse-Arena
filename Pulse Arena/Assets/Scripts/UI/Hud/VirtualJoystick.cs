using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Hud
{
    /// <summary>
    ///     On-screen movement stick. Drag the handle within a radius; Value is the normalised
    ///     direction (-1..1). Assign _background (the ring) and _handle (the knob) in the prefab.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform _background;
        [SerializeField] private RectTransform _handle;
        [SerializeField] private float _radius = 80f;

        public Vector2 Value { get; private set; }

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

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Value = Vector2.zero;

            if (_handle != null)
                _handle.anchoredPosition = Vector2.zero;
        }
    }
}