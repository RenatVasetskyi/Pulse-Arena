using System;
using Architecture.Services.Interfaces;
using Data;
using Game.Player;
using UnityEngine;
using Zenject;

namespace Game.Pickups
{
    /// <summary>
    ///     Floating health orb: the visuals live on the prefab; this component wires the two presentation helpers
    ///     (<see cref="OrbIdleAnimator" /> + <see cref="OrbCollectFeedback" />) and owns the collect decision
    ///     (heal the player on touch).
    /// </summary>
    public class HealthOrbPickup : MonoBehaviour, IPausable
    {
        [Header("Visual (assigned on the prefab)")] [SerializeField]
        private Transform _visualRoot;

        [SerializeField] private Transform _innerGlow;
        [SerializeField] private Transform _innerRing;
        [SerializeField] private Transform _outerRing;
        [SerializeField] private Light _light;
        [SerializeField] private SphereCollider _collider;
        private readonly OrbCollectFeedback _collect = new();
        private readonly OrbIdleAnimator _idle = new();
        private IAudioService _audioService;
        private bool _collected;
        private bool _paused;

        private PickupData _pickupData;
        private IPauseService _pauseService;
        public event Action<HealthOrbPickup> Collected;

        [Inject]
        public void Construct(GameSettings gameSettings, IAudioService audioService, IPauseService pauseService)
        {
            _pickupData = gameSettings.PickupData;
            _audioService = audioService;
            _pauseService = pauseService;
        }

        public void Initialize()
        {
            _idle.Initialize(transform, _visualRoot, _innerGlow, _innerRing, _outerRing, _light, _pickupData);
            _collect.Initialize(transform, _visualRoot, _light, _collider,
                _light != null ? _light.intensity : 0f, _audioService);
            _pauseService?.Register(this);
        }

        private void OnDestroy()
        {
            _pauseService?.Unregister(this);
        }

        /// <summary>Mechanical pause: freeze the orb's idle bob/glow on the current frame.</summary>
        public void Pause()
        {
            _paused = true;
        }

        public void Resume()
        {
            _paused = false;
        }

        private void Update()
        {
            if (_paused || _pickupData == null || _collected)
                return;

            _idle.Tick();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_collected)
                return;

            PlayerController player = other.GetComponentInParent<PlayerController>();

            if (player == null)
                return;

            if (!player.TryHeal(_pickupData.HealthAmount))
                return;

            _collected = true;
            Collected?.Invoke(this);
            _collect.Play();
        }
    }
}