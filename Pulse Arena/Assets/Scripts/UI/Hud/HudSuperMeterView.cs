using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Hud
{
    /// <summary>
    /// The ultimate super-meter bar — a display-only Slider (0..1) that fills as enemies die. When full it
    /// switches the Fill to the "ready" colour, reveals the ultimate prompt, and pulses until spent. Assign
    /// the Slider, its Fill Image and the (optional) ready-prompt CanvasGroup.
    /// </summary>
    public class HudSuperMeterView : MonoBehaviour
    {
        [SerializeField] private Slider _slider;
        [SerializeField] private Image _fill;
        [SerializeField] private CanvasGroup _readyPrompt;
        [SerializeField] private Color _chargingColor = new(0.35f, 0.7f, 1f, 1f);
        [SerializeField] private Color _readyColor = new(1f, 0.85f, 0.25f, 1f);

        private Vector3 _baseScale = Vector3.one;
        private bool _isReady;

        private void Awake()
        {
            if (_slider != null)
                _baseScale = _slider.transform.localScale;

            SetCharge(0f);
            ApplyReady(false, false);
        }

        public void SetCharge(float charge01)
        {
            if (_slider != null)
                _slider.value = Mathf.Clamp01(charge01);
        }

        public void SetReady(bool ready)
        {
            ApplyReady(ready, ready && !_isReady);
        }

        private void ApplyReady(bool ready, bool flourish)
        {
            _isReady = ready;

            if (_fill != null)
                _fill.color = ready ? _readyColor : _chargingColor;

            if (_readyPrompt != null)
            {
                _readyPrompt.DOKill();
                _readyPrompt.DOFade(ready ? 1f : 0f, 0.25f).SetUpdate(true).SetLink(gameObject);
            }

            if (flourish && _slider != null)
            {
                _slider.transform.DOKill();
                _slider.transform.localScale = _baseScale;
                _slider.transform.DOPunchScale(_baseScale * 0.14f, 0.35f, 8, 0.8f).SetUpdate(true).SetLink(gameObject);
            }
        }

        private void Update()
        {
            if (!_isReady || _slider == null)
                return;

            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.03f;
            _slider.transform.localScale = _baseScale * pulse;
        }
    }
}
