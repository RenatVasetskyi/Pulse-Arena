using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace UI
{
    public class GameOverView : MonoBehaviour
    {
        private const float PanelAlpha = 0.78f;

        private Text _scoreText;
        private Button _restartButton;
        private Action _restartClicked;

        public static GameOverView Create(Action restartClicked)
        {
            GameObject root = new("GameOverView");
            GameOverView view = root.AddComponent<GameOverView>();
            view.Initialize(restartClicked);
            return view;
        }

        public void Show(int score)
        {
            if (_scoreText != null)
                _scoreText.text = $"Score: {score}";

            gameObject.SetActive(true);
        }

        private void Initialize(Action restartClicked)
        {
            _restartClicked = restartClicked;

            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();

            RectTransform root = GetComponent<RectTransform>();
            Stretch(root);

            CreateBackground(root);
            CreateContent(root);

            gameObject.SetActive(false);
        }

        private void CreateBackground(RectTransform root)
        {
            Image background = CreateImage("Background", root, new Color(0.03f, 0.035f, 0.05f, PanelAlpha));
            Stretch(background.rectTransform);
        }

        private void CreateContent(RectTransform root)
        {
            RectTransform panel = CreateRect("Panel", root);
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(560f, 420f);
            panel.anchoredPosition = Vector2.zero;

            Image panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0.08f, 0.09f, 0.12f, 0.94f);

            Text title = CreateText("Title", panel, "GAME OVER", 68, FontStyle.Bold, Color.white);
            title.alignment = TextAnchor.MiddleCenter;
            SetRect(title.rectTransform, new Vector2(0f, 110f), new Vector2(520f, 90f));

            _scoreText = CreateText("Score", panel, "Score: 0", 36, FontStyle.Normal,
                new Color(0.78f, 0.86f, 1f, 1f));
            _scoreText.alignment = TextAnchor.MiddleCenter;
            SetRect(_scoreText.rectTransform, new Vector2(0f, 28f), new Vector2(520f, 70f));

            _restartButton = CreateButton(panel);
            SetRect(_restartButton.GetComponent<RectTransform>(), new Vector2(0f, -105f), new Vector2(360f, 92f));
        }

        private Button CreateButton(RectTransform parent)
        {
            Image buttonImage = CreateImage("RestartButton", parent, new Color(0.16f, 0.64f, 1f, 1f));
            Button button = buttonImage.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(OnRestartClicked);

            Text label = CreateText("Label", buttonImage.rectTransform, "Restart", 34, FontStyle.Bold, Color.white);
            label.alignment = TextAnchor.MiddleCenter;
            Stretch(label.rectTransform);

            return button;
        }

        private void OnRestartClicked()
        {
            _restartClicked?.Invoke();
        }

        private static Image CreateImage(string name, RectTransform parent, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(string name, RectTransform parent, string value, int size,
            FontStyle style, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            Text text = rect.gameObject.AddComponent<Text>();
            text.text = value;
            text.font = GetDefaultFont();
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(12, size / 2);
            text.resizeTextMaxSize = size;
            return text;
        }

        private static RectTransform CreateRect(string name, RectTransform parent)
        {
            GameObject child = new(name, typeof(RectTransform));
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static Font GetDefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;

            GameObject eventSystem = new("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }
    }
}
