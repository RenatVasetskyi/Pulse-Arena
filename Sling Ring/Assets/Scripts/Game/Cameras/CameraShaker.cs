using Unity.Cinemachine;
using UnityEngine;

namespace Game.Cameras
{
    /// <summary>
    ///     Perlin-noise camera shake: on <see cref="Shake" /> it ramps the noise amplitude from full down to zero
    ///     over the duration, then mutes. Driven by <see cref="Tick" /> from the camera's Update (no coroutine).
    /// </summary>
    public class CameraShaker
    {
        private float _duration;
        private float _frequency;
        private CinemachineBasicMultiChannelPerlin _noise;
        private float _strength;
        private float _timer;

        public void Initialize(CinemachineBasicMultiChannelPerlin noise, float frequency)
        {
            _noise = noise;
            _frequency = frequency;
            Mute();
        }

        public void Mute()
        {
            if (_noise != null)
                _noise.AmplitudeGain = 0f;
        }

        public void Shake(float duration, float strength)
        {
            if (_noise == null)
                return;

            _duration = Mathf.Max(duration, 0.01f);
            _strength = strength;
            _timer = _duration;
            _noise.FrequencyGain = _frequency;
        }

        public void Tick(float deltaTime)
        {
            if (_noise == null || _timer <= 0f)
                return;

            _noise.AmplitudeGain = _strength * (_timer / _duration);
            _timer -= deltaTime;

            if (_timer <= 0f)
                Mute();
        }
    }
}