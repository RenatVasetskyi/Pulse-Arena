using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UI.MainMenu
{
    /// <summary>
    ///     Prefab-based main menu. Wires the Play button, controls visibility and adds entrance juice
    ///     (title pop-in + Play pulse) relative to each element's designed scale so nothing is resized.
    ///     Assign _canvasGroup, _playButton and (optionally) _title in the prefab inspector.
    /// </summary>
    public class MainMenuView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _resetButton;
        [SerializeField] private ConfirmDialogView _confirmDialog;
        [SerializeField] private RectTransform _title;
        private Vector3 _playButtonBaseScale = Vector3.one;
        private Vector3 _settingsButtonBaseScale = Vector3.one;

        private Vector3 _titleBaseScale = Vector3.one;

        public event Action PlayClicked;
        public event Action ResetClicked;
        public event Action SettingsClicked;

        /// <summary>The reset-progress confirmation modal — the presenter drives its Confirmed/Cancelled + Show/Hide.</summary>
        public ConfirmDialogView ConfirmDialog => _confirmDialog;

        private void Awake()
        {
            if (_playButton != null)
            {
                _playButtonBaseScale = _playButton.transform.localScale;
                _playButton.onClick.AddListener(OnPlayClicked);
            }

            if (_settingsButton != null)
            {
                _settingsButtonBaseScale = _settingsButton.transform.localScale;
                _settingsButton.onClick.AddListener(OnSettingsClicked);
            }

            if (_resetButton != null)
                _resetButton.onClick.AddListener(OnResetClicked);

            if (_title != null)
                _titleBaseScale = _title.localScale;
        }

        private void Start()
        {
            if (gameObject.activeInHierarchy)
                PlayIntro();
        }

        private void OnDestroy()
        {
            if (_playButton != null)
                _playButton.onClick.RemoveListener(OnPlayClicked);

            if (_settingsButton != null)
                _settingsButton.onClick.RemoveListener(OnSettingsClicked);

            if (_resetButton != null)
                _resetButton.onClick.RemoveListener(OnResetClicked);
        }

        public void Dispose()
        {
            if (this != null)
                Destroy(gameObject);
        }

        public void Hide()
        {
            StopTweens();

            if (_canvasGroup == null)
                return;

            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.alpha = 0f;
        }

        public void Show()
        {
            gameObject.SetActive(true);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }

            PlayIntro();
        }

        private void PlayIntro()
        {
            AnimateTitleIn();
            PulseButton(_playButton, _playButtonBaseScale, 1.06f, 0.8f);
            // gentler than Play: smaller amplitude + slower, so it breathes without stealing focus
            PulseButton(_settingsButton, _settingsButtonBaseScale, 1.03f, 1.1f);
        }

        private void AnimateTitleIn()
        {
            if (_title == null)
                return;

            _title.DOKill();
            _title.localScale = Vector3.zero;
            _title.DOScale(_titleBaseScale, 0.5f).SetEase(Ease.OutBack).SetLink(_title.gameObject);
        }

        private static void PulseButton(Button button, Vector3 baseScale, float amplitude, float duration)
        {
            if (button == null)
                return;

            Transform buttonTransform = button.transform;
            buttonTransform.DOKill();
            buttonTransform.localScale = baseScale;
            buttonTransform.DOScale(baseScale * amplitude, duration).SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo).SetLink(button.gameObject);
        }

        private void StopTweens()
        {
            if (_title != null)
            {
                _title.DOKill();
                _title.localScale = _titleBaseScale;
            }

            if (_playButton != null)
            {
                _playButton.transform.DOKill();
                _playButton.transform.localScale = _playButtonBaseScale;
            }

            if (_settingsButton != null)
            {
                _settingsButton.transform.DOKill();
                _settingsButton.transform.localScale = _settingsButtonBaseScale;
            }
        }

        private void OnPlayClicked()
        {
            PlayClicked?.Invoke();
        }

        private void OnSettingsClicked()
        {
            SettingsClicked?.Invoke();
        }

        private void OnResetClicked()
        {
            ResetClicked?.Invoke();
        }
    }
}