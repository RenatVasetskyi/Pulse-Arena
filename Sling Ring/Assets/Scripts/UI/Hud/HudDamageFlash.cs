using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Hud
{
    /// <summary>
    ///     Full-screen red flash shown briefly when the player takes damage. Assign a stretched red
    ///     Image (raycast target off). Starts transparent.
    /// </summary>
    public class HudDamageFlash : MonoBehaviour
    {
        [SerializeField] private Image _flash;
        [SerializeField, Range(0f, 1f)] private float _peakAlpha = 0.32f;
        [SerializeField] private float _duration = 0.35f;

        private void Awake()
        {
            if (_flash != null)
                SetAlpha(0f);
        }

        public void Flash()
        {
            if (_flash == null)
                return;

            _flash.DOKill();
            SetAlpha(_peakAlpha);
            _flash.DOFade(0f, _duration).SetEase(Ease.OutQuad).SetLink(gameObject);
        }

        private void SetAlpha(float alpha)
        {
            Color color = _flash.color;
            color.a = alpha;
            _flash.color = color;
        }
    }
}