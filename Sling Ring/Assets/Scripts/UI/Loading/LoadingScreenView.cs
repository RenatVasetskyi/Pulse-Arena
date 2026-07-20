using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Loading
{
    /// <summary>
    ///     Prefab-based loading screen. Layout lives in the prefab; this script only
    ///     drives the fill bar, percentage label and fade. Assign the three refs in the prefab.
    /// </summary>
    public class LoadingScreenView : MonoBehaviour
    {
        private const float FadeDuration = 0.16f;
        private const float ProgressSpeed = 3.5f;

        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Slider _progressBar;
        [SerializeField] private TextMeshProUGUI _progressLabel;
        private Vector3 _labelBaseScale = Vector3.one;
        private int _lastPercent = -1;

        private float _targetProgress;
        private float _visibleProgress;

        private void Awake()
        {
            if (_progressLabel != null)
                _labelBaseScale = _progressLabel.transform.localScale;
        }

        private void Update()
        {
            _visibleProgress = Mathf.MoveTowards(
                _visibleProgress,
                _targetProgress,
                ProgressSpeed * Time.unscaledDeltaTime);

            ApplyProgress(_visibleProgress);
        }

        public IEnumerator FadeTo(float targetAlpha)
        {
            float startAlpha = _canvasGroup.alpha;
            float time = 0f;

            while (time < FadeDuration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / FadeDuration);
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            _canvasGroup.alpha = targetAlpha;
        }

        public void HideImmediate()
        {
            StopPulse();
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
            gameObject.SetActive(false);
        }

        public void SetProgress(float progress)
        {
            _targetProgress = Mathf.Clamp01(progress);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
            StartPulse();
        }

        public void ShowImmediate()
        {
            gameObject.SetActive(true);
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
            StartPulse();
        }

        private void StartPulse()
        {
            if (_progressLabel == null)
                return;

            Transform label = _progressLabel.transform;
            label.DOKill();
            label.localScale = _labelBaseScale;
            label.DOScale(_labelBaseScale * 1.1f, 0.55f).SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetLink(_progressLabel.gameObject);
        }

        private void StopPulse()
        {
            if (_progressLabel == null)
                return;

            _progressLabel.transform.DOKill();
            _progressLabel.transform.localScale = _labelBaseScale;
        }

        private void ApplyProgress(float progress)
        {
            if (_progressBar != null)
                _progressBar.value = progress;

            if (_progressLabel == null)
                return;

            int percent = Mathf.RoundToInt(progress * 100f);

            if (percent == _lastPercent)
                return;

            _lastPercent = percent;
            _progressLabel.text = $"{percent}%";
        }
    }
}