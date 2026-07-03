using Architecture.Services.Interfaces;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace UI
{
    public class ScoreView : MonoBehaviour
    {
        [SerializeField] private Text _scoreText;

        private IScoreService _scoreService;

        [Inject]
        public void Construct(IScoreService scoreService)
        {
            _scoreService = scoreService;
        }

        private void OnEnable()
        {
            if (_scoreService != null)
                _scoreService.ScoreChanged += UpdateView;
        }

        private void OnDisable()
        {
            if (_scoreService != null)
                _scoreService.ScoreChanged -= UpdateView;
        }

        private void Start()
        {
            UpdateView(_scoreService.Score);
        }

        private void UpdateView(int score)
        {
            if (_scoreText != null)
                _scoreText.text = score.ToString();
        }
    }
}
