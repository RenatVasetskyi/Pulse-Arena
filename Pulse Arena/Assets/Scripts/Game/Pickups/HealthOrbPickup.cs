using System;
using Architecture.Services.Interfaces;
using Data;
using DG.Tweening;
using Game.Player;
using UnityEngine;
using Zenject;

namespace Game.Pickups
{
    /// <summary>
    /// Floating health orb. The whole look (core, glow halo, rings, point light, trigger) lives on the
    /// prefab and is editable by hand; this component only drives the bob, spin, breathing glow and the
    /// collect pop. Assign the visual references on the prefab.
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
        private Vector3 _baseGlowScale = Vector3.one;
        private float _baseLightIntensity = 2.1f;
        private Vector3 _startPosition;
        private bool _collected;

        [Inject]
        public void Construct(GameSettings gameSettings, IAudioService audioService)
        {
            _pickupData = gameSettings.PickupData;
            _audioService = audioService;
        }

        public void Initialize()
        {
            _startPosition = transform.position;

            if (_innerGlow != null)
                _baseGlowScale = _innerGlow.localScale;

            if (_light != null)
                _baseLightIntensity = _light.intensity;
        }

        private void Update()
        {
            if (_pickupData == null || _collected)
                return;

            float bobOffset = Mathf.Sin(Time.time * _pickupData.BobSpeed) * _pickupData.BobHeight;
            transform.position = _startPosition + Vector3.up * bobOffset;

            if (_visualRoot != null)
                _visualRoot.Rotate(Vector3.up, _pickupData.RotateSpeed * Time.deltaTime, Space.World);

            if (_innerRing != null)
                _innerRing.Rotate(Vector3.forward, _pickupData.RotateSpeed * 1.3f * Time.deltaTime, Space.Self);

            if (_outerRing != null)
                _outerRing.Rotate(Vector3.right, -_pickupData.RotateSpeed * 0.9f * Time.deltaTime, Space.Self);

            UpdateGlow();
        }

        // Breathing glow: pulses the halo sphere + point light so the orb reads as "special".
        private void UpdateGlow()
        {
            float pulse = 1f + Mathf.Sin(Time.time * _pickupData.BobSpeed * 1.5f) * 0.22f;

            if (_innerGlow != null)
                _innerGlow.localScale = _baseGlowScale * pulse;

            if (_light != null)
                _light.intensity = _baseLightIntensity * pulse;
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
            PlayCollect();
        }

        private void PlayCollect()
        {
            _audioService?.PlaySfx(GameSfx.HealthPickup);

            if (_collider != null)
                _collider.enabled = false;

            // bright flash then snap to dark
            if (_light != null)
            {
                _light.DOKill();
                _light.DOIntensity(_baseLightIntensity * 3.2f, 0.05f)
                    .SetLink(gameObject)
                    .OnComplete(() =>
                    {
                        if (_light != null)
                            _light.DOIntensity(0f, 0.14f).SetLink(gameObject);
                    });
            }

            // small hop as it's "sucked in"
            transform.DOMoveY(transform.position.y + 0.5f, 0.2f).SetEase(Ease.OutQuad).SetLink(gameObject);

            if (_visualRoot == null)
            {
                Destroy(gameObject, 0.05f);
                return;
            }

            Vector3 baseScale = _visualRoot.localScale;
            _visualRoot.DOKill();

            Sequence sequence = DOTween.Sequence().SetLink(gameObject);
            sequence.Append(_visualRoot.DOScale(baseScale * 1.4f, 0.07f).SetEase(Ease.OutBack));   // quick pop
            sequence.Append(_visualRoot.DOScale(Vector3.zero, 0.12f).SetEase(Ease.InBack));        // fast collapse
            sequence.Join(_visualRoot.DOLocalRotate(new Vector3(0f, 260f, 0f), 0.12f,
                RotateMode.FastBeyond360).SetEase(Ease.InQuad));                                   // spin away
            sequence.OnComplete(() => Destroy(gameObject));
        }
    }
}
