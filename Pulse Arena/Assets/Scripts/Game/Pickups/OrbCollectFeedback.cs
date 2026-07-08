using Architecture.Services.Interfaces;
using Data;
using DG.Tweening;
using UnityEngine;

namespace Game.Pickups
{
    /// <summary>
    /// The collect payoff: pickup SFX, a bright light flash that snaps to dark, a small "sucked-in" hop, and a
    /// pop / collapse / spin-away of the visual — then the orb destroys itself. Fired once when collected.
    /// </summary>
    public class OrbCollectFeedback
    {
        private Transform _self;
        private Transform _visualRoot;
        private Light _light;
        private SphereCollider _collider;
        private float _baseLightIntensity;
        private IAudioService _audio;

        public void Initialize(Transform self, Transform visualRoot, Light light,
            SphereCollider collider, float baseLightIntensity, IAudioService audio)
        {
            _self = self;
            _visualRoot = visualRoot;
            _light = light;
            _collider = collider;
            _baseLightIntensity = baseLightIntensity;
            _audio = audio;
        }

        public void Play()
        {
            _audio?.PlaySfx(GameSfx.HealthPickup);

            if (_collider != null)
                _collider.enabled = false;

            GameObject self = _self.gameObject;

            // bright flash then snap to dark
            if (_light != null)
            {
                _light.DOKill();
                _light.DOIntensity(_baseLightIntensity * 3.2f, 0.05f)
                    .SetLink(self)
                    .OnComplete(() =>
                    {
                        if (_light != null)
                            _light.DOIntensity(0f, 0.14f).SetLink(self);
                    });
            }

            // small hop as it's "sucked in"
            _self.DOMoveY(_self.position.y + 0.5f, 0.2f).SetEase(Ease.OutQuad).SetLink(self);

            if (_visualRoot == null)
            {
                Object.Destroy(self, 0.05f);
                return;
            }

            Vector3 baseScale = _visualRoot.localScale;
            _visualRoot.DOKill();

            Sequence sequence = DOTween.Sequence().SetLink(self);
            sequence.Append(_visualRoot.DOScale(baseScale * 1.4f, 0.07f).SetEase(Ease.OutBack));   // quick pop
            sequence.Append(_visualRoot.DOScale(Vector3.zero, 0.12f).SetEase(Ease.InBack));        // fast collapse
            sequence.Join(_visualRoot.DOLocalRotate(new Vector3(0f, 260f, 0f), 0.12f,
                RotateMode.FastBeyond360).SetEase(Ease.InQuad));                                   // spin away
            sequence.OnComplete(() => Object.Destroy(self));
        }
    }
}
