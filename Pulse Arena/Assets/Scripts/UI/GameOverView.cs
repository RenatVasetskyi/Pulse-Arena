using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    ///     Prefab-based end-of-game screen (used for both defeat and victory — only the title differs).
    ///     The window bounces in, the score counts up and the buttons pulse — all relative to each
    ///     element's designed scale and on unscaled time (the game is paused at timeScale 0).
    /// </summary>
    public class GameOverView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _mainMenuButton;
        [SerializeField] private RectTransform _window;
        private Vector3 _menuBaseScale = Vector3.one;
        private Vector3 _restartBaseScale = Vector3.one;

        private Vector3 _windowBaseScale = Vector3.one;
        private string _scoreFormat = "Score: {0}";
        public event Action MenuClicked;

        public event Action RestartClicked;

        private void Awake()
        {
            if (_window != null)
                _windowBaseScale = _window.localScale;

            if (_restartButton != null)
            {
                _restartBaseScale = _restartButton.transform.localScale;
                _restartButton.onClick.AddListener(OnRestart);
            }

            if (_mainMenuButton != null)
            {
                _menuBaseScale = _mainMenuButton.transform.localScale;
                _mainMenuButton.onClick.AddListener(OnMenu);
            }

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_restartButton != null)
                _restartButton.onClick.RemoveListener(OnRestart);

            if (_mainMenuButton != null)
                _mainMenuButton.onClick.RemoveListener(OnMenu);
        }

        public void Show(int score, string title, string scoreFormat)
        {
            _scoreFormat = scoreFormat;

            if (_titleText != null)
                _titleText.text = title;

            gameObject.SetActive(true);
            Animate(score);
        }

        private void Animate(int score)
        {
            AnimateWindowIn();
            AnimateScoreCountUp(score);
            PulseButton(_restartButton, _restartBaseScale, 1.05f, 0.7f);
            // gentler than Restart so it stays secondary
            PulseButton(_mainMenuButton, _menuBaseScale, 1.025f, 0.95f);
        }

        private void AnimateWindowIn()
        {
            if (_window == null)
                return;

            _window.DOKill();
            _window.localScale = Vector3.zero;
            _window.DOScale(_windowBaseScale, 0.5f).SetEase(Ease.OutBack).SetUpdate(true)
                .SetLink(_window.gameObject);
        }

        private void AnimateScoreCountUp(int score)
        {
            if (_scoreText == null)
                return;

            _scoreText.text = string.Format(_scoreFormat, 0);
            int shown = 0;
            DOTween.To(() => shown, value =>
                {
                    shown = value;
                    _scoreText.text = string.Format(_scoreFormat, shown);
                }, score, 0.6f)
                .SetEase(Ease.OutCubic).SetDelay(0.25f).SetUpdate(true).SetLink(gameObject);
        }

        private static void PulseButton(Button button, Vector3 baseScale, float amplitude, float duration)
        {
            if (button == null)
                return;

            Transform buttonTransform = button.transform;
            buttonTransform.DOKill();
            buttonTransform.localScale = baseScale;
            buttonTransform.DOScale(baseScale * amplitude, duration).SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetLink(button.gameObject);
        }

        private void OnRestart()
        {
            RestartClicked?.Invoke();
        }

        private void OnMenu()
        {
            MenuClicked?.Invoke();
        }
    }
}