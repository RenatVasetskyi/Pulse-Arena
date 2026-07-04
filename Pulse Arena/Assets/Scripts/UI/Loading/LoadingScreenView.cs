using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Loading
{
    public class LoadingScreenView : MonoBehaviour
    {
        private const int ReferenceWidth = 1920;
        private const int ReferenceHeight = 1080;
        private const float FadeDuration = 0.16f;
        private const float ProgressSpeed = 3.5f;

        private CanvasGroup _canvasGroup;
        private Image _progressFill;
        private TextMeshProUGUI _progressLabel;

        private float _targetProgress;
        private float _visibleProgress;

        public static LoadingScreenView Create(Transform parent)
        {
            GameObject root = new("LoadingScreenView");
            root.transform.SetParent(parent, false);

            LoadingScreenView view = root.AddComponent<LoadingScreenView>();
            view.Build();

            return view;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
        }

        public void ShowImmediate()
        {
            gameObject.SetActive(true);
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
        }

        public void HideImmediate()
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
            gameObject.SetActive(false);
        }

        public void SetProgress(float progress)
        {
            _targetProgress = Mathf.Clamp01(progress);
        }

        public IEnumerator FadeTo(float targetAlpha)
        {
            float startAlpha = _canvasGroup.alpha;
            float time = 0f;

            while (time < FadeDuration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / FadeDuration);
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            _canvasGroup.alpha = targetAlpha;
        }

        private void Update()
        {
            _visibleProgress = Mathf.MoveTowards(
                _visibleProgress,
                _targetProgress,
                ProgressSpeed * Time.unscaledDeltaTime);

            ApplyProgress(_visibleProgress);
        }

        private void Build()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            Image background = CreateImage("Background", transform);
            background.color = new Color(0.035f, 0.04f, 0.052f, 1f);
            Stretch(background.rectTransform);

            RectTransform content = CreateRect("Content", transform);
            content.anchorMin = new Vector2(0.5f, 0.5f);
            content.anchorMax = new Vector2(0.5f, 0.5f);
            content.pivot = new Vector2(0.5f, 0.5f);
            content.sizeDelta = new Vector2(620f, 260f);
            content.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 24f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI title = CreateText("Title", content, "LOADING", 54f, FontStyles.Bold);
            title.color = new Color(0.86f, 0.96f, 1f, 1f);
            title.alignment = TextAlignmentOptions.Center;

            RectTransform barRoot = CreateRect("ProgressBar", content);
            barRoot.sizeDelta = new Vector2(520f, 18f);

            Image barBackground = barRoot.gameObject.AddComponent<Image>();
            barBackground.color = new Color(0.12f, 0.17f, 0.22f, 1f);

            _progressFill = CreateImage("Fill", barRoot);
            _progressFill.color = new Color(0.1f, 0.72f, 0.92f, 1f);
            _progressFill.type = Image.Type.Filled;
            _progressFill.fillMethod = Image.FillMethod.Horizontal;
            _progressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            Stretch(_progressFill.rectTransform);

            _progressLabel = CreateText("ProgressLabel", content, "0%", 24f, FontStyles.Normal);
            _progressLabel.color = new Color(0.6f, 0.74f, 0.82f, 1f);
            _progressLabel.alignment = TextAlignmentOptions.Center;

            ApplyProgress(0f);
        }

        private void ApplyProgress(float progress)
        {
            if (_progressFill != null)
                _progressFill.fillAmount = progress;

            if (_progressLabel != null)
                _progressLabel.text = $"{Mathf.RoundToInt(progress * 100f)}%";
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
            label.rectTransform.sizeDelta = new Vector2(620f, fontSize * 1.35f);

            return label;
        }

        private static void Stretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
