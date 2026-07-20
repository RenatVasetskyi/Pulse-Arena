using Architecture.Services.Interfaces;
using Data;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;
using Zenject;

namespace Game.Cameras
{
    /// <summary>
    ///     The thin Cinemachine orchestrator for the battle view: owns component wiring and the per-frame composite
    ///     (base framing + zoom + transient FX), and delegates to focused helpers — <see cref="CameraShaker" />
    ///     (Perlin shake), <see cref="CameraKickFx" /> (offset + FOV punch), <see cref="CameraZoomController" />
    ///     (settings-coupled zoom). Feel values come from <see cref="CameraData" />.
    /// </summary>
    public class BattleCamera : MonoBehaviour, IBattleCamera
    {
        private const float LaunchFeedbackMinChargeScale = 0.65f; // launch shake/kick strength at zero charge (→ 1 at full)
        private const float LaunchFovMinChargeScale = 0.6f; // FOV-punch scale at zero charge
        private const float DeathZoomLerp = 0.7f; // how far the death zoom pushes from DefaultZoom toward MinZoom

        [Header("Cinemachine")] [SerializeField]
        private CinemachineCamera _camera;

        [SerializeField] private CinemachineFollow _follow;
        [SerializeField] private CinemachineRotationComposer _rotationComposer;
        [SerializeField] private CinemachineBasicMultiChannelPerlin _noise;

        private readonly CameraKickFx _kick = new();
        private readonly CameraShaker _shaker = new();
        private readonly CameraZoomController _zoom = new();
        private float _baseFieldOfView;
        private Vector3 _baseFollowOffset;
        private Vector3 _baseLookAtOffset;
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
            CacheBaseRig();
            EnforceWorldSpaceBinding();
            _shaker.Initialize(_noise, _data.ShakeFrequency);
            _zoom.Initialize(_settings, _baseFollowOffset, _data);
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

        public void Follow(Transform target, bool snap = true)
        {
            if (_camera == null)
                return;

            _camera.Follow = target;
            _camera.LookAt = target;

            if (snap && target != null)
            {
                Vector3 position = target.position + _zoom.ZoomedFollowOffset;
                Quaternion rotation = Quaternion.LookRotation(target.position + _baseLookAtOffset - position);
                _camera.ForceCameraPosition(position, rotation);
            }
        }

        public void PlayLassoLaunch(float chargeProgress)
        {
            if (!CameraEffectsEnabled)
                return;

            float safeProgress = Mathf.Clamp01(chargeProgress);

            _shaker.Shake(_data.LaunchShakeDuration,
                Mathf.Lerp(_data.LaunchShakeStrength * LaunchFeedbackMinChargeScale, _data.LaunchShakeStrength, safeProgress));
            _kick.KickOffset(_data.LaunchKickOffset * Mathf.Lerp(LaunchFeedbackMinChargeScale, 1f, safeProgress), _data.LaunchKickDuration);
            _kick.FovPunch(_data.LaunchFovPunch * Mathf.Lerp(LaunchFovMinChargeScale, 1f, safeProgress), _data.LaunchFovDuration);
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

        // A dramatic, transient close-up on the player's death — a tight zoom (not persisted to settings) plus a
        // shake. The scene reload after game-over re-reads the saved zoom, so this never sticks.
        public void PlayDeathZoom()
        {
            _zoom.SetTargetZoom(Mathf.Lerp(_data.DefaultZoom, _data.MinZoom, DeathZoomLerp));

            if (CameraEffectsEnabled)
                _shaker.Shake(_data.PlayerHitShakeDuration, _data.PlayerHitShakeStrength);
        }

        private void OnSettingsChanged()
        {
            _zoom.SyncTargetFromSettings();
        }

        // Per-frame: base FOV/offset + the live zoom + the transient kick FX.
        private void ApplyComposite()
        {
            if (_camera != null)
                _camera.Lens.FieldOfView = _baseFieldOfView + _kick.CurrentFovKick;

            if (_follow != null)
                _follow.FollowOffset = _zoom.ZoomedFollowOffset + _kick.CurrentOffset;
        }

        // The base rig (FOV, follow/look-at offsets, damping) is authored on the Cinemachine components; snapshot
        // the composited-from values exactly once. Awake is the only legal capture point — ApplyComposite stomps
        // Lens.FieldOfView and FollowOffset every frame, so re-capturing later would fold zoom+kick into the base.
        private void CacheBaseRig()
        {
            if (_camera != null)
                _baseFieldOfView = _camera.Lens.FieldOfView;

            if (_follow != null)
                _baseFollowOffset = _follow.FollowOffset;

            if (_rotationComposer != null)
                _baseLookAtOffset = _rotationComposer.TargetOffset;
        }

        // Invariant: FollowOffset is composited and consumed as a WORLD-space vector (see ApplyComposite / Follow);
        // any LockToTarget* mode would make the rig orbit with the target's yaw. Cinemachine defaults a fresh
        // CinemachineFollow to LockToTargetOnAssign, so enforce WorldSpace here rather than trusting the asset.
        private void EnforceWorldSpaceBinding()
        {
            if (_follow != null)
                _follow.TrackerSettings.BindingMode = BindingMode.WorldSpace;
        }
    }
}