using Data;
using UnityEngine;

namespace Game.Pickups
{
    /// <summary>
    ///     The health orb's idle presentation: a vertical bob, the visual-root / inner-ring / outer-ring spins and
    ///     the breathing glow pulse (halo scale + point-light intensity). Driven by <see cref="Tick" /> each frame.
    /// </summary>
    public class OrbIdleAnimator
    {
        private Vector3 _baseGlowScale = Vector3.one;
        private float _baseLightIntensity;
        private PickupData _data;
        private Transform _innerGlow;
        private Transform _innerRing;
        private Light _light;
        private Transform _outerRing;
        private Transform _self;
        private Vector3 _startPosition;
        private Transform _visualRoot;

        public void Initialize(Transform self, Transform visualRoot, Transform innerGlow,
            Transform innerRing, Transform outerRing, Light light, PickupData data)
        {
            _self = self;
            _visualRoot = visualRoot;
            _innerGlow = innerGlow;
            _innerRing = innerRing;
            _outerRing = outerRing;
            _light = light;
            _data = data;
            _startPosition = self.position;

            if (_innerGlow != null)
                _baseGlowScale = _innerGlow.localScale;

            if (_light != null)
                _baseLightIntensity = _light.intensity;
        }

        public void Tick()
        {
            float bobOffset = Mathf.Sin(Time.time * _data.BobSpeed) * _data.BobHeight;
            _self.position = _startPosition + Vector3.up * bobOffset;

            if (_visualRoot != null)
                _visualRoot.Rotate(Vector3.up, _data.RotateSpeed * Time.deltaTime, Space.World);

            if (_innerRing != null)
                _innerRing.Rotate(Vector3.forward, _data.RotateSpeed * 1.3f * Time.deltaTime, Space.Self);

            if (_outerRing != null)
                _outerRing.Rotate(Vector3.right, -_data.RotateSpeed * 0.9f * Time.deltaTime, Space.Self);

            UpdateGlow();
        }

        // Breathing glow: pulses the halo sphere + point light so the orb reads as "special".
        private void UpdateGlow()
        {
            float pulse = 1f + Mathf.Sin(Time.time * _data.BobSpeed * 1.5f) * 0.22f;

            if (_innerGlow != null)
                _innerGlow.localScale = _baseGlowScale * pulse;

            if (_light != null)
                _light.intensity = _baseLightIntensity * pulse;
        }
    }
}