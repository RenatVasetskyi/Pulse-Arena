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
            if (_label != null)
                _label.text = $"Wave {current}/{total}";
        }
    }
}
