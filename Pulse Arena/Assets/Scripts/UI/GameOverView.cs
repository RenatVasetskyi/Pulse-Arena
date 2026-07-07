using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Prefab-based end-of-game screen (used for both defeat and victory — only the title differs).
    /// The window bounces in, the score counts up and the restart button pulses — all relative to
    /// each element's designed scale and on unscaled time (the game is paused at timeScale 0).
    /// </summary>
    public class GameOverView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _mainMenuButton;
        [SerializeField] private RectTransform _window;

        private Vector3 _windowBaseScale = Vector3.one;
        private Vector3 _restartBaseScale = Vector3.one;
        private Vector3 _menuBaseScale = Vector3.one;

        public event Action RestartClicked;
        public event Action MenuClicked;

        public void Show(int score, string title)
        {
            if (_titleText != null)
                _titleText.text = title;

            gameObject.SetActive(true);
            Animate(score);
        }

        private void Animate(int score)
        {
            if (_window != null)
            {
                _window.DOKill();
                _window.localScale = Vector3.zero;
                _window.DOScale(_windowBaseScale, 0.5f).SetEase(Ease.OutBack).SetUpdate(true)
                    .SetLink(_window.gameObject);
            }

            if (_scoreText != null)
            {
                _scoreText.text = "Score: 0";
                int shown = 0;
                DOTween.To(() => shown, value =>
                    {
                        shown = value;
                        _scoreText.text = $"Score: {shown}";
                    }, score, 0.6f)
                    .SetEase(Ease.OutCubic).SetDelay(0.25f).SetUpdate(true).SetLink(gameObject);
            }

            if (_restartButton != null)
            {
                Transform button = _restartButton.transform;
                button.DOKill();
                button.localScale = _restartBaseScale;
                button.DOScale(_restartBaseScale * 1.05f, 0.7f).SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetLink(_restartButton.gameObject);
            }

            if (_mainMenuButton != null)
            {
                Transform button = _mainMenuButton.transform;
                button.DOKill();
                button.localScale = _menuBaseScale;
                // gentler than Restart so it stays secondary
                button.DOScale(_menuBaseScale * 1.025f, 0.95f).SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetLink(_mainMenuButton.gameObject);
            }
        }

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
