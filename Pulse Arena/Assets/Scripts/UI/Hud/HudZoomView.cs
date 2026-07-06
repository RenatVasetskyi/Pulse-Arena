using Game.Cameras;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Hud
{
    /// <summary>HUD zoom buttons. Assign the + / − buttons in the prefab.</summary>
    public class HudZoomView : MonoBehaviour
    {
        [SerializeField] private Button _zoomIn;
        [SerializeField] private Button _zoomOut;

        private IBattleCamera _camera;

        public void Bind(IBattleCamera camera)
        {
            _camera = camera;

            if (_zoomIn != null)
                _zoomIn.onClick.AddListener(OnZoomIn);

            if (_zoomOut != null)
                _zoomOut.onClick.AddListener(OnZoomOut);
        }

        private void OnDestroy()
        {
            if (_zoomIn != null)
                _zoomIn.onClick.RemoveListener(OnZoomIn);

            if (_zoomOut != null)
                _zoomOut.onClick.RemoveListener(OnZoomOut);
        }

        private void OnZoomIn() => _camera?.ZoomIn();
        private void OnZoomOut() => _camera?.ZoomOut();
    }
}
