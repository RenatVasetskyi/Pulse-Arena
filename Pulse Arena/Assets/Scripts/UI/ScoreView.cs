using Architecture.Services.Interfaces;
using Data;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ScoreView : MonoBehaviour
    {
        private IScoreService _scoreService;
        private Text _scoreText;
        private Color _panelColor = new(0.06f, 0.07f, 0.1f, 0.62f);
        private Color _textColor = new(1f, 0.92f, 0.4f, 1f);

        public static ScoreView Create(IScoreService scoreService, UiData ui = null)
        {
            GameObject root = new("ScoreView", typeof(RectTransform));
            ScoreView view = root.AddComponent<ScoreView>();

            if (ui != null)
            {
                view._panelColor = ui.HudPanelColor;
                view._textColor = ui.ScoreTextColor;
            }

            view.Initialize(scoreService);
            return view;
        }

        private void Initialize(IScoreService scoreService)
        {
            _scoreService = scoreService;

            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform root = GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            GameObject panelObject = new("Panel", typeof(RectTransform));
            RectTransform panel = panelObject.GetComponent<RectTransform>();
            panel.SetParent(root, false);
            panel.anchorMin = new Vector2(1f, 1f);
            panel.anchorMax = new Vector2(1f, 1f);
            panel.pivot = new Vector2(1f, 1f);
            panel.anchoredPosition = new Vector2(-42f, -134f);
            panel.sizeDelta = new Vector2(280f, 72f);

            Image background = panelObject.AddComponent<Image>();
            background.color = _panelColor;
            background.raycastTarget = false;

            GameObject textObject = new("Value", typeof(RectTransform));
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(panel, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 0f);
            textRect.offsetMax = new Vector2(-16f, 0f);

            _scoreText = textObject.AddComponent<Text>();
            _scoreText.font = GetDefaultFont();
            _scoreText.fontSize = 36;
            _scoreText.fontStyle = FontStyle.Bold;
            _scoreText.alignment = TextAnchor.MiddleRight;
            _scoreText.color = _textColor;
            _scoreText.raycastTarget = false;

            _scoreService.ScoreChanged += UpdateView;
            UpdateView(_scoreService.Score);
        }

        private void OnDestroy()
        {
            if (_scoreService != null)
                _scoreService.ScoreChanged -= UpdateView;
        }

        private void UpdateView(int score)
        {
            if (_scoreText != null)
                _scoreText.text = $"Score: {score}";
        }

        private static Font GetDefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
