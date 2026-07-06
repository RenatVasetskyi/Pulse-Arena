using Game.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Hud
{
    /// <summary>
    /// HUD rope-tension meter — a display-only Slider. Shows while the lasso is charging: the
    /// slider fills with tension, the Fill shifts safe->danger colour and pulses near the break
    /// point. Assign the CanvasGroup (show/hide), the Slider and its Fill Image.
    /// </summary>
    public class HudTensionView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Slider _slider;
        [SerializeField] private Image _fill;
        [SerializeField] private Color _safeColor = new(1f, 0.82f, 0.3f, 0.95f);
        [SerializeField] private Color _dangerColor = new(1f, 0.22f, 0.12f, 1f);

        private EnemySlingshot _slingshot;
        private float _tension;
        private Vector3 _baseScale = Vector3.one;

        public void Bind(EnemySlingshot slingshot)
        {
            if (_slingshot != null)
                _slingshot.TensionChanged -= OnTensionChanged;

            _slingshot = slingshot;

            if (_slingshot != null)
                _slingshot.TensionChanged += OnTensionChanged;

            OnTensionChanged(0f);
        }

        private void OnDestroy()
        {
            if (_slingshot != null)
                _slingshot.TensionChanged -= OnTensionChanged;
        }

        private void OnTensionChanged(float tension)
        {
            _tension = tension;

            if (_canvasGroup != null)
                _canvasGroup.alpha = tension > 0.01f ? 1f : 0f;

            if (_slider != null)
                _slider.value = Mathf.Clamp01(tension);

            if (_fill != null)
                _fill.color = Color.Lerp(_safeColor, _dangerColor, tension);
        }

        private void Awake()
        {
            if (_slider != null)
                _baseScale = _slider.transform.localScale;
        }

        private void Update()
        {
            if (_canvasGroup == null || _canvasGroup.alpha <= 0f || _slider == null)
                return;

            float danger = Mathf.InverseLerp(0.55f, 1f, _tension);
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 24f) * 0.06f * danger;
            _slider.transform.localScale = _baseScale * pulse;
        }
    }
}
