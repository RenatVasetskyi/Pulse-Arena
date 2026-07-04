using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.MainMenu
{
    public class MainMenuView : MonoBehaviour
    {
        private const int ReferenceWidth = 1920;
        private const int ReferenceHeight = 1080;

        private CanvasGroup _canvasGroup;
        private Button _playButton;

        public event Action PlayClicked;

        public static MainMenuView Create()
        {
            GameObject root = new("MainMenuView");
            MainMenuView view = root.AddComponent<MainMenuView>();
            view.Build();

            return view;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }

        public void Hide()
        {
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.alpha = 0f;
        }

        public void Dispose()
        {
            Destroy(gameObject);
        }

        private void Build()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            Image background = CreateImage("Background", transform);
            background.color = new Color(0.05f, 0.06f, 0.08f, 1f);
            Stretch(background.rectTransform);

            Image vignette = CreateImage("Vignette", transform);
            vignette.color = new Color(0.08f, 0.13f, 0.2f, 0.72f);
            Stretch(vignette.rectTransform);

            RectTransform content = CreateRect("Content", transform);
            content.anchorMin = new Vector2(0.5f, 0.5f);
            content.anchorMax = new Vector2(0.5f, 0.5f);
            content.pivot = new Vector2(0.5f, 0.5f);
            content.sizeDelta = new Vector2(720f, 420f);
            content.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 28f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI title = CreateText("Title", content, "PULSE ARENA", 86f, FontStyles.Bold);
            title.color = new Color(0.9f, 0.98f, 1f, 1f);
            title.alignment = TextAlignmentOptions.Center;

            TextMeshProUGUI subtitle = CreateText("Subtitle", content, "Lasso enemies. Spin harder. Throw smarter.", 28f, FontStyles.Normal);
            subtitle.color = new Color(0.64f, 0.74f, 0.82f, 1f);
            subtitle.alignment = TextAlignmentOptions.Center;

            _playButton = CreateButton(content);
            _playButton.onClick.AddListener(OnPlayClicked);
        }

        private static RectTransform CreateRect(string objectName, Transform parent)
        {
            GameObject instance = new(objectName, typeof(RectTransform));
            instance.transform.SetParent(parent, false);
            return instance.GetComponent<RectTransform>();
        }

        private static Image CreateImage(string objectName, Transform parent)
        {
            GameObject instance = new(objectName, typeof(RectTransform), typeof(Image));
            instance.transform.SetParent(parent, false);
            return instance.GetComponent<Image>();
        }

        private static TextMeshProUGUI CreateText(
            string objectName,
            Transform parent,
            string text,
            float fontSize,
            FontStyles fontStyle)
        {
            GameObject instance = new(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            instance.transform.SetParent(parent, false);

            TextMeshProUGUI label = instance.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.enableAutoSizing = false;
            label.rectTransform.sizeDelta = new Vector2(720f, fontSize * 1.4f);

            return label;
        }

        private static Button CreateButton(Transform parent)
        {
            GameObject instance = new("PlayButton", typeof(RectTransform), typeof(Image), typeof(Button));
            instance.transform.SetParent(parent, false);

            RectTransform rect = instance.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(320f, 86f);

            Image image = instance.GetComponent<Image>();
            image.color = new Color(0.1f, 0.72f, 0.92f, 1f);

            Button button = instance.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.1f, 0.72f, 0.92f, 1f);
            colors.highlightedColor = new Color(0.18f, 0.86f, 1f, 1f);
            colors.pressedColor = new Color(0.06f, 0.52f, 0.72f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            TextMeshProUGUI label = CreateText("Label", instance.transform, "PLAY", 38f, FontStyles.Bold);
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            Stretch(label.rectTransform);

            return button;
        }

        private static void Stretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
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
