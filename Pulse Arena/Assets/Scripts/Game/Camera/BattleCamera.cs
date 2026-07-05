using System.Collections;
using Data;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;
using Zenject;

namespace Game.Cameras
{
    public class BattleCamera : MonoBehaviour, IBattleCamera
    {
        private const string ZoomPrefsKey = "BattleCamera.Zoom";

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

        [Header("Player Zoom")]
        [SerializeField] private float _defaultZoom = 1f;
        [SerializeField] private float _minZoom = 0.72f;
        [SerializeField] private float _maxZoom = 1.38f;
        [SerializeField] private float _zoomStep = 0.08f;
        [SerializeField] private float _zoomSmoothTime = 0.22f;

        [Header("Shake")]
        [SerializeField] private float _defaultShakeDuration = 0.18f;
        [SerializeField] private float _defaultShakeStrength = 0.35f;
        [SerializeField] private float _shakeFrequency = 35f;
        
        [Header("Lasso Camera FX")]
        [SerializeField] private Vector3 _launchKickOffset = new(0f, 0.32f, -0.65f);
        [SerializeField] private float _launchKickDuration = 0.2f;
        [SerializeField] private float _launchShakeDuration = 0.12f;
        [SerializeField] private float _launchShakeStrength = 0.18f;

        private Coroutine _shakeRoutine;
        private Coroutine _offsetKickRoutine;
        private Vector3 _currentKickOffset;
        private float _zoom;
        private float _targetZoom;
        private float _zoomVelocity;

        [Inject]
        public void Construct(GameSettings gameSettings)
        {
            CameraData cameraData = gameSettings.CameraData;

            if (cameraData == null)
                return;

            _defaultZoom = cameraData.DefaultZoom;
            _minZoom = cameraData.MinZoom;
            _maxZoom = cameraData.MaxZoom;
            _zoomStep = cameraData.ZoomStep;
            _zoomSmoothTime = cameraData.ZoomSmoothTime;
            _defaultShakeDuration = cameraData.DefaultShakeDuration;
            _defaultShakeStrength = cameraData.DefaultShakeStrength;
            _shakeFrequency = cameraData.ShakeFrequency;
            _launchKickOffset = cameraData.LaunchKickOffset;
            _launchKickDuration = cameraData.LaunchKickDuration;
            _launchShakeDuration = cameraData.LaunchShakeDuration;
            _launchShakeStrength = cameraData.LaunchShakeStrength;
        }

        private void Awake()
        {
            CacheComponents();
            LoadZoom();
            ApplySettings();
            MuteNoise();
        }

        private void Update()
        {
            TickZoom();
            ApplyDynamicCameraSettings();
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
                Vector3 position = target.position + ZoomedFollowOffset;
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

        public void PlayLassoLaunch(float chargeProgress)
        {
            float safeProgress = Mathf.Clamp01(chargeProgress);

            Shake(_launchShakeDuration, Mathf.Lerp(_launchShakeStrength * 0.65f, _launchShakeStrength, safeProgress));
            KickOffset(_launchKickOffset * Mathf.Lerp(0.65f, 1f, safeProgress), _launchKickDuration);
        }

        public void ZoomIn()
        {
            SetTargetZoom(_targetZoom - _zoomStep);
        }

        public void ZoomOut()
        {
            SetTargetZoom(_targetZoom + _zoomStep);
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

        private void KickOffset(Vector3 offset, float duration)
        {
            if (_offsetKickRoutine != null)
                StopCoroutine(_offsetKickRoutine);

            _offsetKickRoutine = StartCoroutine(OffsetKickRoutine(offset, duration));
        }

        private IEnumerator OffsetKickRoutine(Vector3 offset, float duration)
        {
            float safeDuration = Mathf.Max(duration, 0.01f);
            float timer = 0f;

            while (timer < safeDuration)
            {
                float progress = timer / safeDuration;
                float kickProgress = Mathf.Sin(progress * Mathf.PI);
                _currentKickOffset = offset * kickProgress;

                timer += Time.deltaTime;
                yield return null;
            }

            _currentKickOffset = Vector3.zero;
            _offsetKickRoutine = null;
        }

        private void ApplyDynamicCameraSettings()
        {
            if (_camera != null)
                _camera.Lens.FieldOfView = _fieldOfView;

            if (_follow != null)
                _follow.FollowOffset = ZoomedFollowOffset + _currentKickOffset;
        }

        private void LoadZoom()
        {
            _zoom = PlayerPrefs.GetFloat(ZoomPrefsKey, _defaultZoom);
            _zoom = Mathf.Clamp(_zoom, _minZoom, _maxZoom);
            _targetZoom = _zoom;
        }

        private void SetTargetZoom(float zoom)
        {
            _targetZoom = Mathf.Clamp(zoom, _minZoom, _maxZoom);
        }

        private void OnDestroy()
        {
            PlayerPrefs.SetFloat(ZoomPrefsKey, _targetZoom);
        }

        private void TickZoom()
        {
            if (Mathf.Approximately(_zoom, _targetZoom))
                return;

            _zoom = Mathf.SmoothDamp(_zoom, _targetZoom, ref _zoomVelocity,
                Mathf.Max(0.01f, _zoomSmoothTime), Mathf.Infinity, Time.unscaledDeltaTime);
        }

        private Vector3 ZoomedFollowOffset
        {
            get
            {
                float safeZoom = _zoom > 0f ? _zoom : Mathf.Clamp(_defaultZoom, _minZoom, _maxZoom);
                return _followOffset * safeZoom;
            }
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
            ApplyDynamicCameraSettings();

            if (_follow != null)
            {
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
