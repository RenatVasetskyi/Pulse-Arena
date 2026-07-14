using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace UI.Hud
{
    /// <summary>
    ///     "COMBO xN" popup. Pops in on each kill in the chain and fades out after the hold time (kept in
    ///     sync with the combo window). Assign the CanvasGroup + TMP label; starts hidden.
    /// </summary>
    public class HudComboView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private float _holdDuration = 0.7f;
        private Vector3 _baseScale = Vector3.one;
        private Coroutine _hideRoutine;

        private RectTransform _rect;

        private void Awake()
        {
            _rect = (RectTransform)(_label != null ? _label.transform : transform);
            _baseScale = _rect.localScale;

            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;
        }

        public void Hide()
        {
            if (_hideRoutine != null)
            {
                StopCoroutine(_hideRoutine);
                _hideRoutine = null;
            }

            FadeOut();
        }

        public void Show(int combo)
        {
            if (_rect == null)
                _rect = (RectTransform)(_label != null ? _label.transform : transform);

            if (_label != null)
                _label.text = $"COMBO x{combo}";

            if (_canvasGroup != null)
            {
                _canvasGroup.DOKill();
                _canvasGroup.alpha = 1f;
            }

            _rect.DOKill();
            _rect.localScale = _baseScale * 0.3f;
            _rect.localRotation = Quaternion.identity;

            // Aggressive impact: snap well past full size then settle back, with a quick rotational kick — reads as a
            // punchy hit rather than a gentle grow.
            Sequence pop = DOTween.Sequence().SetLink(gameObject);
            pop.Append(_rect.DOScale(_baseScale * 1.3f, 0.09f).SetEase(Ease.OutQuad));
            pop.Append(_rect.DOScale(_baseScale, 0.1f).SetEase(Ease.OutBack));
            pop.Insert(0f, _rect.DOPunchRotation(new Vector3(0f, 0f, 7f), 0.26f, 9, 1f));

            if (_hideRoutine != null)
                StopCoroutine(_hideRoutine);

            _hideRoutine = StartCoroutine(HideAfter(_holdDuration));
        }

        private IEnumerator HideAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            FadeOut();
            _hideRoutine = null;
        }

        // Snappy exit: a fast fade paired with a slight scale-up so it bursts away instead of lingering.
        private void FadeOut()
        {
            if (_canvasGroup != null)
                _canvasGroup.DOFade(0f, 0.14f).SetLink(gameObject);

            if (_rect != null)
            {
                _rect.DOKill();
                _rect.DOScale(_baseScale * 1.25f, 0.14f).SetEase(Ease.InBack).SetLink(gameObject);
            }
        }
    }
}