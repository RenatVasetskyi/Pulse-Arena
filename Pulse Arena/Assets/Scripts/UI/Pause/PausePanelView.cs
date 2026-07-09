using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Pause
{
    /// <summary>
    ///     Self-contained pause overlay (own canvas). Raises an event per button; the PauseController wires
    ///     them. Window pops in on unscaled time (the game is frozen at timeScale 0 while paused).
    /// </summary>
    public class PausePanelView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _window;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _menuButton;
        public event Action MenuClicked;
        public event Action RestartClicked;

        public event Action ResumeClicked;
        public event Action SettingsClicked;

        private void Awake()
        {
            if (_resumeButton != null) _resumeButton.onClick.AddListener(() => ResumeClicked?.Invoke());
            if (_settingsButton != null) _settingsButton.onClick.AddListener(() => SettingsClicked?.Invoke());
            if (_restartButton != null) _restartButton.onClick.AddListener(() => RestartClicked?.Invoke());
            if (_menuButton != null) _menuButton.onClick.AddListener(() => MenuClicked?.Invoke());
        }

        public void Hide()
        {
            SetVisible(false);
            gameObject.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            SetVisible(true);

            if (_window != null)
            {
                _window.DOKill();
                _window.localScale = Vector3.one * 0.7f;
                _window.DOScale(Vector3.one, 0.3f).SetUpdate(true).SetEase(Ease.OutBack).SetLink(gameObject);
            }
        }

        private void SetVisible(bool visible)
        {
            if (_canvasGroup == null)
                return;

            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
        }
    }
}