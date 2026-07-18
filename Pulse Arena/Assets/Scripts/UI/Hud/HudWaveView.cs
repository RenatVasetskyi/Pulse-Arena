using TMPro;
using UnityEngine;

namespace UI.Hud
{
    /// <summary>HUD wave counter. Assign the TMP label in the prefab.</summary>
    public class HudWaveView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private string _waveFormat = "Wave {0}/{1}";

        [Tooltip("Endless Survival wave label — {0} is the wave; the total slot is an infinity sprite (no glyph in the font).")]
        [SerializeField] private string _waveEndlessFormat = "Wave {0}/<sprite name=\"inf\" tint=1>";

        public void SetWave(int current, int total)
        {
            if (_label == null)
                return;

            // total 0 = endless Survival — show a custom infinity sprite (GoblinOne has no ∞ glyph); tint=1 so it
            // takes the label's text colour.
            _label.text = total > 0
                ? string.Format(_waveFormat, current, total)
                : string.Format(_waveEndlessFormat, current);
            UiTween.Punch(_label.transform, 0.4f, 0.35f);
        }
    }
}