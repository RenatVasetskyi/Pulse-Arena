using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class RarePickupToastView : MonoBehaviour
    {
        private const float FadeDuration = 0.18f;

        private CanvasGroup _canvasGroup;
        private Text _messageText;
        private Coroutine _showRoutine;

        public static RarePickupToastView Create()
        {
            GameObject root = new("Rare Pickup Toast View");
            RarePickupToastView view = root.AddComponent<RarePickupToastView>();
            view.BuildView();
            return view;
        }

        public void Show(string message, float duration)
        {
            if (_showRoutine != null)
                StopCoroutine(_showRoutine);

            _showRoutine = StartCoroutine(ShowRoutine(message, duration));
        }

        private void BuildView()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            GameObject panel = new("Toast Panel");
            panel.transform.SetParent(transform, false);

            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = new Vector2(0f, -88f);
            panelRect.sizeDelta = new Vector2(560f, 72f);

            Image background = panel.AddComponent<Image>();
            background.color = new Color(0.05f, 0.12f, 0.08f, 0.82f);

            _messageText = CreateText(panel.transform);
        }

        private static Text CreateText(Transform parent)
        {
            GameObject textObject = new("Message");
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(24f, 0f);
            rect.offsetMax = new Vector2(-24f, 0f);

            Text text = textObject.AddComponent<Text>();
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.78f, 1f, 0.68f, 1f);
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.font = font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 30;
            text.fontStyle = FontStyle.Bold;
            text.raycastTarget = false;

            return text;
        }

        private IEnumerator ShowRoutine(string message, float duration)
        {
            _messageText.text = message;
            yield return FadeTo(1f, FadeDuration);
            yield return new WaitForSeconds(Mathf.Max(0.1f, duration));
            yield return FadeTo(0f, FadeDuration);
            _showRoutine = null;
        }

        private IEnumerator FadeTo(float targetAlpha, float duration)
        {
            float startAlpha = _canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                yield return null;
            }

            _canvasGroup.alpha = targetAlpha;
        }
    }
}
