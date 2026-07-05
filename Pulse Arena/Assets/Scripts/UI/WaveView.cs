using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class WaveView : MonoBehaviour
    {
        private Text _text;
        private RectTransform _panel;

        public static WaveView Create()
        {
            GameObject root = new("WaveView", typeof(RectTransform));
            WaveView view = root.AddComponent<WaveView>();
            view.Build();
            return view;
        }

        public void SetWave(int current, int total)
        {
            if (_panel != null && !_panel.gameObject.activeSelf)
                _panel.gameObject.SetActive(true);

            if (_text != null)
                _text.text = $"Wave {current}/{total}";
        }

        private void Build()
        {
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
            _panel = panelObject.GetComponent<RectTransform>();
            _panel.SetParent(root, false);
            _panel.anchorMin = new Vector2(0.5f, 1f);
            _panel.anchorMax = new Vector2(0.5f, 1f);
            _panel.pivot = new Vector2(0.5f, 1f);
            _panel.anchoredPosition = new Vector2(0f, -18f);
            _panel.sizeDelta = new Vector2(280f, 56f);

            Image background = panelObject.AddComponent<Image>();
            background.color = new Color(0.06f, 0.07f, 0.1f, 0.62f);
            background.raycastTarget = false;

            GameObject textObject = new("Value", typeof(RectTransform));
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(_panel, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            _text = textObject.AddComponent<Text>();
            _text.font = GetDefaultFont();
            _text.fontSize = 32;
            _text.fontStyle = FontStyle.Bold;
            _text.alignment = TextAnchor.MiddleCenter;
            _text.color = new Color(0.92f, 0.96f, 1f, 1f);
            _text.raycastTarget = false;

            _panel.gameObject.SetActive(false);
        }

        private static Font GetDefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
