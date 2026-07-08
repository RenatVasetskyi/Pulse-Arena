using System;
using Architecture.Services.Interfaces;
using Data;
using Game.Player;
using UnityEngine;
using Zenject;

namespace Game.Pickups
{
    /// <summary>
    /// Floating health orb. The whole look (core, glow halo, rings, point light, trigger) lives on the prefab
    /// and is editable by hand; this component only wires the two presentation helpers and owns the collect
    /// gameplay decision (heal the player on touch). Assign the visual references on the prefab.
    /// </summary>
    public class HealthOrbPickup : MonoBehaviour
    {
        public event Action<HealthOrbPickup> Collected;

        [Header("Visual (assigned on the prefab)")]
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private Transform _innerGlow;
        [SerializeField] private Transform _innerRing;
        [SerializeField] private Transform _outerRing;
        [SerializeField] private Light _light;
        [SerializeField] private SphereCollider _collider;

        private PickupData _pickupData;
        private IAudioService _audioService;
        private readonly OrbIdleAnimator _idle = new();
        private readonly OrbCollectFeedback _collect = new();
        private bool _collected;

        [Inject]
        public void Construct(GameSettings gameSettings, IAudioService audioService)
        {
            _pickupData = gameSettings.PickupData;
            _audioService = audioService;
        }

        public void Initialize()
        {
            _idle.Initialize(transform, _visualRoot, _innerGlow, _innerRing, _outerRing, _light, _pickupData);
            _collect.Initialize(transform, _visualRoot, _light, _collider,
                _light != null ? _light.intensity : 0f, _audioService);
        }

        private void Update()
        {
            if (_pickupData == null || _collected)
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
