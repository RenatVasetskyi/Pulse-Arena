using Architecture.Services.Interfaces;
using Game.Cameras;
using Game.Player;
using UnityEngine;

namespace UI.Hud
{
    /// <summary>
    /// One HUD canvas for the game scene. Holds the sub-views (health/score/wave/zoom) and
    /// wires each to its data source. All sub-views are optional (null-safe) so you can
    /// build a partial HUD. Assign the sub-view components on the prefab root.
    /// </summary>
    public class GameHud : MonoBehaviour
    {
        [SerializeField] private HudHealthView _health;
        [SerializeField] private HudScoreView _score;
        [SerializeField] private HudWaveView _wave;
        [SerializeField] private HudZoomView _zoom;

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
    }
}
