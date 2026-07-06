using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Prefab-based end-of-game screen (used for both defeat and victory — only the title differs).
    /// Assign _titleText, _scoreText and _restartButton in the prefab. Starts hidden.
    /// </summary>
    public class GameOverView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private Button _restartButton;

        public event Action RestartClicked;

        public void Show(int score, string title)
        {
            if (_titleText != null)
                _titleText.text = title;

            if (_scoreText != null)
                _scoreText.text = $"Score: {score}";

            gameObject.SetActive(true);
        }

        private void Awake()
        {
            if (_restartButton != null)
                _restartButton.onClick.AddListener(OnRestart);

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_restartButton != null)
                _restartButton.onClick.RemoveListener(OnRestart);
        }

        private void OnRestart()
        {
            RestartClicked?.Invoke();
        }
    }
}
