using DG.Tweening;
using TMPro;
using UnityEngine;

namespace UI.Hud
{
    /// <summary>
    ///     Text-only HUD toast (e.g. "Rare Health Orb spawned!"). Pops in with a bounce, holds, then
    ///     floats up and fades out. Assign the CanvasGroup (fade) and the TMP label. Starts hidden.
    /// </summary>
    public class HudToastView : MonoBehaviour
    {
        private const float PopDuration = 0.32f;
        private const float OutDuration = 0.45f;
        private const float RiseDistance = 46f;

        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TextMeshProUGUI _label;
        private Vector2 _basePosition;
        private Vector3 _baseScale;

        private RectTransform _rect;
        private Sequence _sequence;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _basePosition = _rect.anchoredPosition;
            _baseScale = _rect.localScale;

            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;
        }

        public void Show(string message, float duration)
        {
            if (_rect == null)
                _rect = (RectTransform)transform;

            if (_label != null)
                _label.text = message;

            _sequence?.Kill();
            ResetForShow();
            _sequence = BuildToastSequence(duration);
        }

        private void ResetForShow()
        {
            _rect.anchoredPosition = _basePosition;
            _rect.localScale = _baseScale * 0.6f;

            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;
        }

        private Sequence BuildToastSequence(float duration)
        {
            Sequence sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);

            sequence.Append(_rect.DOScale(_baseScale, PopDuration).SetEase(Ease.OutBack));

            if (_canvasGroup != null)
                sequence.Join(_canvasGroup.DOFade(1f, PopDuration * 0.6f));

            sequence.AppendInterval(Mathf.Max(0.1f, duration));

            sequence.Append(_rect.DOAnchorPosY(_basePosition.y + RiseDistance, OutDuration).SetEase(Ease.InQuad));

            if (_canvasGroup != null)
                sequence.Join(_canvasGroup.DOFade(0f, OutDuration));

            sequence.OnComplete(() =>
            {
                _rect.anchoredPosition = _basePosition;
                _rect.localScale = _baseScale;
            });

            return sequence;
        }
    }
}