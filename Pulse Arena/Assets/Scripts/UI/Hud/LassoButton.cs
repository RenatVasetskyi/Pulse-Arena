using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Hud
{
    /// <summary>
    ///     On-screen lasso/attack button. Press = throw, hold = charge, release = launch (frame-accurate
    ///     via Time.frameCount). Stays fully opaque; on press it scales down slightly and darkens for a
    ///     tactile "pressed" feel. Assign _graphic to the button's Image.
    /// </summary>
    public class LassoButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private Graphic _graphic;
        [SerializeField, Range(0.7f, 1f)] private float _pressedScale = 0.92f;
        [SerializeField] private Color _pressedTint = new Color(0.82f, 0.82f, 0.82f, 1f);
        [SerializeField] private float _feedbackSpeed = 18f;
        private Vector3 _baseScale = Vector3.one;

        private int _pressedFrame = -1;
        private int _releasedFrame = -1;
        private float _targetScale = 1f;
        private Color _targetTint = Color.white;

        public bool Held { get; private set; }
        public bool PressedThisFrame => _pressedFrame == Time.frameCount;
        public bool ReleasedThisFrame => _releasedFrame == Time.frameCount;

        private void Awake()
        {
            _baseScale = transform.localScale;
            _targetScale = 1f;
            _targetTint = Color.white;

            if (_graphic != null)
                _graphic.color = Color.white;
        }

        private void Update()
        {
            float t = 1f - Mathf.Exp(-_feedbackSpeed * Time.unscaledDeltaTime);
            transform.localScale = Vector3.Lerp(transform.localScale, _baseScale * _targetScale, t);

            if (_graphic != null)
                _graphic.color = Color.Lerp(_graphic.color, _targetTint, t);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressedFrame = Time.frameCount;
            Held = true;
            _targetScale = _pressedScale;
            _targetTint = _pressedTint;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _releasedFrame = Time.frameCount;
            Held = false;
            _targetScale = 1f;
            _targetTint = Color.white;
        }
    }
}