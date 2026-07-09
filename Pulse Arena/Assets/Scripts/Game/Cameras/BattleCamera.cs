using Architecture.Services.Interfaces;
using Data;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;
using Zenject;

namespace Game.Cameras
{
    /// <summary>
    ///     The thin Cinemachine orchestrator for the battle view. It owns the component wiring + the per-frame
    ///     composite (base framing + zoom + transient FX) + the high-level choreography (lasso launch / player
    ///     hit), and delegates the three independent effects to focused helpers: <see cref="CameraShaker" />
    ///     (Perlin shake), <see cref="CameraKickFx" /> (offset + FOV punch) and <see cref="CameraZoomController" />
    ///     (the settings-coupled zoom axis). Feel/balance values are read straight from <see cref="CameraData" />;
    ///     only the base rig setup (offsets/damping/FOV) lives on the component.
    /// </summary>
    public class BattleCamera : MonoBehaviour, IBattleCamera
    {
        [Header("Cinemachine")] [SerializeField]
        private CinemachineCamera _camera;

        [SerializeField] private CinemachineFollow _follow;
        [SerializeField] private CinemachineRotationComposer _rotationComposer;
        [SerializeField] private CinemachineBasicMultiChannelPerlin _noise;

        [Header("Battle View (rig setup)")] [SerializeField]
        private Vector3 _followOffset = new(0f, 14f, -11f);

        [SerializeField] private Vector3 _lookAtOffset = new(0f, 0.8f, 0f);
        [SerializeField] private Vector3 _positionDamping = new(0.12f, 0.18f, 0.12f);
        [SerializeField] private Vector2 _rotationDamping = new(0.25f, 0.25f);
        [SerializeField] private float _fieldOfView = 55f;
        private readonly CameraKickFx _kick = new();
        private readonly CameraShaker _shaker = new();
        private readonly CameraZoomController _zoom = new();
        private CameraData _data;

        private ISettingsService _settings;

        private bool CameraEffectsEnabled => _settings == null || _settings.CameraEffectsEnabled;

        [Inject]
        public void Construct(GameSettings gameSettings, ISettingsService settings)
        {
            _settings = settings;
            _data = gameSettings.CameraData;

            if (_settings != null)
                _settings.Changed += OnSettingsChanged;
        }

        private void Awake()
        {
            CacheComponents();
            _shaker.Initialize(_noise, _data.ShakeFrequency);
            _zoom.Initialize(_settings, _followOffset, _data);
            ApplySettings();
        }

        private void Update()
        {
            _zoom.Tick();
            _shaker.Tick(Time.deltaTime);
            _kick.Tick(Time.deltaTime);
            ApplyComposite();
        }

        private void OnDestroy()
        {
            if (_settings != null)
                _settings.Changed -= OnSettingsChanged;
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
                Vector3 position = target.position + _zoom.ZoomedFollowOffset;
                Quaternion rotation = Quaternion.LookRotation(target.position + _lookAtOffset - position);
                _camera.ForceCameraPosition(position, rotation);
            }
        }

        public void PlayLassoLaunch(float chargeProgress)
        {
            if (!CameraEffectsEnabled)
                return;

            float safeProgress = Mathf.Clamp01(chargeProgress);

            _shaker.Shake(_data.LaunchShakeDuration,
                Mathf.Lerp(_data.LaunchShakeStrength * 0.65f, _data.LaunchShakeStrength, safeProgress));
            _kick.KickOffset(_data.LaunchKickOffset * Mathf.Lerp(0.65f, 1f, safeProgress), _data.LaunchKickDuration);
            _kick.FovPunch(_data.LaunchFovPunch * Mathf.Lerp(0.6f, 1f, safeProgress), _data.LaunchFovDuration);
        }

        public void PlayPlayerHit()
        {
            Shake(_data.PlayerHitShakeDuration, _data.PlayerHitShakeStrength);
        }

        public void Shake(float duration, float strength)
        {
            if (!CameraEffectsEnabled)
                return;

            _shaker.Shake(duration, strength);
        }

        public void Shake()
        {
            Shake(_data.DefaultShakeDuration, _data.DefaultShakeStrength);
        }

        public void ZoomIn()
        {
            _zoom.ZoomIn();
        }

        public void ZoomOut()
        {
            _zoom.ZoomOut();
        }

        private void OnSettingsChanged()
        {
            _zoom.SyncTargetFromSettings();
        }

        // Per-frame: base FOV/offset + the live zoom + the transient kick FX.
        private void ApplyComposite()
        {
            if (_camera != null)
                _camera.Lens.FieldOfView = _fieldOfView + _kick.CurrentFovKick;

            if (_follow != null)
                _follow.FollowOffset = _zoom.ZoomedFollowOffset + _kick.CurrentOffset;
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

        // Base rig framing applied on Awake + in the editor (OnValidate). Update refreshes the zoom composite
        // every frame at runtime, but the editor doesn't run Update, so this seeds a correct base FollowOffset.
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
        }
    }
}