using Data;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class WorldHealthBar : MonoBehaviour
    {
        private const float SegmentSpacing = 0.04f;

        private Image[] _segments;
        private Camera _camera;
        private Color _aliveColor = new(1f, 0.2f, 0.12f, 1f);
        private Color _emptyColor = new(0.12f, 0.12f, 0.15f, 0.76f);
        private Color _backgroundColor = new(0.02f, 0.025f, 0.035f, 0.72f);

        public static WorldHealthBar Create(Transform target, int maxHealth, float height, UiData ui = null)
        {
            GameObject root = new("WorldHealthBar", typeof(RectTransform));
            root.transform.SetParent(target, false);
            root.transform.localPosition = Vector3.up * height;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            WorldHealthBar view = root.AddComponent<WorldHealthBar>();

            if (ui != null)
            {
                view._aliveColor = ui.WorldHealthAliveColor;
                view._emptyColor = ui.WorldHealthEmptyColor;
                view._backgroundColor = ui.WorldHealthBackgroundColor;
            }

            view.Initialize(maxHealth);
            return view;
        }

        public void SetHealth(int health, int maxHealth)
        {
            if (_segments == null)
                return;

            for (int i = 0; i < _segments.Length; i++)
            {
                bool isAlive = i < health;
                _segments[i].color = isAlive ? _aliveColor : _emptyColor;
            }
        }

        private void LateUpdate()
        {
            if (_camera == null)
            {
                _camera = Camera.main;

                if (_camera == null)
                    return;
            }

            transform.rotation = _camera.transform.rotation;
        }

        private void Initialize(int maxHealth)
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 20;

            RectTransform root = GetComponent<RectTransform>();
            root.sizeDelta = new Vector2(1.25f, 0.18f);

            Image background = CreateImage("Background", root, _backgroundColor);
            Stretch(background.rectTransform);

            CreateSegments(root, maxHealth);
        }

        private void CreateSegments(RectTransform parent, int maxHealth)
        {
            int count = Mathf.Max(1, maxHealth);
            _segments = new Image[count];

            float totalWidth = 1.05f;
            float segmentWidth = (totalWidth - SegmentSpacing * (count - 1)) / count;
            float startX = -totalWidth * 0.5f;

            for (int i = 0; i < count; i++)
            {
                Image segment = CreateImage($"HP_{i + 1}", parent, _aliveColor);
                RectTransform rect = segment.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.anchoredPosition = new Vector2(startX + i * (segmentWidth + SegmentSpacing), 0f);
                rect.sizeDelta = new Vector2(segmentWidth, 0.1f);
                _segments[i] = segment;
            }
        }

        private static Image CreateImage(string name, RectTransform parent, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
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
    }
}
