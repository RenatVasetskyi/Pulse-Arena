using Architecture.Services.Interfaces;
using Game.Cameras;
using Game.Combat;
using Game.Player;
using UnityEngine;

namespace UI.Hud
{
    /// <summary>
    /// One HUD canvas for the game scene. Holds the sub-views (health/score/wave/zoom) and
    /// the touch controls (joystick + lasso button), and wires each to its source. All refs
    /// are optional (null-safe) so a partial HUD works. Assign the components on the prefab root.
    /// </summary>
    public class GameHud : MonoBehaviour, ITouchInput
    {
        [SerializeField] private HudHealthView _health;
        [SerializeField] private HudScoreView _score;
        [SerializeField] private HudWaveView _wave;
        [SerializeField] private HudZoomView _zoom;
        [SerializeField] private HudToastView _toast;
        [SerializeField] private HudTensionView _tension;

        [Header("Touch controls (mobile)")]
        [SerializeField] private VirtualJoystick _joystick;
        [SerializeField] private LassoButton _lassoButton;

        Vector2 ITouchInput.Move => _joystick != null ? _joystick.Value : Vector2.zero;
        bool ITouchInput.LassoPressedThisFrame => _lassoButton != null && _lassoButton.PressedThisFrame;
        bool ITouchInput.LassoHeld => _lassoButton != null && _lassoButton.Held;
        bool ITouchInput.LassoReleasedThisFrame => _lassoButton != null && _lassoButton.ReleasedThisFrame;

        public void Bind(PlayerController player, IScoreService score, IBattleCamera camera)
        {
            if (_health != null)
                _health.Bind(player);

            if (_score != null)
                _score.Bind(score);

            if (_zoom != null)
                _zoom.Bind(camera);
        }

        public void SetWave(int current, int total)
        {
            if (_wave != null)
                _wave.SetWave(current, total);
        }

        public void ShowToast(string message, float duration)
        {
            if (_toast != null)
                _toast.Show(message, duration);
        }

        public void BindTension(EnemySlingshot slingshot)
        {
            if (_tension != null)
                _tension.Bind(slingshot);
        }
    }
}
