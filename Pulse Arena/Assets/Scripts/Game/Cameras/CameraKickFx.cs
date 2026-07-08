using UnityEngine;

namespace Game.Cameras
{
    /// <summary>
    /// The transient additive "lens juice": a sine-pulse position offset kick and an attack/spring FOV punch.
    /// Both decay back to zero on their own. The camera composites <see cref="CurrentOffset"/> and
    /// <see cref="CurrentFovKick"/> on top of the base framing each frame. Driven by <see cref="Tick"/>.
    /// </summary>
    public class CameraKickFx
    {
        private Vector3 _kickOffset;
        private float _offsetDuration;
        private float _offsetTimer;
        private Vector3 _currentOffset;

        private float _fovDelta;
        private float _fovDuration;
        private float _fovTimer;
        private float _currentFovKick;

        public Vector3 CurrentOffset => _currentOffset;
        public float CurrentFovKick => _currentFovKick;

        public void KickOffset(Vector3 offset, float duration)
        {
            _kickOffset = offset;
            _offsetDuration = Mathf.Max(duration, 0.01f);
            _offsetTimer = 0f;
        }

        public void FovPunch(float delta, float duration)
        {
            _fovDelta = delta;
            _fovDuration = Mathf.Max(duration, 0.01f);
            _fovTimer = 0f;
        }

        public void Tick(float deltaTime)
        {
            TickOffset(deltaTime);
            TickFov(deltaTime);
        }

        private void TickOffset(float deltaTime)
        {
            if (_offsetTimer >= _offsetDuration)
            {
                _currentOffset = Vector3.zero;
                return;
            }

            float progress = _offsetTimer / _offsetDuration;
            _currentOffset = _kickOffset * Mathf.Sin(progress * Mathf.PI);
            _offsetTimer += deltaTime;
        }

        private void TickFov(float deltaTime)
        {
            if (_fovTimer >= _fovDuration)
            {
                _currentFovKick = 0f;
                return;
            }

            float attack = _fovDuration * 0.3f;
            float kick;

            if (_fovTimer < attack)
            {
                float t = attack > 0f ? _fovTimer / attack : 1f; // fast zoom-in
                kick = _fovDelta * (1f - (1f - t) * (1f - t));    // ease-out
            }
            else
            {
                float denominator = _fovDuration - attack;
                float t = denominator > 0f ? (_fovTimer - attack) / denominator : 1f;
                kick = _fovDelta * (1f - Mathf.SmoothStep(0f, 1f, t)); // spring back
            }

            _currentFovKick = kick;
            _fovTimer += deltaTime;
        }
    }
}
