using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UI.MainMenu
{
    /// <summary>
    /// Prefab-based main menu. Wires the Play button, controls visibility and adds entrance juice
    /// (title pop-in + Play pulse) relative to each element's designed scale so nothing is resized.
    /// Assign _canvasGroup, _playButton and (optionally) _title in the prefab inspector.
    /// </summary>
    public class MainMenuView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private RectTransform _title;

        private Vector3 _titleBaseScale = Vector3.one;
        private Vector3 _playButtonBaseScale = Vector3.one;
        private Vector3 _settingsButtonBaseScale = Vector3.one;

        public event Action PlayClicked;
        public event Action SettingsClicked;

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

        public void Hide()
        {
            StopTweens();

            if (_canvasGroup == null)
                return;

            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.alpha = 0f;
        }

        public void Dispose()
        {
            if (this != null)
                Destroy(gameObject);
        }

        private void PlayIntro()
        {
            if (_title != null)
            {
                _title.DOKill();
                _title.localScale = Vector3.zero;
                _title.DOScale(_titleBaseScale, 0.5f).SetEase(Ease.OutBack).SetLink(_title.gameObject);
            }

            if (_playButton != null)
            {
                Transform button = _playButton.transform;
                button.DOKill();
                button.localScale = _playButtonBaseScale;
                button.DOScale(_playButtonBaseScale * 1.06f, 0.8f).SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo).SetLink(_playButton.gameObject);
            }

            if (_settingsButton != null)
            {
                Transform button = _settingsButton.transform;
                button.DOKill();
                button.localScale = _settingsButtonBaseScale;
                // gentler than Play: smaller amplitude + slower, so it breathes without stealing focus
                button.DOScale(_settingsButtonBaseScale * 1.03f, 1.1f).SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo).SetLink(_settingsButton.gameObject);
            }
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
        }

        private void OnPlayClicked()
        {
            PlayClicked?.Invoke();
        }

        private void OnSettingsClicked()
        {
            SettingsClicked?.Invoke();
        }
    }
}
