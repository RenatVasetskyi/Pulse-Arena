using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace UI.Hud
{
    /// <summary>
    ///     The first-run onboarding banner: a single styled card that pops in with a hint and either holds until the
    ///     player performs the taught action (<paramref name="autoHide" /> &lt;= 0, learn-by-doing) or fades out after
    ///     a beat. To stay readable when the player blitzes through steps, a freshly shown hint is guaranteed a
    ///     minimum time on screen (<see cref="MinReadTime" />) before the NEXT hint can replace it — a same-frame
    ///     grab→fling won't flash the second prompt away unread. Passive: the onboarding controller decides WHAT/WHEN.
    /// </summary>
    public class HudHintView : MonoBehaviour
    {
        // A just-shown hint stays at least this long (unscaled) before a queued switch replaces it.
        private const float MinReadTime = 1.8f;

        [SerializeField] private RectTransform _panel;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private TextMeshProUGUI _label;

        private Sequence _sequence;
        private Vector3 _baseScale = Vector3.one;
        private float _shownAt = -99f;
        private Coroutine _pending;
        private string _pendingMessage;
        private float _pendingAutoHide;

        private void Awake()
        {
            if (_panel != null)
                _baseScale = _panel.localScale;

            if (_group != null)
                _group.alpha = 0f;
        }

        /// <summary>Show a hint. <paramref name="autoHide" /> &lt;= 0 keeps it up until the next Show/Hide.</summary>
        public void Show(string message, float autoHide)
        {
            float elapsed = Time.unscaledTime - _shownAt;

            // Something is showing that the player hasn't had time to read — hold it, queue this switch until it has.
            if (IsVisible() && elapsed < MinReadTime)
            {
                _pendingMessage = message;
                _pendingAutoHide = autoHide;

                if (_pending == null)
                    _pending = StartCoroutine(ShowQueued(MinReadTime - elapsed));

                return;
            }

            ShowNow(message, autoHide);
        }

        public void Hide()
        {
            CancelPending();

            if (_group == null)
                return;

            _sequence?.Kill();
            _sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            _sequence.Append(_group.DOFade(0f, 0.2f));
        }

        private bool IsVisible()
        {
            return _group != null && _group.alpha > 0.01f;
        }

        private IEnumerator ShowQueued(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            _pending = null;
            ShowNow(_pendingMessage, _pendingAutoHide);
        }

        private void CancelPending()
        {
            if (_pending == null)
                return;

            StopCoroutine(_pending);
            _pending = null;
        }

        private void ShowNow(string message, float autoHide)
        {
            if (_label == null || _group == null || _panel == null)
                return;

            _shownAt = Time.unscaledTime;
            _sequence?.Kill();
            _label.text = message;
            _group.alpha = 0f;
            _panel.localScale = _baseScale * 0.9f;

            _sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            _sequence.Append(_group.DOFade(1f, 0.2f));
            _sequence.Join(_panel.DOScale(_baseScale, 0.28f).SetEase(Ease.OutBack));

            if (autoHide > 0f)
            {
                _sequence.AppendInterval(autoHide);
                _sequence.Append(_group.DOFade(0f, 0.28f).SetEase(Ease.InQuad));
            }
        }
    }
}
