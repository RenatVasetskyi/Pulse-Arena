using Architecture.Services.Interfaces;
using Data;
using UnityEngine;

namespace Game.Cameras
{
    /// <summary>
    ///     The zoom axis: clamps + smooth-damps the current zoom toward a target, and is the ONLY piece coupled to
    ///     <see cref="ISettingsService" /> so the +/- buttons and the settings slider share one persisted value.
    ///     Reads its tuning straight from <see cref="CameraData" /> and exposes <see cref="ZoomedFollowOffset" /> —
    ///     the base follow offset scaled by the live zoom — which the camera composites each frame.
    /// </summary>
    public class CameraZoomController
    {
        private Vector3 _baseFollowOffset;
        private CameraData _data;
        private ISettingsService _settings;
        private float _targetZoom;

        private float _zoom;
        private float _zoomVelocity;

        public Vector3 ZoomedFollowOffset
        {
            get
            {
                float safeZoom = _zoom > 0f ? _zoom : Mathf.Clamp(_data.DefaultZoom, _data.MinZoom, _data.MaxZoom);
                return _baseFollowOffset * safeZoom;
            }
        }

        public void Initialize(ISettingsService settings, Vector3 baseFollowOffset, CameraData data)
        {
            _settings = settings;
            _baseFollowOffset = baseFollowOffset;
            _data = data;

            float initial = _settings != null ? _settings.CameraZoom : _data.DefaultZoom;
            _zoom = Mathf.Clamp(initial, _data.MinZoom, _data.MaxZoom);
            _targetZoom = _zoom;
        }

        public void SetTargetZoom(float zoom)
        {
            _targetZoom = Mathf.Clamp(zoom, _data.MinZoom, _data.MaxZoom);
        }

        /// <summary>Re-reads the persisted zoom (called when the settings slider changes it).</summary>
        public void SyncTargetFromSettings()
        {
            if (_settings != null)
                SetTargetZoom(_settings.CameraZoom);
        }

        public void Tick()
        {
            if (Mathf.Approximately(_zoom, _targetZoom))
                return;

            _zoom = Mathf.SmoothDamp(_zoom, _targetZoom, ref _zoomVelocity,
                Mathf.Max(0.01f, _data.ZoomSmoothTime), Mathf.Infinity, Time.unscaledDeltaTime);
        }

        public void ZoomIn()
        {
            ApplyZoom(_targetZoom - _data.ZoomStep);
        }

        public void ZoomOut()
        {
            ApplyZoom(_targetZoom + _data.ZoomStep);
        }

        // Route zoom through settings so the +/- buttons and the settings slider share one persisted value.
        private void ApplyZoom(float value)
        {
            if (_settings != null)
                _settings.SetCameraZoom(value);
            else
                SetTargetZoom(value);
        }
    }
}