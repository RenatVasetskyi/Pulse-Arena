using Architecture.Services.Interfaces;
using TMPro;
using UnityEngine;

namespace UI.Hud
{
    /// <summary>
    /// HUD score label. Large values are abbreviated (K/M) so a bug can never blow the
    /// panel up to full-screen width. Assign the TMP label in the prefab.
    /// </summary>
    public class HudScoreView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _label;

        private IScoreService _score;
        private int _lastValue = int.MinValue;

        public void Bind(IScoreService score)
        {
            _score = score;
            _score.ScoreChanged += OnScoreChanged;
            OnScoreChanged(score.Score);
        }

        private void OnDestroy()
        {
            if (_score != null)
                _score.ScoreChanged -= OnScoreChanged;
        }

        private void OnScoreChanged(int value)
        {
            if (_label != null)
                _label.text = $"Score: {Format(value)}";

            if (value > _lastValue && _lastValue != int.MinValue && _label != null)
                UiTween.Punch(_label.transform, 0.35f);

            _lastValue = value;
        }

        private static string Format(int value)
        {
            if (value >= 1_000_000)
                return $"{value / 1_000_000f:0.#}M";

            if (value >= 100_000)
                return $"{value / 1_000f:0.#}K";

            return value.ToString();
        }
    }
}
