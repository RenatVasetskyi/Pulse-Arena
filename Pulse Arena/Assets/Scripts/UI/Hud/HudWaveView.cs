using TMPro;
using UnityEngine;

namespace UI.Hud
{
    /// <summary>HUD wave counter. Assign the TMP label in the prefab.</summary>
    public class HudWaveView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _label;

        public void SetWave(int current, int total)
        {
            if (_label == null)
                return;

            // total 0 = endless Survival — show a custom infinity sprite (GoblinOne has no ∞ glyph); tint=1 so it
            // takes the label's text colour.
            _label.text = total > 0
                ? $"Wave {current}/{total}"
                : $"Wave {current}/<sprite name=\"inf\" tint=1>";
            UiTween.Punch(_label.transform, 0.4f, 0.35f);
        }
    }
}