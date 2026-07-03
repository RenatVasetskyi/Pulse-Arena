using System.Collections;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;

namespace Game.Cameras
{
    public class BattleCamera : MonoBehaviour, IBattleCamera
    {
        [Header("Cinemachine")]
        [SerializeField] private CinemachineCamera _camera;
        [SerializeField] private CinemachineFollow _follow;
        [SerializeField] private CinemachineRotationComposer _rotationComposer;
        [SerializeField] private CinemachineBasicMultiChannelPerlin _noise;

        [Header("Battle View")]
        [SerializeField] private Vector3 _followOffset = new(0f, 14f, -11f);
        [SerializeField] private Vector3 _lookAtOffset = new(0f, 0.8f, 0f);
        [SerializeField] private Vector3 _positionDamping = new(0.12f, 0.18f, 0.12f);
        [SerializeField] private Vector2 _rotationDamping = new(0.25f, 0.25f);
        [SerializeField] private float _fieldOfView = 55f;

        [Header("Shake")]
        [SerializeField] private float _defaultShakeDuration = 0.18f;
        [SerializeField] private float _defaultShakeStrength = 0.35f;
        [SerializeField] private float _shakeFrequency = 35f;

        private Coroutine _shakeRoutine;

        private void Awake()
        {
            CacheComponents();
            ApplySettings();
            MuteNoise();
        }

        private void OnValidate()
        {
            CacheComponents();
            ApplySettings();
        }

        public void Follow(Transform target, bool snap = true)
        {
            if (_camera == null)
                return;

            _camera.Follow = target;
            _camera.LookAt = target;

            if (snap && target != null)
            {
                Vector3 position = target.position + _followOffset;
                Quaternion rotation = Quaternion.LookRotation(target.position + _lookAtOffset - position);
                _camera.ForceCameraPosition(position, rotation);
            }
        }

        public void Shake(float duration, float strength)
        {
            if (_noise == null)
                return;

            if (_shakeRoutine != null)
                StopCoroutine(_shakeRoutine);

            _shakeRoutine = StartCoroutine(ShakeRoutine(duration, strength));
        }

        public void Shake()
        {
            Shake(_defaultShakeDuration, _defaultShakeStrength);
        }

        private IEnumerator ShakeRoutine(float duration, float strength)
        {
            float safeDuration = Mathf.Max(duration, 0.01f);
            float timer = safeDuration;

            _noise.FrequencyGain = _shakeFrequency;

            while (timer > 0f)
            {
                float progress = timer / safeDuration;
                _noise.AmplitudeGain = strength * progress;

                timer -= Time.deltaTime;
                yield return null;
            }

            MuteNoise();
            _shakeRoutine = null;
        }

        private void CacheComponents()
        {
            if (_camera == null)
                _camera = GetComponent<CinemachineCamera>();

            if (_follow == null)
                _follow = GetComponent<CinemachineFollow>();

            if (_rotationComposer == null)
                _rotationComposer = GetComponent<CinemachineRotationComposer>();

            if (_noise == null)
                _noise = GetComponent<CinemachineBasicMultiChannelPerlin>();
        }

        private void ApplySettings()
        {
            if (_camera != null)
                _camera.Lens.FieldOfView = _fieldOfView;

            if (_follow != null)
            {
                _follow.FollowOffset = _followOffset;
                _follow.TrackerSettings.BindingMode = BindingMode.WorldSpace;
                _follow.TrackerSettings.PositionDamping = _positionDamping;
            }

            if (_rotationComposer != null)
            {
                _rotationComposer.TargetOffset = _lookAtOffset;
                _rotationComposer.Damping = _rotationDamping;
            }

            if (_noise != null)
                _noise.FrequencyGain = _shakeFrequency;
        }

        private void MuteNoise()
        {
            if (_noise == null)
                return;

            _noise.AmplitudeGain = 0f;
        }
    }
}
