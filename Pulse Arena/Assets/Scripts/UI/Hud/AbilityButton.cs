using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Hud
{
    /// <summary>
    ///     On-screen ability button (dash / ultimate) for touch. A tap fires the ability (frame-accurate via
    ///     Time.frameCount); a radial Fill image shows cooldown/charge and the button dims until it's ready.
    ///     The press is always reported — the ability itself decides whether it can fire — so a not-ready tap
    ///     is simply a no-op. Assign the radial Fill image and the CanvasGroup (for dimming).
    /// </summary>
    public class AbilityButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private Image _fill;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField, Range(0.7f, 1f)] private float _pressedScale = 0.9f;
        [SerializeField, Range(0.1f, 1f)] private float _chargingAlpha = 0.5f;
        [SerializeField] private float _feedbackSpeed = 18f;
        private Vector3 _baseScale = Vector3.one;

        private int _pressedFrame = -1;
        private float _targetScale = 1f;

        public bool PressedThisFrame => _pressedFrame == Time.frameCount;

        private void Awake()
        {
            _baseScale = transform.localScale;
            SetCharge(1f);
        }

        private void Update()
        {
            float t = 1f - Mathf.Exp(-_feedbackSpeed * Time.unscaledDeltaTime);
            transform.localScale = Vector3.Lerp(transform.localScale, _baseScale * _targetScale, t);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressedFrame = Time.frameCount;
            _targetScale = _pressedScale;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _targetScale = 1f;
        }

        public void SetCharge(float charge01)
        {
            charge01 = Mathf.Clamp01(charge01);

            if (_fill != null)
                _fill.fillAmount = charge01;

            if (_canvasGroup != null)
                _canvasGroup.alpha = charge01 >= 0.999f ? 1f : _chargingAlpha;
        }
    }
}