using Data;
using Game.Player;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class PlayerHealthView : MonoBehaviour
    {
        private const float SegmentSpacing = 12f;

        private PlayerController _player;
        private Image[] _healthSegments;
        private Color _aliveColor = new(1f, 0.16f, 0.12f, 1f);
        private Color _emptyColor = new(0.18f, 0.2f, 0.24f, 0.82f);

        public static PlayerHealthView Create(PlayerController player, UiData ui = null)
        {
            GameObject root = new("PlayerHealthView", typeof(RectTransform));
            PlayerHealthView view = root.AddComponent<PlayerHealthView>();

            if (ui != null)
            {
                view._aliveColor = ui.HealthAliveColor;
                view._emptyColor = ui.HealthEmptyColor;
            }

            view.Initialize(player);
            return view;
        }

        private void Initialize(PlayerController player)
        {
            _player = player;

            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform root = GetComponent<RectTransform>();
            Stretch(root);

            RectTransform panel = CreateRect("HealthPanel", root);
            panel.anchorMin = new Vector2(0f, 1f);
            panel.anchorMax = new Vector2(0f, 1f);
            panel.pivot = new Vector2(0f, 1f);
            panel.anchoredPosition = new Vector2(42f, -42f);
            panel.sizeDelta = new Vector2(340f, 92f);

            Text label = CreateText("Label", panel, "HP", 36, FontStyle.Bold,
                new Color(0.92f, 0.96f, 1f, 1f));
            label.alignment = TextAnchor.MiddleLeft;
            SetRect(label.rectTransform, new Vector2(0f, 0f), new Vector2(70f, 72f));

            CreateSegments(panel, player.MaxHealth);
            SetHealth(player.Health, player.MaxHealth);

            _player.HealthChanged += SetHealth;
        }

        private void OnDestroy()
        {
            if (_player != null)
                _player.HealthChanged -= SetHealth;
        }

        private void CreateSegments(RectTransform parent, int count)
        {
            int segmentCount = Mathf.Max(1, count);
            _healthSegments = new Image[segmentCount];

            for (int i = 0; i < segmentCount; i++)
            {
                Image segment = CreateImage($"Life_{i + 1}", parent, _aliveColor);
                RectTransform rect = segment.rectTransform;
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.anchoredPosition = new Vector2(82f + i * (52f + SegmentSpacing), 0f);
                rect.sizeDelta = new Vector2(52f, 52f);
                _healthSegments[i] = segment;
            }
        }

        private void SetHealth(int health, int maxHealth)
        {
            if (_healthSegments == null)
                return;

            for (int i = 0; i < _healthSegments.Length; i++)
            {
                bool isAlive = i < health;
                _healthSegments[i].color = isAlive ? _aliveColor : _emptyColor;
            }
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
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static Font GetDefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
