using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.MainMenu
{
    /// <summary>
    /// Prefab-based main menu. The layout lives in the prefab (built in the editor);
    /// this script only wires the Play button and controls visibility.
    /// Assign _canvasGroup and _playButton in the prefab inspector.
    /// </summary>
    public class MainMenuView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Button _playButton;

        public event Action PlayClicked;

        public void Show()
        {
            gameObject.SetActive(true);

            if (_canvasGroup == null)
                return;

            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }

        public void Hide()
        {
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

        private void Awake()
        {
            if (_playButton != null)
                _playButton.onClick.AddListener(OnPlayClicked);
        }

        private void OnDestroy()
        {
            if (_playButton != null)
                _playButton.onClick.RemoveListener(OnPlayClicked);
        }

        private void OnPlayClicked()
        {
            PlayClicked?.Invoke();
        }
    }
}
