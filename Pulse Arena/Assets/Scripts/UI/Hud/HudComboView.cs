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
        [SerializeField] private float _holdDuration = 2.5f;
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
            _rect.localScale = _baseScale * 0.6f;
            _rect.DOScale(_baseScale, 0.28f).SetEase(Ease.OutBack).SetLink(gameObject);

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

        private void FadeOut()
        {
            if (_canvasGroup != null)
                _canvasGroup.DOFade(0f, 0.3f).SetLink(gameObject);
        }
    }
}